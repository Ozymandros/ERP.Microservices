using Microsoft.Extensions.DependencyInjection;
using MyApp.Projects.Application.Internals;
using MyApp.Projects.Application.UseCases.Posting;

namespace MyApp.Projects.Application.DependencyInjection;

public static class ProjectsApplicationExtensions
{
    public static IServiceCollection AddProjectsApplication(this IServiceCollection services)
    {
        services.AddScoped<IProjectsTransactionBoundary, ProjectsTransactionBoundary>();
        services.AddScoped<CreateItemConsumptionUseCase>();
        return services;
    }
}