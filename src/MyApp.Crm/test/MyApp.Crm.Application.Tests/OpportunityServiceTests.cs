using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Services;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Sales.Application.Contracts.DTOs;

namespace MyApp.Crm.Application.Tests;

public class OpportunityServiceTests
{
    [Fact]
    public async Task MarkWonAsync_WhenConvertToQuoteAndQuoteLinesEmpty_UsesOpportunityLines()
    {
        var oppId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var opp = new Opportunity(oppId, customerId, "Opp", "owner");
        opp.AddLine(Guid.NewGuid(), "Line", 2m, 10m, 0m, productId: productId);

        var repo = new Mock<IOpportunityRepository>();
        repo.Setup(r => r.GetByIdAsync(oppId)).ReturnsAsync(opp);
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

        var invoker = new Mock<IServiceInvoker>();
        invoker.Setup(i => i.InvokeAsync<CreateQuoteDto, SalesOrderDto>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.Is<CreateQuoteDto>(r => r.Lines.Count == 1
                                          && r.Lines[0].ProductId == productId
                                          && r.Lines[0].Quantity == 2
                                          && r.Lines[0].UnitPrice == 10m),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SalesOrderDto(Guid.NewGuid()) { OrderNumber = "Q-1" });

        var svc = new OpportunityService(repo.Object, mapper.Object, logger.Object, unitOfWork.Object, publisher.Object, invoker.Object);

        var request = new MarkOpportunityWonRequest(
            Note: null,
            ConvertToQuote: true,
            Quote: new ConvertOpportunityToQuoteDto(
                ValidityDays: 30,
                Lines: new List<CreateUpdateSalesOrderLineDto>(),
                OrderDate: null));

        await svc.MarkWonAsync(oppId, request);

        invoker.VerifyAll();
    }
}

