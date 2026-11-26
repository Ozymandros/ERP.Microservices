using System.Net;
using FluentAssertions;
using MyApp.Projects.Infrastructure.Adapters;
using Xunit;

namespace MyApp.Projects.Infrastructure.Tests.Adapters;

public class ServiceInvocationErrorsTests
{
    [Fact]
    public void IsNotFound_DirectNotFoundHttpException_ReturnsTrue()
    {
        var ex = new HttpRequestException("missing", null, HttpStatusCode.NotFound);
        ServiceInvocationErrors.IsNotFound(ex).Should().BeTrue();
    }

    [Fact]
    public void IsNotFound_NestedNotFound_ReturnsTrue()
    {
        var inner = new HttpRequestException("missing", null, HttpStatusCode.NotFound);
        var outer = new Exception("wrap", inner);
        ServiceInvocationErrors.IsNotFound(outer).Should().BeTrue();
    }

    [Fact]
    public void IsNotFound_OtherStatus_ReturnsFalse()
    {
        var ex = new HttpRequestException("fail", null, HttpStatusCode.BadGateway);
        ServiceInvocationErrors.IsNotFound(ex).Should().BeFalse();
    }

    [Fact]
    public void IsNotFound_NonHttpException_ReturnsFalse()
    {
        ServiceInvocationErrors.IsNotFound(new InvalidOperationException("x")).Should().BeFalse();
    }
}
