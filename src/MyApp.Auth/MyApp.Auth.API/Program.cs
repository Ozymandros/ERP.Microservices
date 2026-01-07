
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using MyApp.Shared.Infrastructure.OpenApi;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Auth.Application.Services;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Auth.Infrastructure.Data;
using MyApp.Auth.Infrastructure.Data.Repositories;
using MyApp.Auth.Infrastructure.Data.Seeders;
using MyApp.Auth.Infrastructure.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Infrastructure.Caching;
using MyApp.Shared.Infrastructure.Extensions;
using MyApp.Shared.Infrastructure.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with sensitive data masking and OpenTelemetry integration
builder.AddCustomLogging();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddOtlpExporter(); // to the collector or Azure Monitor
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddRuntimeInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter();
    });

// This line registers the DaprClient (Singleton) in the Dependency Injection (DI) container
builder.Services.AddDaprClient();

// Configure JSON options FIRST - before AddOpenApi() so JsonSchemaExporter uses them
// builder.Services.ConfigureHttpJsonOptions(options =>
// {
//     options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
//     options.SerializerOptions.Converters.Add(new MyApp.Shared.Infrastructure.Json.DateTimeConverter());
//     options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
// });

// Configure JSON options for Controllers as well
// builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
// {
//     options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
//     options.JsonSerializerOptions.Converters.Add(new MyApp.Shared.Infrastructure.Json.DateTimeConverter());
//     options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
// });

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<JwtSecuritySchemeDocumentTransformer>();
    options.AddDocumentTransformer<MyApp.Shared.Infrastructure.OpenApi.DateTimeSchemaDocumentTransformer>();

    // Forçem el tipus a nivell d'esquema per evitar que el serialitzador intern pete
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (context.JsonTypeInfo.Type == typeof(DateTime) || context.JsonTypeInfo.Type == typeof(DateTime?))
        {
            schema.Type = Microsoft.OpenApi.JsonSchemaType.String;
            schema.Format = "date-time";
            schema.Default = null; // Evita que el motor intenti serialitzar un default(DateTime)
            schema.Example = null;
        }
        return Task.CompletedTask;
    });

    // També afegim el transformer compartit com a fallback
    options.AddSchemaTransformer<MyApp.Shared.Infrastructure.OpenApi.DateTimeSchemaTransformer>();
});

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("AuthDb")
    ?? "Server=localhost;Database=AuthDb;Trusted_Connection=True;";

// Health Checks
builder.Services.AddCustomHealthChecks(connectionString ?? throw new InvalidOperationException("Connection string 'authdb' not found."));

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(connectionString, options => options.EnableRetryOnFailure()));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new ArgumentNullException("Jwt:SecretKey");
var issuer = jwtSettings["Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer");
var audience = jwtSettings["Audience"] ?? throw new ArgumentNullException("Jwt:Audience");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30) // Permet 30 segons de marge per sincronització Docker/contenidors
        };
        // Si es Desarrollo, permitimos HTTP. Si no (Prod/Staging), HTTPS es obligatorio.
        if (builder.Environment.IsDevelopment())
        {
            options.RequireHttpsMetadata = false;
        }
        else
        {
            options.RequireHttpsMetadata = true;
        }
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new ArgumentNullException("Authentication:Google:ClientId");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new ArgumentNullException("Authentication:Google:ClientSecret");
        options.Scope.Add("profile");
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? throw new ArgumentNullException("Authentication:Microsoft:ClientId");
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? throw new ArgumentNullException("Authentication:Microsoft:ClientSecret");
    })
    .AddOAuth("Apple", options =>
    {
        options.ClientId = builder.Configuration["Authentication:Apple:ClientId"] ?? throw new ArgumentNullException("Authentication:Apple:ClientId");
        options.ClientSecret = builder.Configuration["Authentication:Apple:ClientSecret"] ?? throw new ArgumentNullException("Authentication:Apple:ClientSecret");
        options.CallbackPath = new PathString("/signin-apple");
        options.AuthorizationEndpoint = "https://appleid.apple.com/auth/authorize";
        options.TokenEndpoint = "https://appleid.apple.com/auth/token";
        options.UserInformationEndpoint = "https://appleid.apple.com/auth/validate";
    })
    .AddOAuth("GitHub", options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"] ?? throw new ArgumentNullException("Authentication:GitHub:ClientId");
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? throw new ArgumentNullException("Authentication:GitHub:ClientSecret");
        options.CallbackPath = new PathString("/signin-github");
        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";
    });

builder.Services.AddHttpContextAccessor();

// Add repository registration
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Add service registration
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();

builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IPermissionChecker, DaprPermissionChecker>();

builder.AddRedisDistributedCache("cache");
builder.Services.AddScoped<ICacheService, DistributedCacheWrapper>();

// AutoMapper registration
builder.Services.AddAutoMapper(
    cfg => { /* optional configuration */ },
    typeof(MyApp.Auth.Application.Mappings.AuthMappingProfile).Assembly
);

var origins = builder.Configuration["FRONTEND_ORIGIN"]?.Split(';') ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Apply migrations and seed default roles
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    await dbContext.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    await RoleSeeder.SeedAsync(roleManager);

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await AdminUserSeeder.SeedAsync(userManager);

    // Seed default permissions
    await PermissionSeeder.SeedPermissionsAsync(dbContext);
}

// Map OpenAPI endpoint (available for Gateway access)
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    // Use Scalar instead of SwaggerUI - lighter and more stable
    app.MapScalarApiReference();
}
else
{
    // ONLY in production we force HTTPS
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map health check endpoint
app.UseCustomHealthChecks();

app.Run();