using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Services;
using MyApp.Crm.Domain.Leads;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Crm.Application.Tests;

public class LeadServiceTests
{
    [Fact]
    public async Task CreateAsync_PublishesLeadCreatedEvent()
    {
        var repo = new Mock<ILeadRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Lead>())).ReturnsAsync((Lead l) => l);

        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<LeadDto>(It.IsAny<Lead>()))
            .Returns((Lead l) => new LeadDto(l.Id, l.Title, l.Source, l.ContactName, l.ContactEmail, l.ContactPhone, l.CustomerId, l.Status.ToString(), l.OwnerUsername, l.CreatedAt, l.UpdatedAt));

        var logger = new Mock<ILogger<LeadService>>();
        var publisher = new Mock<IEventPublisher>();

        var svc = new LeadService(repo.Object, mapper.Object, logger.Object, publisher.Object);

        var dto = new CreateLeadDto("Lead", "owner", "web", null, null, null);
        await svc.CreateAsync(dto);

        publisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<CrmLeadCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

