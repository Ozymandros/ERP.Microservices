//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.IdentityModel.Tokens;
//using MyApp.Shared.Infrastructure.Extensions;
//using System.Text;
//using Xunit;

//namespace MyApp.Shared.Tests.Extensions;

//public class JwtAuthenticationExtensionsTests
//{
//    private readonly IServiceCollection _services;
//    private readonly IConfiguration _configuration;

//    public JwtAuthenticationExtensionsTests()
//    {
//        _services = new ServiceCollection();
//        var configurationBuilder = new ConfigurationBuilder();
//        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
//        {
//            ["Jwt:Key"] = "your-256-bit-secret-key-here-minimum-32-characters",
//            ["Jwt:Issuer"] = "https://auth.myapp.com",
//            ["Jwt:Audience"] = "https://myapp.com",
//            ["Jwt:TokenExpiryMinutes"] = "60"
//        });
//        _configuration = configurationBuilder.Build();
//    }

//    [Fact]
//    public void AddJwtAuthentication_ConfiguresAuthenticationWithValidOptions()
//    {
//        // Act
//        _services.AddJwtAuthentication(_configuration);

//        // Assert
//        var serviceProvider = _services.BuildServiceProvider();
//        var authOptions = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Authentication.AuthenticationOptions>();

//        Assert.NotNull(authOptions);
//        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.DefaultAuthenticateScheme);
//        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.DefaultChallengeScheme);
//    }

//    [Fact]
//    public void AddJwtAuthentication_ConfiguresJwtBearerOptions()
//    {
//        // Act
//        _services.AddJwtAuthentication(_configuration);

//        // Assert
//        var serviceProvider = _services.BuildServiceProvider();
//        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
//            .Get(JwtBearerDefaults.AuthenticationScheme);

//        Assert.NotNull(options);
//        Assert.True(options.RequireHttpsMetadata);
//        Assert.True(options.SaveToken);
//        Assert.NotNull(options.TokenValidationParameters);
//    }

//    [Fact]
//    public void AddJwtAuthentication_ConfiguresTokenValidationParameters()
//    {
//        // Act
//        _services.AddJwtAuthentication(_configuration);

//        // Assert
//        var serviceProvider = _services.BuildServiceProvider();
//        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
//            .Get(JwtBearerDefaults.AuthenticationScheme);
//        var validationParams = options.TokenValidationParameters;

//        Assert.True(validationParams.ValidateIssuerSigningKey);
//        Assert.True(validationParams.ValidateIssuer);
//        Assert.True(validationParams.ValidateAudience);
//        Assert.True(validationParams.ValidateLifetime);
//        Assert.Equal(_Configuration["Jwt:Issuer"], validationParams.ValidIssuer);
//        Assert.Equal(_Configuration["Jwt:Audience"], validationParams.ValidAudience);
//    }

//    [Fact]
//    public void AddJwtAuthentication_ValidatesTokenSignature()
//    {
//        // Act
//        _services.AddJwtAuthentication(_configuration);

//        // Assert
//        var serviceProvider = _services.BuildServiceProvider();
//        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
//            .Get(JwtBearerDefaults.AuthenticationScheme);
//        var validationParams = options.TokenValidationParameters;

//        var expectedKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_Configuration["Jwt:Key"]));
//        var actualKey = validationParams.IssuerSigningKey as SymmetricSecurityKey;

//        Assert.NotNull(actualKey);
//        Assert.Equal(expectedKey.Key, actualKey.Key);
//    }

//    [Fact]
//    public void AddJwtAuthentication_ThrowsException_WhenJwtKeyIsMissing()
//    {
//        // Arrange
//        var invalidConfig = new ConfigurationBuilder()
//            .AddInMemoryCollection(new Dictionary<string, string>
//            {
//                ["Jwt:Issuer"] = "https://auth.myapp.com",
//                ["Jwt:Audience"] = "https://myapp.com"
//            })
//            .Build();

//        // Act & Assert
//        var exception = Assert.Throws<ArgumentException>(() =>
//            _services.AddJwtAuthentication(invalidConfig));

//        Assert.Contains("JWT key", exception.Message);
//    }

//    [Fact]
//    public void AddJwtAuthentication_ThrowsException_WhenJwtKeyIsTooShort()
//    {
//        // Arrange
//        var invalidConfig = new ConfigurationBuilder()
//            .AddInMemoryCollection(new Dictionary<string, string>
//            {
//                ["Jwt:Key"] = "too-short",
//                ["Jwt:Issuer"] = "https://auth.myapp.com",
//                ["Jwt:Audience"] = "https://myapp.com"
//            })
//            .Build();

//        // Act & Assert
//        var exception = Assert.Throws<ArgumentException>(() =>
//            _services.AddJwtAuthentication(invalidConfig));

//        Assert.Contains("minimum length", exception.Message);
//    }

//    [Fact]
//    public void AddJwtAuthentication_ConfiguresClockSkew()
//    {
//        // Act
//        _services.AddJwtAuthentication(_configuration);

//        // Assert
//        var serviceProvider = _services.BuildServiceProvider();
//        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
//            .Get(JwtBearerDefaults.AuthenticationScheme);
//        var validationParams = options.TokenValidationParameters;

//        // Default clock skew should be 5 minutes
//        Assert.Equal(TimeSpan.FromMinutes(5), validationParams.ClockSkew);
//    }

//    [Fact]
//    public void AddJwtAuthentication_SetsCorrectTokenExpiry()
//    {
//        // Act
//        _services.AddJwtAuthentication(_configuration);

//        // Assert
//        var serviceProvider = _services.BuildServiceProvider();
//        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
//            .Get(JwtBearerDefaults.AuthenticationScheme);

//        Assert.NotNull(options.Events);
//        Assert.NotNull(options.Events.OnTokenValidated);
//    }

//    [Fact]
//    public void AddJwtAuthentication_ConfiguresTokenValidationEvents()
//    {
//        // Act
//        _services.AddJwtAuthentication(_configuration);

//        // Assert
//        var serviceProvider = _services.BuildServiceProvider();
//        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
//            .Get(JwtBearerDefaults.AuthenticationScheme);

//        Assert.NotNull(options.Events);
//        Assert.NotNull(options.Events.OnAuthenticationFailed);
//        Assert.NotNull(options.Events.OnTokenValidated);
//        Assert.NotNull(options.Events.OnChallenge);
//    }
//}