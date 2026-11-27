using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyApp.Agentic.API.Controllers;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using Xunit;

namespace MyApp.Agentic.API.Tests.Controllers;

public class AIProvidersControllerTests
{
    private readonly Mock<IAIProviderService> _service = new();
    private readonly AIProvidersController _sut;

    public AIProvidersControllerTests()
    {
        _sut = new AIProvidersController(_service.Object); // primary constructor controller
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
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((AIProviderDto?)null);
        var result = await _sut.GetById(id, CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_Returns201()
    {
        var dto = new CreateAIProviderDto("OpenAI", "https://api.openai.com", null);
        var created = new AIProviderDto(
            Guid.NewGuid(), "OpenAI", "https://api.openai.com", null, false,
            0.7, 3, 2048, 1536, true, true, null, Domain.Agents.BotType.Chat, null);
        _service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(created);
        var result = await _sut.Create(dto, CancellationToken.None);
        result.Should().BeOfType<CreatedAtActionResult>();
    }
}