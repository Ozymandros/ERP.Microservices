using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Application.Services;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Agentic.API;

/// <summary>
/// DI extensions for registering agent skills from markdown instruction files.
/// </summary>
public static class AgentSkillExtensions
{
    /// <summary>
    /// Registers <see cref="SkillService"/> and loads skills via the supplied configuration callback.
    /// </summary>
    public static IServiceCollection AddAgentSkills(
        this IServiceCollection services,
        Action<AgentSkillOptions> configure)
    {
        var options = new AgentSkillOptions();
        configure(options);

        services.AddSingleton<SkillService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SkillService>>();
            var eventPublisher = sp.GetRequiredService<IEventPublisher>();
            var service = new SkillService(new NoOpUnitOfWork(), eventPublisher, logger);
            options.LoadSkills(service);
            return service;
        });

        services.AddSingleton<ISkillService>(sp => sp.GetRequiredService<SkillService>());

        return services;
    }
}
