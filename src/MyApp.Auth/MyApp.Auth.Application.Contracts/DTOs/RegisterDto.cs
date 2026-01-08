using System.ComponentModel.DataAnnotations;

namespace MyApp.Auth.Application.Contracts.DTOs;

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
