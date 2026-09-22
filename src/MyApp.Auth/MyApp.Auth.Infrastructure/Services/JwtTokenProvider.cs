using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Infrastructure.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MyApp.Auth.Infrastructure.Services;

/// <summary>
/// Defines the contract for generating and validating JWT access tokens and refresh tokens.
/// </summary>
public interface IJwtTokenProvider
{
    /// <summary>
    /// Generates a signed JWT access token for the specified user, embedding the given roles and claims.
    /// </summary>
    /// <param name="user">The user for whom the token is generated.</param>
    /// <param name="roles">Optional list of role names to embed as role claims.</param>
    /// <param name="claims">Optional list of additional claims to embed in the token.</param>
    /// <returns>A signed JWT access token string.</returns>
    Task<string> GenerateAccessTokenAsync(ApplicationUser user, IList<string>? roles = null, IList<Claim>? claims = null);

    /// <summary>
    /// Generates a cryptographically random refresh token string.
    /// </summary>
    /// <returns>A Base64-encoded refresh token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT access token, checking signature, issuer, audience, and lifetime.
    /// </summary>
    /// <param name="token">The JWT token string to validate.</param>
    /// <returns>The <see cref="ClaimsPrincipal"/> from the token if valid; otherwise, <c>null</c>.</returns>
    ClaimsPrincipal? ValidateAccessToken(string token);

    /// <summary>
    /// Extracts the claims principal from an expired JWT token without validating the lifetime.
    /// </summary>
    /// <param name="token">The expired JWT token string.</param>
    /// <returns>The <see cref="ClaimsPrincipal"/> from the token if the signature is valid; otherwise, <c>null</c>.</returns>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

/// <summary>
/// Generates and validates JWT access tokens and refresh tokens using HMAC-SHA256 signing.
/// </summary>
public class JwtTokenProvider : IJwtTokenProvider
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;

    /// <summary>
    /// Initializes a new instance of the JwtTokenProvider class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is <c>null</c>, or when required configuration values are missing.</exception>
    public JwtTokenProvider(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _secretKey = JwtSecretResolver.GetRequiredSecretKey();
        _issuer = configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer");
        _audience = configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience");
        _accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
    }

    /// <summary>
    /// Generate access token asynchronously.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="roles">The roles.</param>
    /// <param name="claims">The claims.</param>
    /// <returns>A signed JWT access token string.</returns>
    public Task<string> GenerateAccessTokenAsync(ApplicationUser user, IList<string>? roles = null, IList<Claim>? claims = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var resultClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim("FirstName", user.FirstName ?? string.Empty),
            new Claim("LastName", user.LastName ?? string.Empty),
            new Claim("EmailConfirmed", user.EmailConfirmed.ToString()),
            new Claim("IsExternalLogin", user.IsExternalLogin.ToString()),
            new Claim("ExternalProvider", user.ExternalProvider ?? string.Empty),
        };

        if (user.UserRoles is not null)
            foreach (var role in user.UserRoles)
            {
                resultClaims?.Add(new Claim(ClaimTypes.Role, role.RoleId.ToString()));
            }

        if (roles is not null)
            foreach (var role in roles)
            {
                resultClaims?.Add(new Claim(ClaimTypes.Role, role.ToString()));
            }

        if (user.UserClaims is not null)
            foreach (var claim in user.UserClaims)
                if (!string.IsNullOrWhiteSpace(claim.ClaimType) && !string.IsNullOrWhiteSpace(claim.ClaimValue))
                {
                    resultClaims?.Add(new Claim(claim.ClaimType, claim.ClaimValue));
                }

        if (claims is not null)
            foreach (var claim in claims)
                if (!string.IsNullOrWhiteSpace(claim.Type) && !string.IsNullOrWhiteSpace(claim.Value))
                {
                    resultClaims?.Add(new Claim(claim.Type, claim.Value));
                }

        // Do not use HashSet<Claim>: Claim equality can collapse distinct role/permission claims and break token creation.
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: resultClaims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials);

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    /// <summary>
    /// Generate refresh token.
    /// </summary>
    /// <returns>A Base64-encoded 32-byte random refresh token.</returns>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    /// <summary>
    /// Validate access token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The <see cref="ClaimsPrincipal"/> from the token if valid; otherwise, <c>null</c>.</returns>
    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        try
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role,
            };

            var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the principal from expired token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The <see cref="ClaimsPrincipal"/> from the token if the signature is valid; otherwise, <c>null</c>.</returns>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        try
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateLifetime = false,
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role,
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
