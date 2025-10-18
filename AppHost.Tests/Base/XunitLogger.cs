using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

public class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _categoryName;

    public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
    {
        _testOutputHelper = testOutputHelper;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        string message = $"{logLevel}: [{_categoryName}] {formatter(state, exception)}";
        if (exception != null)
        {
            message += $"{Environment.NewLine}{exception}";
        }
        _testOutputHelper.WriteLine(message);
    }
}
