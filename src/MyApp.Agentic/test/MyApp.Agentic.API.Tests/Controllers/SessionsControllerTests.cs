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

public class SessionsControllerTests
{
    private readonly Mock<IAgentService> _service = new();
    private readonly SessionsController _sut;

    public SessionsControllerTests()
    {
        _sut = new SessionsController(_service.Object, new Mock<ILogger<SessionsController>>().Object);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "test"))
            }
        };
    }

    [Fact]
    public async Task ListSessions_ReturnsOk()
    {
        _service.Setup(s => s.ListSessionsAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var result = await _sut.ListSessions(CancellationToken.None);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSession_NotFound()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetSessionAsync(id, "user-1", It.IsAny<CancellationToken>())).ReturnsAsync((SessionDetailsResponse?)null);
        var result = await _sut.GetSession(id, CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateSession_Returns201()
    {
        var agentId = Guid.NewGuid();
        var request = new StartSessionRequest(agentId, "sess");
        var response = new StartSessionResponse(Guid.NewGuid(), agentId, "Bot", Domain.Agents.BotType.Chat, "user-1", "sess", DateTime.UtcNow, Domain.Sessions.SessionStatus.Active);
        _service.Setup(s => s.StartSessionAsync(request, "user-1", It.IsAny<CancellationToken>())).ReturnsAsync(response);
        var result = await _sut.CreateSession(request, CancellationToken.None);
        result.Should().BeOfType<CreatedAtActionResult>();
    }
}