using System.Net;

namespace MyApp.Projects.Infrastructure.Adapters;

internal static class ServiceInvocationErrors
{
    public static bool IsNotFound(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException { StatusCode: HttpStatusCode.NotFound })
                return true;
        }

        return false;
    }
}