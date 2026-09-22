using System.ComponentModel.DataAnnotations;

namespace MyApp.Auth.Application.Contracts.DTOs;

/// <summary>
/// Login dto.
/// </summary>
/// <param name="Email">The email.</param>
/// <param name="Password">The password.</param>
public record LoginDto(
    [param: Required(ErrorMessage = "Email is required")]
    [param: EmailAddress(ErrorMessage = "Invalid email address")]
    string Email,

    [param: Required(ErrorMessage = "Password is required")]
    [param: StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    string Password
);
