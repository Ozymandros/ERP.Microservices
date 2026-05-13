using Microsoft.AspNetCore.Mvc;
using Moq;
using MyApp.Agentic.API.Controllers;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;

namespace MyApp.Agentic.Application.Tests;

public class AIModelsControllerTests
{
    [Fact]
    public async Task GetByProvider_ReturnsOkWithModels()
    {
        var providerId = Guid.NewGuid();
        var service = new Mock<IAIModelService>();
        service.Setup(s => s.ListByProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AIModelDto(Guid.NewGuid(), providerId, "OpenAI", "GPT-5", "gpt-5", 8192, "chat", 0.7, 3, 2048, 1536, true, true, null, Domain.Agents.BotType.Chat, null)]);

        var controller = new AIModelsController(service.Object);
        var result = await controller.GetByProvider(providerId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
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

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
