using MyApp.Agentic.Application.AI;
using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Application.Tests;

public class AgentToolResolverTests
{
    private readonly AgentToolRegistry _registry = new();
    private readonly AgentToolResolver _resolver;

    public AgentToolResolverTests()
    {
        _resolver = new AgentToolResolver(_registry);
        _registry.RegisterTool(
            new RegisteredAgentTool("get_product_by_name", "Get ERP product by name.", ToolHttpVerb.Get),
            (_, _) => Task.FromResult("{}"));
        _registry.RegisterTool(
            new RegisteredAgentTool("create_inventory_stock", "Create ERP inventory stock.", ToolHttpVerb.Post),
            (_, _) => Task.FromResult("{}"));
    }

    [Fact]
    public void ResolveTools_WhenAgentHasNoPlugins_ExposesRegisteredReadToolsForChatBot()
    {
        var agent = CreateAgent(BotType.Chat);

        var tools = _resolver.ResolveTools(agent);

        Assert.Single(tools);
        Assert.Equal("get_product_by_name", tools[0].Name);
    }

    [Fact]
    public void ResolveTools_WhenAgentHasPlugins_FiltersToConfiguredTools()
    {
        var agent = CreateAgent(BotType.Chat);
        agent.Plugins.Add(new AgentPlugin(Guid.NewGuid(), agent.Id, "create_inventory_stock", "inventory.create"));

        var tools = _resolver.ResolveTools(agent);

        Assert.Empty(tools);
    }

    [Fact]
    public void ResolveTools_WhenChatBot_IncludesSearchAndDocsButNotMutations()
    {
        _registry.RegisterTool(
            new RegisteredAgentTool("search_docs", "Search ERP docs.", ToolHttpVerb.Get),
            (_, _) => Task.FromResult("{}"));
        _registry.RegisterTool(
            new RegisteredAgentTool("create_inventory_stock", "Create inventory stock.", ToolHttpVerb.Post),
            (_, _) => Task.FromResult("{}"));

        var agent = CreateAgent(BotType.Chat);
        var tools = _resolver.ResolveTools(agent);

        Assert.Contains(tools, tool => tool.Name == "get_product_by_name");
        Assert.Contains(tools, tool => tool.Name == "search_docs");
        Assert.DoesNotContain(tools, tool => tool.Name == "create_inventory_stock");
        Assert.All(tools, tool => Assert.Equal(ToolHttpVerb.Get, tool.Verb));
    }

    [Fact]
    public void ResolveTools_WhenAgentBot_IncludesWriteTools()
    {
        var agent = CreateAgent(BotType.Agent);
        var tools = _resolver.ResolveTools(agent);

        Assert.Contains(tools, tool => tool.Name == "get_product_by_name" && tool.Verb == ToolHttpVerb.Get);
        Assert.Contains(tools, tool => tool.Name == "create_inventory_stock" && tool.Verb == ToolHttpVerb.Post);
    }

    private static Agent CreateAgent(BotType botType)
    {
        return new Agent(
            Guid.NewGuid(),
            "ERP Assistant",
            "Helps with ERP data",
            Guid.NewGuid(),
            0.7,
            "You are a helpful AI assistant.",
            botType: botType);
    }
}
