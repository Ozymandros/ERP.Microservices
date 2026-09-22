using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;

namespace MyApp.Inventory.Application.Tests.Common;

/// <summary>Base class for service unit tests, providing shared mock infrastructure such as an AutoMapper mock.</summary>
public abstract class BaseServiceTest
{
    /// <summary>Gets the mock AutoMapper instance for configuring mapping expectations.</summary>
    protected readonly Mock<IMapper> MockMapper;
    /// <summary>Gets the concrete <see cref="IMapper"/> proxy backed by <see cref="MockMapper"/>.</summary>
    protected readonly IMapper Mapper;

    /// <summary>Initialises mock infrastructure shared across all derived test classes.</summary>
    protected BaseServiceTest()
    {
        MockMapper = new Mock<IMapper>();
        Mapper = MockMapper.Object;
    }

    /// <summary>Creates a new mock logger for the specified service type.</summary>
    /// <typeparam name="T">The service type whose logger is being mocked.</typeparam>
    /// <returns>A <see cref="Mock{ILogger}"/> instance for <typeparamref name="T"/>.</returns>
    protected static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>Verifies that the logger was called with the specified log level the expected number of times.</summary>
    /// <typeparam name="T">The service type associated with the logger.</typeparam>
    /// <param name="mockLogger">The mock logger to verify.</param>
    /// <param name="logLevel">The log level to assert on.</param>
    /// <param name="times">The expected number of invocations.</param>
    protected static void VerifyLoggerCalled<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel, Times times)
    {
        mockLogger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    /// <summary>Verifies that the logger was called with the specified log level at least the given number of times.</summary>
    /// <typeparam name="T">The service type associated with the logger.</typeparam>
    /// <param name="mockLogger">The mock logger to verify.</param>
    /// <param name="logLevel">The log level to assert on.</param>
    /// <param name="count">The minimum expected number of invocations.</param>
    protected static void VerifyLoggerCalledAtLeast<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel, int count)
    {
        mockLogger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(count));
    }
}
