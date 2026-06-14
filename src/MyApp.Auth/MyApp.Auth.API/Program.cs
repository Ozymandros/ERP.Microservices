using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MyApp.Auth.API.Permissions;
using MyApp.Auth.API.Seeders;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Auth.Application.Mappings;
using MyApp.Auth.Application.Services;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Auth.Infrastructure.Data;
using MyApp.Auth.Infrastructure.Data.Repositories;
using MyApp.Auth.Infrastructure.Data.Seeders;
using MyApp.Auth.Infrastructure.Services;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisDistributedCache("cache");

builder.AddServiceDefaults(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Auth.API",
    ConnectionStringKey = "authdb",
    DbContextType = typeof(AuthDbContext),
    AutoMapperAssembly = typeof(AuthMappingProfile).Assembly,
    ConfigureServiceDependencies = services =>
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AuthDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();
        services.AddScoped<IPermissionChecker, MyApp.Auth.API.Permissions.PermissionChecker>();
    }
});

// AddIdentity sets cookie auth as the default scheme; API controllers use [AuthorizeJwt] / this policy.
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

app.UseServiceDefaults();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    await RoleSeeder.SeedAsync(roleManager);
    await AdminUserSeeder.SeedAsync(userManager);
    await PermissionSeeder.SeedPermissionsAsync(db);
}

app.Run();
