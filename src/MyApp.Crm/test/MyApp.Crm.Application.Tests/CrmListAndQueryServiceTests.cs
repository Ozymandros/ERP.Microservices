using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Ports;
using MyApp.Crm.Application.Services;
using MyApp.Crm.Domain.Activities;
using MyApp.Crm.Domain.Leads;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Crm.Application.Tests;

/// <summary>
/// Covers list / query / lookup paths of CRM application services and verifies the
/// cancellation token is forwarded to the repositories.
/// </summary>
public class CrmListAndQueryServiceTests
{
    private readonly Mock<IMapper> _mapper = new(MockBehavior.Loose);
    private readonly CancellationTokenSource _cts = new();

    #region ActivityService

    private ActivityService CreateActivityService(Mock<IActivityRepository> repo) =>
        new(repo.Object, _mapper.Object, Mock.Of<ILogger<ActivityService>>(), Mock.Of<IUnitOfWork>(), Mock.Of<IEventPublisher>());

    [Fact]
    public async Task ActivityService_ListAsync_ForwardsTokenAndMaps()
    {
        var token = _cts.Token;
        var repo = new Mock<IActivityRepository>();
        var entities = new List<Activity>
        {
            new(Guid.NewGuid(), "Call", ActivityType.Call, DateTimeOffset.UtcNow.AddDays(1), "agent", customerId: Guid.NewGuid())
        };
        repo.Setup(r => r.ListAsync(token)).ReturnsAsync(entities);
        _mapper.Setup(m => m.Map<IEnumerable<ActivityDto>>(entities)).Returns(Array.Empty<ActivityDto>());

        var result = await CreateActivityService(repo).ListAsync(token);

        Assert.NotNull(result);
        repo.Verify(r => r.ListAsync(token), Times.Once);
        _mapper.Verify(m => m.Map<IEnumerable<ActivityDto>>(entities), Times.Once);
    }

    [Fact]
    public async Task ActivityService_QueryAsync_ReturnsPagedResult()
    {
        var token = _cts.Token;
        var repo = new Mock<IActivityRepository>();
        var spec = Mock.Of<ISpecification<Activity>>();
        var activity = new Activity(Guid.NewGuid(), "Call", ActivityType.Call, DateTimeOffset.UtcNow.AddDays(1), "agent", customerId: Guid.NewGuid());
        repo.Setup(r => r.QueryAsync(spec, token))
            .ReturnsAsync(new PaginatedResult<Activity>([activity], 2, 5, 11));

        var result = await CreateActivityService(repo).QueryAsync(spec, token);

        Assert.Single(result.Items);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(11, result.TotalCount);
        repo.Verify(r => r.QueryAsync(spec, token), Times.Once);
    }

    #endregion

    #region LeadService

    private LeadService CreateLeadService(Mock<ILeadRepository> repo) =>
        new(repo.Object, _mapper.Object, Mock.Of<ILogger<LeadService>>(), Mock.Of<IUnitOfWork>(), Mock.Of<IEventPublisher>());

    [Fact]
    public async Task LeadService_GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var token = _cts.Token;
        var id = Guid.NewGuid();
        var repo = new Mock<ILeadRepository>();
        repo.Setup(r => r.GetByIdAsync(id, token)).ReturnsAsync((Lead?)null);

        var result = await CreateLeadService(repo).GetByIdAsync(id, token);

        Assert.Null(result);
        repo.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public async Task LeadService_GetByIdAsync_WhenFound_MapsEntity()
    {
        var token = _cts.Token;
        var lead = new Lead(Guid.NewGuid(), "Lead", "owner", "web");
        var repo = new Mock<ILeadRepository>();
        repo.Setup(r => r.GetByIdAsync(lead.Id, token)).ReturnsAsync(lead);

        await CreateLeadService(repo).GetByIdAsync(lead.Id, token);

        _mapper.Verify(m => m.Map<LeadDto>(lead), Times.Once);
    }

    [Fact]
    public async Task LeadService_ListAsync_ForwardsTokenAndMaps()
    {
        var token = _cts.Token;
        var leads = new List<Lead> { new(Guid.NewGuid(), "Lead", "owner", "web") };
        var repo = new Mock<ILeadRepository>();
        repo.Setup(r => r.ListAsync(token)).ReturnsAsync(leads);
        _mapper.Setup(m => m.Map<IEnumerable<LeadDto>>(leads)).Returns(Array.Empty<LeadDto>());

        var result = await CreateLeadService(repo).ListAsync(token);

        Assert.NotNull(result);
        repo.Verify(r => r.ListAsync(token), Times.Once);
    }

    [Fact]
    public async Task LeadService_ListPaginatedAsync_ReturnsPagedResult()
    {
        var token = _cts.Token;
        var leads = new List<Lead> { new(Guid.NewGuid(), "Lead", "owner", "web") };
        var repo = new Mock<ILeadRepository>();
        repo.Setup(r => r.GetAllPaginatedAsync(1, 10, null, token))
            .ReturnsAsync(new PaginatedResult<Lead>(leads, 1, 10, 1));
        _mapper.Setup(m => m.Map<IEnumerable<LeadDto>>(It.IsAny<IEnumerable<Lead>>())).Returns(Array.Empty<LeadDto>());

        var result = await CreateLeadService(repo).ListPaginatedAsync(1, 10, token);

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        repo.Verify(r => r.GetAllPaginatedAsync(1, 10, null, token), Times.Once);
    }

    [Fact]
    public async Task LeadService_QueryAsync_ReturnsPagedResult()
    {
        var token = _cts.Token;
        var spec = Mock.Of<ISpecification<Lead>>();
        var repo = new Mock<ILeadRepository>();
        repo.Setup(r => r.QueryAsync(spec, token))
            .ReturnsAsync(new PaginatedResult<Lead>([new Lead(Guid.NewGuid(), "Lead", "owner", "web")], 1, 20, 1));

        var result = await CreateLeadService(repo).QueryAsync(spec, token);

        Assert.Single(result.Items);
        Assert.Equal(20, result.PageSize);
        repo.Verify(r => r.QueryAsync(spec, token), Times.Once);
    }

    #endregion

    #region OpportunityService

    private OpportunityService CreateOpportunityService(Mock<IOpportunityRepository> repo) =>
        new(repo.Object, _mapper.Object, Mock.Of<ILogger<OpportunityService>>(), Mock.Of<IUnitOfWork>(),
            Mock.Of<IEventPublisher>(), Mock.Of<ISalesQuoteCreationPort>());

    [Fact]
    public async Task OpportunityService_ListAsync_ForwardsTokenAndMaps()
    {
        var token = _cts.Token;
        var list = new List<Opportunity> { new(Guid.NewGuid(), Guid.NewGuid(), "Opp", "owner") };
        var repo = new Mock<IOpportunityRepository>();
        repo.Setup(r => r.ListAsync(token)).ReturnsAsync(list);
        _mapper.Setup(m => m.Map<IEnumerable<OpportunityDto>>(list)).Returns(Array.Empty<OpportunityDto>());

        var result = await CreateOpportunityService(repo).ListAsync(token);

        Assert.NotNull(result);
        repo.Verify(r => r.ListAsync(token), Times.Once);
    }

    [Fact]
    public async Task OpportunityService_QueryAsync_ReturnsPagedResult()
    {
        var token = _cts.Token;
        var spec = Mock.Of<ISpecification<Opportunity>>();
        var repo = new Mock<IOpportunityRepository>();
        repo.Setup(r => r.QueryAsync(spec, token))
            .ReturnsAsync(new PaginatedResult<Opportunity>([new Opportunity(Guid.NewGuid(), Guid.NewGuid(), "Opp", "owner")], 3, 10, 21));

        var result = await CreateOpportunityService(repo).QueryAsync(spec, token);

        Assert.Single(result.Items);
        Assert.Equal(3, result.PageNumber);
        Assert.Equal(21, result.TotalCount);
    }

    [Fact]
    public async Task OpportunityService_RemoveLineAsync_WhenMissing_ThrowsKeyNotFound()
    {
        var token = _cts.Token;
        var id = Guid.NewGuid();
        var repo = new Mock<IOpportunityRepository>();
        repo.Setup(r => r.GetByIdAsync(id, token)).ReturnsAsync((Opportunity?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateOpportunityService(repo).RemoveLineAsync(id, Guid.NewGuid(), token));
        repo.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    #endregion
}
