using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Services;
using MyApp.Crm.Domain.Activities;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Application.Tests;

public class ActivityServiceTests
{
    private readonly Mock<IActivityRepository> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IEventPublisher> _events = new();
    private readonly ActivityService _sut;

    public ActivityServiceTests()
    {
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        _events.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sut = new ActivityService(
            _repo.Object,
            _mapper.Object,
            Mock.Of<ILogger<ActivityService>>(),
            _uow.Object,
            _events.Object);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var activity = new Activity(id, "Call", ActivityType.Call, DateTimeOffset.UtcNow.AddDays(1), "agent", customerId: Guid.NewGuid());
        var dto = new ActivityDto(
            id, "Call", "Call", "Open", activity.DueAt, null, "agent",
            null, null, activity.CustomerId, activity.CreatedAt, activity.UpdatedAt);

        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(activity);
        _mapper.Setup(m => m.Map<ActivityDto>(activity)).Returns(dto);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("Call", result!.Subject);
    }

    [Fact]
    public async Task CompleteAsync_Existing_CompletesActivity()
    {
        var id = Guid.NewGuid();
        var activity = new Activity(id, "Call", ActivityType.Call, DateTimeOffset.UtcNow.AddDays(1), "agent", customerId: Guid.NewGuid());
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(activity);
        _repo.Setup(r => r.UpdateAsync(activity)).ReturnsAsync(activity);
        _mapper.Setup(m => m.Map<ActivityDto>(activity))
            .Returns((Activity a) => new ActivityDto(
                a.Id, a.Subject, a.Type.ToString(), a.Status.ToString(), a.DueAt, a.CompletedAt,
                a.AssignedToUsername, a.LeadId, a.OpportunityId, a.CustomerId, a.CreatedAt, a.UpdatedAt));

        var result = await _sut.CompleteAsync(id, new CompleteActivityDto("done"));

        Assert.Equal(ActivityStatus.Completed.ToString(), result.Status);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task CreateAsync_Valid_AddsActivityAndPublishes()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<Activity>())).ReturnsAsync((Activity a) => a);
        _mapper.Setup(m => m.Map<ActivityDto>(It.IsAny<Activity>()))
            .Returns((Activity a) => new ActivityDto(
                a.Id, a.Subject, a.Type.ToString(), a.Status.ToString(), a.DueAt, a.CompletedAt,
                a.AssignedToUsername, a.LeadId, a.OpportunityId, a.CustomerId, a.CreatedAt, a.UpdatedAt));

        var dueAt = DateTimeOffset.UtcNow.AddDays(2);
        var result = await _sut.CreateAsync(new CreateActivityDto(
            "Follow up", "Call", dueAt, "agent", null, null, Guid.NewGuid()));

        Assert.Equal("Follow up", result.Subject);
        Assert.Equal(ActivityType.Call.ToString(), result.Type);
        _repo.Verify(r => r.AddAsync(It.Is<Activity>(a => a.Subject == "Follow up")), Times.Once);
        _events.Verify(e => e.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<CrmActivityCreatedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidType_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.CreateAsync(new CreateActivityDto(
                "X", "NotAType", DateTimeOffset.UtcNow, "agent", null, null, null)));
    }
}
