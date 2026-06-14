using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace MyApp.Auth.API.Authorization;

/// <summary>
/// Requires JWT Bearer authentication. Use on API controllers instead of [Authorize] because
/// AddIdentity sets the default scheme to cookies, which ignores Authorization headers.
/// </summary>
public sealed class AuthorizeJwtAttribute : AuthorizeAttribute
{
    public AuthorizeJwtAttribute()
    {
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
    }
}
