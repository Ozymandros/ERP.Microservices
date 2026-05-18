using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyApp.Audit.Application.Mapping;

namespace MyApp.Audit.Application.Tests.Common;

internal static class MapperTestHelper
{
    public static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<AuditMappingProfile>());
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
