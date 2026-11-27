using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

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

    private static int CountLogInvocationsAtLevel<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel) =>
        mockLogger.Invocations.Count(inv =>
            inv.Method.Name == nameof(ILogger.Log)
            && inv.Arguments.Count > 0
            && inv.Arguments[0] is LogLevel level
            && level == logLevel);

    /// <summary>Verifies that the logger was called with the specified log level the expected number of times.</summary>
    /// <typeparam name="T">The service type associated with the logger.</typeparam>
    /// <param name="mockLogger">The mock logger to verify.</param>
    /// <param name="logLevel">The log level to assert on.</param>
    /// <param name="times">The expected number of invocations.</param>
    protected static void VerifyLoggerCalled<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel, Times times)
    {
        var count = CountLogInvocationsAtLevel(mockLogger, logLevel);

        if (times == Times.Once())
            Assert.Equal(1, count);
        else if (times == Times.AtLeastOnce())
            Assert.True(count >= 1, $"Expected at least one {logLevel} log call; got {count}.");
        else
            throw new NotSupportedException(
                $"VerifyLoggerCalled only supports Times.Once() and Times.AtLeastOnce(); got {times}.");
    }

    /// <summary>Verifies that the logger was called with the specified log level at least the given number of times.</summary>
    /// <typeparam name="T">The service type associated with the logger.</typeparam>
    /// <param name="mockLogger">The mock logger to verify.</param>
    /// <param name="logLevel">The log level to assert on.</param>
    /// <param name="count">The minimum expected number of invocations.</param>
    protected static void VerifyLoggerCalledAtLeast<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel, int count)
    {
        var actual = CountLogInvocationsAtLevel(mockLogger, logLevel);
        Assert.True(actual >= count,
            $"Expected at least {count} {logLevel} log call(s); got {actual}.");
    }
}