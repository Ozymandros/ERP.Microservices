using System.ComponentModel.DataAnnotations;

namespace MyApp.Auth.Application.Contracts.DTOs;

public record RegisterDto(
    [property: Required(ErrorMessage = "Email is required")]
    [property: EmailAddress(ErrorMessage = "Invalid email address")]
    string Email,
    
    [property: Required(ErrorMessage = "Username is required")]
    [property: StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters")]
    string Username,
    
    [property: Required(ErrorMessage = "Password is required")]
    [property: StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    string Password,
    
    [property: Required(ErrorMessage = "Password confirmation is required")]
    [property: Compare("Password", ErrorMessage = "Passwords do not match")]
    string PasswordConfirm,
    
    [property: StringLength(100)]
    string? FirstName = null,
    
    [property: StringLength(100)]
    string? LastName = null
);
