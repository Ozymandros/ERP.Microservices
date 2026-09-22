using MyApp.Auth.Application.Contracts.DTOs;

namespace MyApp.Auth.Application.Tests.Builders;

/// <summary>
/// Builder for constructing <see cref="UserDto"/> test fixtures using a fluent API.
/// </summary>
public class UserDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private DateTime _createdAt = DateTime.UtcNow;
    private string _createdBy = "TestUser";
    private DateTime? _updatedAt = null;
    private string? _updatedBy = null;
    private string? _email = "test@example.com";
    private string? _username = "test@example.com";
    private string? _firstName = "Test";
    private string? _lastName = "ApplicationUser";
    private bool _emailConfirmed = true;
    private bool _isExternalLogin = false;
    private string? _externalProvider = null;
    private List<RoleDto?>? _roles = null;
    private List<PermissionDto?>? _permissions = null;
    private bool _isAdmin = false;

    /// <summary>
    /// Sets the user DTO's unique identifier.
    /// </summary>
    /// <param name="id">The identifier to assign.</param>
    /// <returns>The current <see cref="UserDtoBuilder"/> instance for chaining.</returns>
    public UserDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the user DTO's email address and username.
    /// </summary>
    /// <param name="email">The email address to assign.</param>
    /// <returns>The current <see cref="UserDtoBuilder"/> instance for chaining.</returns>
    public UserDtoBuilder WithEmail(string email)
    {
        _email = email;
        _username = email; // Assuming username follows email
        return this;
    }

    /// <summary>
    /// Sets the user DTO's username independently of the email address.
    /// </summary>
    /// <param name="userName">The username to assign.</param>
    /// <returns>The current <see cref="UserDtoBuilder"/> instance for chaining.</returns>
    public UserDtoBuilder WithUserName(string userName)
    {
        _username = userName;
        return this;
    }

    /// <summary>
    /// Sets the user DTO's first name.
    /// </summary>
    /// <param name="firstName">The first name to assign.</param>
    /// <returns>The current <see cref="UserDtoBuilder"/> instance for chaining.</returns>
    public UserDtoBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    /// <summary>
    /// Sets the user DTO's last name.
    /// </summary>
    /// <param name="lastName">The last name to assign.</param>
    /// <returns>The current <see cref="UserDtoBuilder"/> instance for chaining.</returns>
    public UserDtoBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    /// <summary>
    /// Sets whether the user DTO's email address has been confirmed.
    /// </summary>
    /// <param name="confirmed">Whether the email is confirmed (default: <c>true</c>).</param>
    /// <returns>The current <see cref="UserDtoBuilder"/> instance for chaining.</returns>
    public UserDtoBuilder WithEmailConfirmed(bool confirmed = true)
    {
        _emailConfirmed = confirmed;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="UserDto"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="UserDto"/>.</returns>
    public UserDto Build() => new(_id)
    {
        CreatedAt = _createdAt,
        CreatedBy = _createdBy,
        UpdatedAt = _updatedAt,
        UpdatedBy = _updatedBy,
        Email = _email,
        Username = _username,
        FirstName = _firstName,
        LastName = _lastName,
        EmailConfirmed = _emailConfirmed,
        IsExternalLogin = _isExternalLogin,
        ExternalProvider = _externalProvider,
        Roles = _roles ?? new List<RoleDto?>(),
        Permissions = _permissions ?? new List<PermissionDto?>(),
        IsAdmin = _isAdmin,
        IsActive = true
    };

    /// <summary>
    /// Implicitly converts a <see cref="UserDtoBuilder"/> to a <see cref="UserDto"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="UserDto"/>.</returns>
    public static implicit operator UserDto(UserDtoBuilder builder) => builder.Build();
}

/// <summary>
/// Builder for constructing <see cref="UpdateUserDto"/> test fixtures using a fluent API.
/// </summary>
public class UpdateUserDtoBuilder
{
    private string? _email = "updated@example.com";
    private string? _firstName = "Updated";
    private string? _lastName = "ApplicationUser";
    private string? _phoneNumber = null;

    /// <summary>
    /// Sets the updated email address.
    /// </summary>
    /// <param name="email">The email address to assign.</param>
    /// <returns>The current <see cref="UpdateUserDtoBuilder"/> instance for chaining.</returns>
    public UpdateUserDtoBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    /// <summary>
    /// Sets the updated first name.
    /// </summary>
    /// <param name="firstName">The first name to assign.</param>
    /// <returns>The current <see cref="UpdateUserDtoBuilder"/> instance for chaining.</returns>
    public UpdateUserDtoBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    /// <summary>
    /// Sets the updated last name.
    /// </summary>
    /// <param name="lastName">The last name to assign.</param>
    /// <returns>The current <see cref="UpdateUserDtoBuilder"/> instance for chaining.</returns>
    public UpdateUserDtoBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="UpdateUserDto"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="UpdateUserDto"/>.</returns>
    public UpdateUserDto Build() => new(_email, _firstName, _lastName, _phoneNumber);

    /// <summary>
    /// Implicitly converts an <see cref="UpdateUserDtoBuilder"/> to an <see cref="UpdateUserDto"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="UpdateUserDto"/>.</returns>
    public static implicit operator UpdateUserDto(UpdateUserDtoBuilder builder) => builder.Build();
}