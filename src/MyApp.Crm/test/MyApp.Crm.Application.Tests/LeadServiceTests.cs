using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Services;
using MyApp.Crm.Domain.Leads;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Application.Tests;

/// <summary>Tests for the LeadService application service.</summary>
public class LeadServiceTests
{
    /// <summary>Verifies that creating a lead publishes a CrmLeadCreatedEvent.</summary>
    [Fact]
    public async Task CreateAsync_PublishesLeadCreatedEvent()
    {
        var (svc, _, _, publisher) = CreateService();

        var dto = new CreateLeadDto("Lead", "owner", "web", null, null, null);
        await svc.CreateAsync(dto);

        publisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<CrmLeadCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Existing_UpdatesDetailsAndPublishes()
    {
        var (svc, repo, mapper, publisher) = CreateService();
        var lead = new Lead(Guid.NewGuid(), "Old", "owner", "web");
        repo.Setup(r => r.GetByIdForUpdateAsync(lead.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lead);
        repo.Setup(r => r.UpdateAsync(lead)).ReturnsAsync(lead);
        mapper.Setup(m => m.Map<LeadDto>(lead)).Returns((Lead l) => Map(l));

        var result = await svc.UpdateAsync(lead.Id, new UpdateLeadDto("New", "referral", "Jane", "j@x.com", "123"));

        Assert.Equal("New", result.Title);
        Assert.Equal("Jane", result.ContactName);
        publisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<CrmLeadUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Missing_Throws()
    {
        var (svc, repo, _, _) = CreateService();
        var id = Guid.NewGuid();
        repo.Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Lead?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.UpdateAsync(id, new UpdateLeadDto("Title", null, null, null, null)));
    }

    [Fact]
    public async Task QualifyAsync_Existing_QualifiesAndPublishes()
    {
        var (svc, repo, _, publisher) = CreateService();
        var lead = new Lead(Guid.NewGuid(), "Lead", "owner");
        var customerId = Guid.NewGuid();
        repo.Setup(r => r.GetByIdForUpdateAsync(lead.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lead);
        repo.Setup(r => r.UpdateAsync(lead)).ReturnsAsync(lead);

        await svc.QualifyAsync(lead.Id, new QualifyLeadDto(customerId));

        Assert.Equal(LeadStatus.Qualified, lead.Status);
        Assert.Equal(customerId, lead.CustomerId);
        publisher.Verify(p => p.PublishAsync(
            It.IsAny<string>(),
            It.Is<CrmLeadQualifiedEvent>(e => e.LeadId == lead.Id && e.CustomerId == customerId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QualifyAsync_Missing_Throws()
    {
        var (svc, repo, _, _) = CreateService();
        var id = Guid.NewGuid();
        repo.Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Lead?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.QualifyAsync(id, new QualifyLeadDto(Guid.NewGuid())));
    }

    [Fact]
    public async Task DeleteAsync_Existing_DeletesAndSaves()
    {
        var (svc, repo, _, _) = CreateService();
        var lead = new Lead(Guid.NewGuid(), "Lead", "owner");
        repo.Setup(r => r.GetByIdForUpdateAsync(lead.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lead);
        repo.Setup(r => r.DeleteAsync(lead)).Returns(Task.CompletedTask);

        await svc.DeleteAsync(lead.Id);

        repo.Verify(r => r.DeleteAsync(lead), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Missing_DoesNothing()
    {
        var (svc, repo, _, _) = CreateService();
        var id = Guid.NewGuid();
        repo.Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Lead?)null);

        await svc.DeleteAsync(id);

        repo.Verify(r => r.DeleteAsync(It.IsAny<Lead>()), Times.Never);
    }

    private static (LeadService Svc, Mock<ILeadRepository> Repo, Mock<IMapper> Mapper, Mock<IEventPublisher> Publisher) CreateService()
    {
        var repo = new Mock<ILeadRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Lead>())).ReturnsAsync((Lead l) => l);

        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<LeadDto>(It.IsAny<Lead>())).Returns((Lead l) => Map(l));

        var publisher = new Mock<IEventPublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());

        var svc = new LeadService(repo.Object, mapper.Object, Mock.Of<ILogger<LeadService>>(), unitOfWork.Object, publisher.Object);
        return (svc, repo, mapper, publisher);
    }

    private static LeadDto Map(Lead l) =>
        new(l.Id, l.Title, l.Source, l.ContactName, l.ContactEmail, l.ContactPhone, l.CustomerId, l.Status.ToString(), l.OwnerUsername, l.CreatedAt, l.UpdatedAt);
}
