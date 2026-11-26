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

