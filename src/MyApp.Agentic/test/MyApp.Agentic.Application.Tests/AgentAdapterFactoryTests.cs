using MyApp.Agentic.Application.AI;

namespace MyApp.Agentic.Application.Tests;

public class AgentAdapterFactoryTests
{
    [Fact]
    public void EnsureAutoSuffix_AppendsAutoSuffixWhenMissing()
    {
        var result = AgentAdapterFactory.EnsureAutoSuffix("deepseek/deepseek-v4");
        Assert.Equal("deepseek/deepseek-v4:auto", result);
    }

    [Fact]
    public void EnsureAutoSuffix_PreservesSuffixWhenAlreadyPresent()
    {
        var result = AgentAdapterFactory.EnsureAutoSuffix("deepseek/deepseek-v4:auto");
        Assert.Equal("deepseek/deepseek-v4:auto", result);
    }
}
