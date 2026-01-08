using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Infrastructure.Services;
using Xunit;

namespace MyApp.Auth.Tests.Services;

public class JwtTokenProviderTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly IJwtTokenProvider _tokenProvider;
    private readonly string _testSecretKey = "ThisIsAVeryLongSecretKeyForTestingJwtTokenGeneration";
    private readonly string _testIssuer = "TestIssuer";
    private readonly string _testAudience = "TestAudience";
    private const int TestAccessTokenExpirationMinutes = 15;

    public JwtTokenProviderTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        SetupConfigurationMock();
        _tokenProvider = new JwtTokenProvider(_configurationMock.Object);
    }

    private void SetupConfigurationMock()
    {
        _configurationMock.Setup(x => x["Jwt:SecretKey"]).Returns(_testSecretKey);
        _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns(_testIssuer);
        _configurationMock.Setup(x => x["Jwt:Audience"]).Returns(_testAudience);
        _configurationMock.Setup(x => x["Jwt:AccessTokenExpirationMinutes"]).Returns(TestAccessTokenExpirationMinutes.ToString());
    }

    #region GenerateAccessTokenAsync Tests

    [Fact]
    public async Task GenerateAccessTokenAsync_WithValidUser_ReturnsValidToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var token = await _tokenProvider.GenerateAccessTokenAsync(user, null, null);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.IsType<string>(token);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_GeneratedTokenIsValidJwt_CanBeDecoded()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var token = await _tokenProvider.GenerateAccessTokenAsync(user);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert
        Assert.NotNull(jsonToken);
        Assert.Equal(_testIssuer, jsonToken.Issuer);
        Assert.Equal(_testAudience, jsonToken.Audiences.First());
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_TokenContainsCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userEmail = "test@example.com";
        var userName = "testuser";
        var firstName = "Test";
        var lastName = "User";

        var user = new ApplicationUser
        {
            Id = userId,
            Email = userEmail,
            UserName = userName,
            FirstName = firstName,
            LastName = lastName
        };

        // Act
        var token = await _tokenProvider.GenerateAccessTokenAsync(user);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert
        Assert.NotNull(jsonToken);
        Assert.Contains(jsonToken.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        Assert.Contains(jsonToken.Claims, c => c.Type == ClaimTypes.Email && c.Value == userEmail);
        Assert.Contains(jsonToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == userName);
        Assert.Contains(jsonToken.Claims, c => c.Type == "FirstName" && c.Value == firstName);
        Assert.Contains(jsonToken.Claims, c => c.Type == "LastName" && c.Value == lastName);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_TokenExpirationIsCorrect()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser"
        };
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = await _tokenProvider.GenerateAccessTokenAsync(user);
        var afterGeneration = DateTime.UtcNow;
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert
        Assert.NotNull(jsonToken?.ValidTo);
        var expectedExpiration = beforeGeneration.AddMinutes(TestAccessTokenExpirationMinutes);
        var actualExpiration = jsonToken.ValidTo;
        
        // Allow 1 second tolerance for execution time
        Assert.True(Math.Abs((actualExpiration - expectedExpiration).TotalSeconds) < 1);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_WithNullEmail_StillGeneratesValidToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = null,
            UserName = "testuser"
        };

        // Act
        var token = await _tokenProvider.GenerateAccessTokenAsync(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    #endregion

    #region GenerateRefreshToken Tests

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        // Act
        var refreshToken = _tokenProvider.GenerateRefreshToken();

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64EncodedString()
    {
        // Act
        var refreshToken = _tokenProvider.GenerateRefreshToken();

        // Assert
        try
        {
            var decodedBytes = Convert.FromBase64String(refreshToken);
            Assert.NotNull(decodedBytes);
            Assert.Equal(32, decodedBytes.Length); // Random number generator creates 32 bytes
        }
        catch (FormatException)
        {
            throw; // Fail the test if not a valid base64 string
        }
    }

    [Fact]
    public void GenerateRefreshToken_GeneratesDifferentTokensEachTime()
    {
        // Act
        var token1 = _tokenProvider.GenerateRefreshToken();
        var token2 = _tokenProvider.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateRefreshToken_TokenIsOfExpectedLength()
    {
        // Act
        var refreshToken = _tokenProvider.GenerateRefreshToken();

        // Assert
        // 32 bytes encoded to base64 = 44 characters (32 * 4 / 3 rounded up)
        Assert.Equal(44, refreshToken.Length);
    }

    #endregion

    #region GetPrincipalFromExpiredToken Tests

    [Fact]
    public async Task GetPrincipalFromExpiredToken_WithValidExpiredToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User"
        };
        var token = await _tokenProvider.GenerateAccessTokenAsync(user);

        // Act
        var principal = _tokenProvider.GetPrincipalFromExpiredToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.IsType<ClaimsPrincipal>(principal);
    }

    [Fact]
    public async Task GetPrincipalFromExpiredToken_WithValidToken_ContainsUserClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User"
        };
        var token = await _tokenProvider.GenerateAccessTokenAsync(user);

        // Act
        var principal = _tokenProvider.GetPrincipalFromExpiredToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
        Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Name && c.Value == "testuser");
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.format";

        // Act
        var principal = _tokenProvider.GetPrincipalFromExpiredToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithMalformedToken_ReturnsNull()
    {
        // Arrange
        var malformedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.signature";

        // Act
        var principal = _tokenProvider.GetPrincipalFromExpiredToken(malformedToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithWrongSigningAlgorithm_ReturnsNull()
    {
        // Arrange
        // Use a key that's at least 64 bytes (512 bits) for HS512
        var keyBytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(keyBytes);
        var secretKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha512);
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "test") };
        var token = new JwtSecurityToken(
            issuer: _testIssuer,
            audience: _testAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: credentials);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Act
        var principal = _tokenProvider.GetPrincipalFromExpiredToken(tokenString);

        // Assert
        Assert.Null(principal);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Constructor_WithMissingSecretKey_ThrowsArgumentNullException()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Jwt:SecretKey"]).Returns((string)null!);
        configMock.Setup(x => x["Jwt:Issuer"]).Returns(_testIssuer);
        configMock.Setup(x => x["Jwt:Audience"]).Returns(_testAudience);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new JwtTokenProvider(configMock.Object));
        Assert.Contains("Jwt:SecretKey", ex.Message);
    }

    [Fact]
    public void Constructor_WithMissingIssuer_ThrowsArgumentNullException()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Jwt:SecretKey"]).Returns(_testSecretKey);
        configMock.Setup(x => x["Jwt:Issuer"]).Returns((string)null!);
        configMock.Setup(x => x["Jwt:Audience"]).Returns(_testAudience);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new JwtTokenProvider(configMock.Object));
        Assert.Contains("Jwt:Issuer", ex.Message);
    }

    [Fact]
    public void Constructor_WithMissingAudience_ThrowsArgumentNullException()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Jwt:SecretKey"]).Returns(_testSecretKey);
        configMock.Setup(x => x["Jwt:Issuer"]).Returns(_testIssuer);
        configMock.Setup(x => x["Jwt:Audience"]).Returns((string)null!);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new JwtTokenProvider(configMock.Object));
        Assert.Contains("Jwt:Audience", ex.Message);
    }

    #endregion
}
