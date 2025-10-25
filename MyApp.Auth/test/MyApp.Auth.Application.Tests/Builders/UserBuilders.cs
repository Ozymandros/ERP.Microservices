using MyApp.Auth.Application.Contracts.DTOs;

namespace MyApp.Auth.Application.Tests.Builders;

public class UserDtoBuilder
{
    private UserDto _dto = new()
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        Username = "test@example.com",
        FirstName = "Test",
        LastName = "ApplicationUser",
        EmailConfirmed = true
    };

    public UserDtoBuilder WithId(Guid id)
    {
        _dto.Id = id;
        return this;
    }

    public UserDtoBuilder WithEmail(string email)
    {
        _dto.Email = email;
        _dto.Username = email; // Assuming username follows email
        return this;
    }

    public UserDtoBuilder WithUserName(string userName)
    {
        _dto.Username = userName;
        return this;
    }

    public UserDtoBuilder WithFirstName(string firstName)
    {
        _dto.FirstName = firstName;
        return this;
    }

    public UserDtoBuilder WithLastName(string lastName)
    {
        _dto.LastName = lastName;
        return this;
    }

    public UserDtoBuilder WithEmailConfirmed(bool confirmed = true)
    {
        _dto.EmailConfirmed = confirmed;
        return this;
    }

    public UserDto Build() => _dto;

    public static implicit operator UserDto(UserDtoBuilder builder) => builder.Build();
}

public class UpdateUserDtoBuilder
{
    private UpdateUserDto _dto = new()
    {
        Email = "updated@example.com",
        FirstName = "Updated",
        LastName = "ApplicationUser"
    };

    public UpdateUserDtoBuilder WithEmail(string email)
    {
        _dto.Email = email;
        return this;
    }

    public UpdateUserDtoBuilder WithFirstName(string firstName)
    {
        _dto.FirstName = firstName;
        return this;
    }

    public UpdateUserDtoBuilder WithLastName(string lastName)
    {
        _dto.LastName = lastName;
        return this;
    }

    public UpdateUserDto Build() => _dto;

    public static implicit operator UpdateUserDto(UpdateUserDtoBuilder builder) => builder.Build();
}