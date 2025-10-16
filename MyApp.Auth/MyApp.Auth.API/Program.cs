using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Auth.Application.Mappings;
using MyApp.Auth.Application.Services;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Auth.Infrastructure.Data;
using MyApp.Auth.Infrastructure.Data.Repositories;
using MyApp.Auth.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Auth API", Version = "v1" });

    // Add JWT Bearer token configuration to Swagger
    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("AuthDb")
    ?? "Server=localhost;Database=AuthDb;Trusted_Connection=True;";

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Identity
builder.Services.AddIdentity<User, Role>(options =>
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
            ClockSkew = TimeSpan.Zero
        };
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

// Add repository registration
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Add service registration
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();

// Add AutoMapper

var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile(new AuthMappingProfile());
    // afegeix més perfils si cal
});
var mapper = config.CreateMapper();
builder.Services.AddSingleton(mapper);

// Add Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply migrations and seed default roles
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    dbContext.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    await RoleSeeder.SeedAsync(roleManager);

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    await AdminUserSeeder.SeedAsync(userManager);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();