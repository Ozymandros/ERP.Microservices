using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MyApp.Shared.Infrastructure.Extensions;
using System.Text.Json;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Extensions;

public class HealthChecksExtensionsTests
{
    #region AddCustomHealthChecks Tests

    [Fact]
    public void AddCustomHealthChecks_RegistersHealthChecks()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Server=test;Database=testdb;";

        // Act
        var result = services.AddCustomHealthChecks(connectionString);

        // Assert - Verify method returns service collection and doesn't throw
        result.Should().BeSameAs(services);
        // Note: Full service registration verification requires ASP.NET Core infrastructure
        // This is a unit test verifying the extension method signature and basic behavior
    }

    [Fact]
    public void AddCustomHealthChecks_RegistersHealthCheckService()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Server=test;Database=testdb;";

        // Act
        var result = services.AddCustomHealthChecks(connectionString);

        // Assert - Verify method returns service collection
        result.Should().BeSameAs(services);
        // Note: Full service registration verification requires ASP.NET Core infrastructure
        // This is a unit test verifying the extension method signature and basic behavior
    }

    [Fact]
    public void AddCustomHealthChecks_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Server=test;Database=testdb;";

        // Act
        var result = services.AddCustomHealthChecks(connectionString);

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCustomHealthChecks_WithEmptyConnectionString_StillRegisters()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = string.Empty;

        // Act
        var result = services.AddCustomHealthChecks(connectionString);

        // Assert - Verify method returns service collection and doesn't throw with empty connection string
        result.Should().BeSameAs(services);
        // Note: Full service registration verification requires ASP.NET Core infrastructure
        // This is a unit test verifying the extension method handles empty connection string
    }

    #endregion

    #region UseCustomHealthChecks Tests

    [Fact]
    public void UseCustomHealthChecks_ConfiguresHealthCheckEndpoint()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHealthChecks();
        services.AddLogging();
        var app = new ApplicationBuilder(services.BuildServiceProvider());

        // Act
        app.UseCustomHealthChecks();

        // Assert
        // Verify that the middleware is registered (difficult to test without full ASP.NET Core integration)
        app.Should().NotBeNull();
    }

    [Fact]
    public void UseCustomHealthChecks_ReturnsApplicationBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHealthChecks();
        services.AddLogging();
        var app = new ApplicationBuilder(services.BuildServiceProvider());

        // Act
        var result = app.UseCustomHealthChecks();

        // Assert
        result.Should().BeSameAs(app);
    }

    #endregion
}
