using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Agentic.API.Controllers;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Domain.Agents;
using Xunit;

namespace MyApp.Agentic.API.Tests.Controllers;

public class AgentsControllerTests
{
    private readonly Mock<IAgentService> _service = new();
    private readonly AgentsController _sut;

    public AgentsControllerTests()
    {
        _sut = new AgentsController(_service.Object, new Mock<ILogger<AgentsController>>().Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        _service.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var result = await _sut.GetAll(CancellationToken.None);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NotFound()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((AgentDto?)null);
        var result = await _sut.GetById(id, CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_Returns201()
    {
        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var dto = new CreateAgentDto("Bot", "desc", providerId, modelId, 0.7, "prompt", null);
        var created = new AgentDto(
            Guid.NewGuid(), "Bot", "desc", providerId, "prov", modelId, "model",
            BotType.Chat, "prompt", 0.7, 3, 2048, 1536, true, true, null, true, null);
        _service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(created);
        var result = await _sut.Create(dto, CancellationToken.None);
        result.Should().BeOfType<CreatedAtActionResult>();
    }
}