using System.ComponentModel.DataAnnotations;

namespace MyApp.Auth.Application.Contracts.DTOs;

/// <summary>
/// Register dto.
/// </summary>
/// <param name="Email">The email.</param>
/// <param name="Username">The username.</param>
/// <param name="Password">The password.</param>
/// <param name="PasswordConfirm">The password Confirm.</param>
/// <param name="FirstName">The first Name.</param>
/// <param name="LastName">The last Name.</param>
public record RegisterDto(
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    string Email,

    [Required(ErrorMessage = "Username is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters")]
    string Username,

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    string Password,

    [Required(ErrorMessage = "Password confirmation is required")]
    [property: Compare("Password", ErrorMessage = "Passwords do not match")]
    string PasswordConfirm,

    [StringLength(100)]
    string? FirstName = null,

    [StringLength(100)]
    string? LastName = null
);
