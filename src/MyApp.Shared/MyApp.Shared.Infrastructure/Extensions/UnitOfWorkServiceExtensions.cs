using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Shared.Infrastructure.Extensions;

/// <summary>
/// DI registration helpers for <see cref="IUnitOfWork"/>.
/// </summary>
public static class UnitOfWorkServiceExtensions
{
    /// <summary>
    /// Registers <see cref="EfUnitOfWork"/> bound to the given <typeparamref name="TContext"/>.
    /// Call from <see cref="MicroserviceConfigurationOptions.ConfigureServiceDependencies"/> to override with a custom implementation.
    /// </summary>
    public static IServiceCollection AddEfUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IUnitOfWork>(sp => new EfUnitOfWork(sp.GetRequiredService<TContext>()));
        return services;
    }
}
