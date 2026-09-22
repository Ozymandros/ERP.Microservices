using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;

namespace MyApp.Auth.Application.Tests.Common;

/// <summary>
/// Abstract base class for application-service unit tests, providing shared AutoMapper and logger mock infrastructure.
/// </summary>
public abstract class BaseServiceTest
{
    /// <summary>Gets the mock <see cref="IMapper"/> instance for configuring mapping expectations.</summary>
    protected readonly Mock<IMapper> MockMapper;

    /// <summary>Gets the <see cref="IMapper"/> proxy backed by <see cref="MockMapper"/>.</summary>
    protected readonly IMapper Mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseServiceTest"/> class with a fresh mapper mock.
    /// </summary>
    protected BaseServiceTest()
    {
        MockMapper = new Mock<IMapper>();
        Mapper = MockMapper.Object;
    }

    /// <summary>
    /// Creates a new mock logger for the specified service type.
    /// </summary>
    /// <typeparam name="T">The type whose logger mock is being created.</typeparam>
    /// <returns>A new <see cref="Mock{T}"/> of <see cref="ILogger{TCategoryName}"/>.</returns>
    protected static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Verifies that a structured log message was emitted at the specified log level the expected number of times.
    /// </summary>
    /// <typeparam name="T">The category type of the logger under test.</typeparam>
    /// <param name="mockLogger">The mock logger to verify against.</param>
    /// <param name="logLevel">The expected log level.</param>
    /// <param name="times">The expected invocation count.</param>
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
}