using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Agentic.API.Controllers;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using Xunit;

namespace MyApp.Agentic.API.Tests.Controllers;

public class MessagesControllerTests
{
    private readonly Mock<IAgentService> _service = new();
    private readonly MessagesController _sut;

    public MessagesControllerTests()
    {
        _sut = new MessagesController(_service.Object, new Mock<ILogger<MessagesController>>().Object);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "test"))
            }
        };
    }

    [Fact]
    public async Task ProcessMessage_ReturnsOk()
    {
        var agentId = Guid.NewGuid();
        var request = new ProcessAgentMessageRequest(agentId, "hi", new AgentExecutionOptions(0.5));
        var response = new ProcessAgentMessageResponse(Guid.NewGuid(), "user-1", "hi", "reply", DateTime.UtcNow);
        _service.Setup(s => s.ProcessMessageAsync(request, "user-1", It.IsAny<CancellationToken>())).ReturnsAsync(response);
        var result = await _sut.ProcessMessage(request, CancellationToken.None);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ProcessMessage_InvalidModel_BadRequest()
    {
        _sut.ModelState.AddModelError("AgentId", "required");
        var result = await _sut.ProcessMessage(new ProcessAgentMessageRequest(Guid.Empty, "", null), CancellationToken.None);
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}