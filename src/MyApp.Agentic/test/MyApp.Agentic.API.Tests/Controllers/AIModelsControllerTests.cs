using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyApp.Agentic.API.Controllers;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Domain.Agents;
using Xunit;

namespace MyApp.Agentic.API.Tests.Controllers;

public class AIModelsControllerTests
{
    [Fact]
    public async Task GetByProvider_ReturnsOkWithModels()
    {
        var providerId = Guid.NewGuid();
        var service = new Mock<IAIModelService>();
        service.Setup(s => s.ListByProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AIModelDto(Guid.NewGuid(), providerId, "OpenAI", "GPT-5", "gpt-5", 8192, "chat", 0.7, 3, 2048, 1536, true, true, null, BotType.Chat, null)]);

        var controller = new AIModelsController(service.Object);
        var result = await controller.GetByProvider(providerId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidationFails_ReturnsBadRequest()
    {
        var providerId = Guid.NewGuid();
        var service = new Mock<IAIModelService>();
        service.Setup(s => s.CreateAsync(It.IsAny<CreateAIModelDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("invalid"));

        var controller = new AIModelsController(service.Object);
        var result = await controller.Create(new CreateAIModelDto(providerId, "GPT-5", "gpt-5", 8192, "chat"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_NotFound()
    {
        var id = Guid.NewGuid();
        var service = new Mock<IAIModelService>();
        service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((AIModelDto?)null);
        var controller = new AIModelsController(service.Object);
        var result = await controller.GetById(id, CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}