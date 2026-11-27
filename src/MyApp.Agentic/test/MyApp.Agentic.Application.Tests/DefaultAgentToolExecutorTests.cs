using Moq;
using MyApp.Agentic.Application.AI;

namespace MyApp.Agentic.Application.Tests;

public class DefaultAgentToolExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenToolNotRegistered_ReturnsErrorMessage()
    {
        var registry = new Mock<IAgentToolRegistry>();
        registry.Setup(r => r.GetHandler(It.IsAny<string>())).Returns((Func<string, CancellationToken, Task<string>>?)null);
        var sut = new DefaultAgentToolExecutor(registry.Object);

        var result = await sut.ExecuteAsync("missing_tool", "{}", CancellationToken.None);

        Assert.Contains("not registered", result);
        Assert.Contains("missing_tool", result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHandlerSucceeds_ReturnsHandlerResult()
    {
        var registry = new AgentToolRegistry();
        registry.RegisterTool(
            new RegisteredAgentTool("echo", "Echo args", ToolHttpVerb.Get),
            (args, _) => Task.FromResult($"ok:{args}"));
        var sut = new DefaultAgentToolExecutor(registry);

        var result = await sut.ExecuteAsync("echo", "{\"q\":1}", CancellationToken.None);

        Assert.Equal("ok:{\"q\":1}", result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHandlerThrows_ReturnsErrorMessage()
    {
        var registry = new AgentToolRegistry();
        registry.RegisterTool(
            new RegisteredAgentTool("boom", "Fails", ToolHttpVerb.Post),
            (_, _) => throw new InvalidOperationException("boom-failure"));
        var sut = new DefaultAgentToolExecutor(registry);

        var result = await sut.ExecuteAsync("boom", "{}", CancellationToken.None);

        Assert.Contains("Error executing tool 'boom'", result);
        Assert.Contains("boom-failure", result);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationTokenToHandler()
    {
        CancellationToken? observed = null;
        var registry = new AgentToolRegistry();
        registry.RegisterTool(
            new RegisteredAgentTool("ct_tool", "Observes CT", ToolHttpVerb.Get),
            (_, ct) =>
            {
                observed = ct;
                return Task.FromResult("done");
            });
        var sut = new DefaultAgentToolExecutor(registry);
        using var cts = new CancellationTokenSource();

        await sut.ExecuteAsync("ct_tool", "{}", cts.Token);

        Assert.Equal(cts.Token, observed);
    }
}
