using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Services;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Crm.Application.Contracts.Ports;

namespace MyApp.Crm.Application.Tests;

/// <summary>Tests for the OpportunityService application service.</summary>
public class OpportunityServiceTests
{
    /// <summary>Verifies that when marking an opportunity as won with ConvertToQuote set to true and no explicit quote lines, the opportunity's own lines are used.</summary>
    [Fact]
    public async Task MarkWonAsync_WhenConvertToQuoteAndQuoteLinesEmpty_UsesOpportunityLines()
    {
        var oppId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var opp = new Opportunity(oppId, customerId, "Opp", "owner");
        opp.AddLine(Guid.NewGuid(), "Line", 2m, 10m, 0m, productId: productId);

        var repo = new Mock<IOpportunityRepository>();
        repo.Setup(r => r.GetByIdAsync(oppId, It.IsAny<CancellationToken>())).ReturnsAsync(opp);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Opportunity>())).ReturnsAsync((Opportunity o) => o);

        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<OpportunityDto>(It.IsAny<Opportunity>()))
            .Returns((Opportunity o) => new OpportunityDto(
                o.Id, o.CustomerId, o.LeadId, o.Name, o.Stage.ToString(), o.Probability, o.ExpectedAmount,
                o.ExpectedCloseDate, o.ConvertedSalesQuoteId, o.ConvertedSalesQuoteNumber, o.OwnerUsername, o.CreatedAt, o.UpdatedAt));

        var logger = new Mock<ILogger<OpportunityService>>();
        var publisher = new Mock<IEventPublisher>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());

        var salesQuotes = new Mock<ISalesQuoteCreationPort>();
        salesQuotes.Setup(i => i.CreateQuoteAsync(
                It.Is<CreateSalesQuoteRequest>(r => r.Lines.Count == 1
                                          && r.Lines[0].ProductId == productId
                                          && r.Lines[0].Quantity == 2
                                          && r.Lines[0].UnitPrice == 10m),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SalesQuoteSnapshot(Guid.NewGuid(), "Q-1"));

        var svc = new OpportunityService(repo.Object, mapper.Object, logger.Object, unitOfWork.Object, publisher.Object, salesQuotes.Object);

        var request = new MarkOpportunityWonRequest(
            Note: null,
            ConvertToQuote: true,
            Quote: new ConvertOpportunityToQuoteDto(
                ValidityDays: 30,
                Lines: new List<CrmQuoteLineDto>(),
                OrderDate: null));

        await svc.MarkWonAsync(oppId, request);

        salesQuotes.VerifyAll();
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsMappedDto()
    {
        var (svc, repo, mapper) = CreateService();
        var oppId = Guid.NewGuid();
        var opp = new Opportunity(oppId, Guid.NewGuid(), "Opp", "owner");
        repo.Setup(r => r.GetByIdAsync(oppId, It.IsAny<CancellationToken>())).ReturnsAsync(opp);
        mapper.Setup(m => m.Map<OpportunityDto>(opp)).Returns(Map(opp));

        var result = await svc.GetByIdAsync(oppId);

        Assert.NotNull(result);
        Assert.Equal("Opp", result!.Name);
    }

    [Fact]
    public async Task UpdateForecastAsync_Existing_UpdatesProbability()
    {
        var (svc, repo, mapper) = CreateService();
        var oppId = Guid.NewGuid();
        var opp = new Opportunity(oppId, Guid.NewGuid(), "Opp", "owner");
        repo.Setup(r => r.GetByIdAsync(oppId, It.IsAny<CancellationToken>())).ReturnsAsync(opp);
        repo.Setup(r => r.UpdateAsync(opp)).ReturnsAsync(opp);
        mapper.Setup(m => m.Map<OpportunityDto>(opp)).Returns((Opportunity o) => Map(o));

        var result = await svc.UpdateForecastAsync(
            oppId,
            new UpdateOpportunityForecastDto(0.4m, 1000m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10))));

        Assert.Equal(0.4m, result.Probability);
        Assert.Equal(1000m, result.ExpectedAmount);
    }

    [Fact]
    public async Task MoveStageAsync_ToQualification_UpdatesStage()
    {
        var (svc, repo, mapper) = CreateService();
        var oppId = Guid.NewGuid();
        var opp = new Opportunity(oppId, Guid.NewGuid(), "Opp", "owner");
        repo.Setup(r => r.GetByIdAsync(oppId, It.IsAny<CancellationToken>())).ReturnsAsync(opp);
        repo.Setup(r => r.UpdateAsync(opp)).ReturnsAsync(opp);
        mapper.Setup(m => m.Map<OpportunityDto>(opp)).Returns((Opportunity o) => Map(o));

        var result = await svc.MoveStageAsync(oppId, new MoveOpportunityStageDto("Qualification"));

        Assert.Equal(OpportunityStage.Qualification.ToString(), result.Stage);
    }

    [Fact]
    public async Task MarkLostAsync_Existing_SetsLostStage()
    {
        var (svc, repo, mapper) = CreateService();
        var oppId = Guid.NewGuid();
        var opp = new Opportunity(oppId, Guid.NewGuid(), "Opp", "owner");
        repo.Setup(r => r.GetByIdAsync(oppId, It.IsAny<CancellationToken>())).ReturnsAsync(opp);
        repo.Setup(r => r.UpdateAsync(opp)).ReturnsAsync(opp);
        mapper.Setup(m => m.Map<OpportunityDto>(opp)).Returns((Opportunity o) => Map(o));

        var result = await svc.MarkLostAsync(oppId, new MarkOpportunityLostDto("no budget"));

        Assert.Equal(OpportunityStage.Lost.ToString(), result.Stage);
    }

    [Fact]
    public async Task AddLineAsync_Existing_AddsLine()
    {
        var (svc, repo, mapper) = CreateService();
        var opp = new Opportunity(Guid.NewGuid(), Guid.NewGuid(), "Opp", "owner");
        repo.Setup(r => r.GetByIdAsync(opp.Id, It.IsAny<CancellationToken>())).ReturnsAsync(opp);
        repo.Setup(r => r.UpdateAsync(opp)).ReturnsAsync(opp);
        mapper.Setup(m => m.Map<OpportunityLineDto>(It.IsAny<OpportunityLine>()))
            .Returns((OpportunityLine l) => new OpportunityLineDto(
                l.Id, l.OpportunityId, l.ProductId, l.Sku, l.Description, l.Quantity, l.UnitPrice,
                l.DiscountPercent, l.LineTotal, l.CreatedAt, l.UpdatedAt));

        var result = await svc.AddLineAsync(opp.Id, new CreateOpportunityLineDto("Widget", 2m, 50m, 0.1m));

        Assert.Equal("Widget", result.Description);
        Assert.Equal(2m, result.Quantity);
        Assert.Single(opp.Lines);
    }

    [Fact]
    public async Task AddLineAsync_MissingOpportunity_Throws()
    {
        var (svc, repo, _) = CreateService();
        var id = Guid.NewGuid();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Opportunity?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.AddLineAsync(id, new CreateOpportunityLineDto("X", 1m, 1m)));
    }

    [Fact]
    public async Task RemoveLineAsync_Existing_RemovesLine()
    {
        var (svc, repo, _) = CreateService();
        var opp = new Opportunity(Guid.NewGuid(), Guid.NewGuid(), "Opp", "owner");
        var line = opp.AddLine(Guid.NewGuid(), "Line", 1m, 10m, 0m);
        repo.Setup(r => r.GetByIdAsync(opp.Id, It.IsAny<CancellationToken>())).ReturnsAsync(opp);
        repo.Setup(r => r.UpdateAsync(opp)).ReturnsAsync(opp);

        await svc.RemoveLineAsync(opp.Id, line.Id);

        Assert.Empty(opp.Lines);
    }

    [Fact]
    public async Task GetForecastSummaryAsync_AggregatesByStage()
    {
        var (svc, repo, _) = CreateService();
        var o1 = new Opportunity(Guid.NewGuid(), Guid.NewGuid(), "A", "owner");
        o1.UpdateForecast(0.5m, 1000m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));
        var o2 = new Opportunity(Guid.NewGuid(), Guid.NewGuid(), "B", "owner");
        o2.MoveToStage(OpportunityStage.Qualification);
        o2.UpdateForecast(0.8m, 500m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)));

        repo.Setup(r => r.ListForForecastAsync("owner", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([o1, o2]);

        var result = await svc.GetForecastSummaryAsync("owner", null, null);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1500m, result.TotalExpectedAmount);
        Assert.Equal(900m, result.TotalWeightedAmount); // 1000*0.5 + 500*0.8
        Assert.Equal(2, result.ByStage.Count);
    }

    [Fact]
    public async Task GetForecastSummaryAsync_BlankOwner_Throws()
    {
        var (svc, _, _) = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.GetForecastSummaryAsync("  ", null, null));
    }

    private static (OpportunityService Svc, Mock<IOpportunityRepository> Repo, Mock<IMapper> Mapper) CreateService()
    {
        var repo = new Mock<IOpportunityRepository>();
        var mapper = new Mock<IMapper>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        var publisher = new Mock<IEventPublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = new OpportunityService(
            repo.Object,
            mapper.Object,
            Mock.Of<ILogger<OpportunityService>>(),
            unitOfWork.Object,
            publisher.Object,
            Mock.Of<ISalesQuoteCreationPort>());
        return (svc, repo, mapper);
    }

    private static OpportunityDto Map(Opportunity o) =>
        new(o.Id, o.CustomerId, o.LeadId, o.Name, o.Stage.ToString(), o.Probability, o.ExpectedAmount,
            o.ExpectedCloseDate, o.ConvertedSalesQuoteId, o.ConvertedSalesQuoteNumber, o.OwnerUsername, o.CreatedAt, o.UpdatedAt);
}

