using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MyApp.Auth.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MyApp.Auth.Infrastructure.Services;

public interface IJwtTokenProvider
{
    Task<string> GenerateAccessTokenAsync(ApplicationUser user, IList<string>? roles = null, IList<Claim>? claims = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

public class JwtTokenProvider : IJwtTokenProvider
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;

    public JwtTokenProvider(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:SecretKey"] ?? throw new ArgumentNullException(nameof(configuration), "Jwt:SecretKey is required");
        _issuer = configuration["Jwt:Issuer"] ?? throw new ArgumentNullException(nameof(configuration), "Jwt:Issuer is required");
        _audience = configuration["Jwt:Audience"] ?? throw new ArgumentNullException(nameof(configuration), "Jwt:Audience is required");
        _accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
    }

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

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: resultClaims?.ToHashSet(),
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials);

        var result = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult(result);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

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
