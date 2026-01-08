using MyApp.Auth.Application.Contracts.DTOs;

namespace MyApp.Auth.Application.Tests.Builders;

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

    public UserDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserDtoBuilder WithEmail(string email)
    {
        _email = email;
        _username = email; // Assuming username follows email
        return this;
    }

    public UserDtoBuilder WithUserName(string userName)
    {
        _username = userName;
        return this;
    }

    public UserDtoBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public UserDtoBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public UserDtoBuilder WithEmailConfirmed(bool confirmed = true)
    {
        _emailConfirmed = confirmed;
        return this;
    }

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
        IsAdmin = _isAdmin
    };

    public static implicit operator UserDto(UserDtoBuilder builder) => builder.Build();
}

public class UpdateUserDtoBuilder
{
    private string? _email = "updated@example.com";
    private string? _firstName = "Updated";
    private string? _lastName = "ApplicationUser";
    private string? _phoneNumber = null;

    public UpdateUserDtoBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UpdateUserDtoBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public UpdateUserDtoBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public UpdateUserDto Build() => new(_email, _firstName, _lastName, _phoneNumber);

    public static implicit operator UpdateUserDto(UpdateUserDtoBuilder builder) => builder.Build();
}