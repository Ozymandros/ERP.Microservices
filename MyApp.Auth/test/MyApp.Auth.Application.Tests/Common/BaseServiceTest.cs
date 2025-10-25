using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;

namespace MyApp.Auth.Application.Tests.Common;

public abstract class BaseServiceTest
{
    protected readonly Mock<IMapper> MockMapper;
    protected readonly IMapper Mapper;

    protected BaseServiceTest()
    {
        MockMapper = new Mock<IMapper>();
        Mapper = MockMapper.Object;
    }

    protected static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

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