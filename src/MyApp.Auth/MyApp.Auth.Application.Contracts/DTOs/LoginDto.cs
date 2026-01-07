using System.ComponentModel.DataAnnotations;

namespace MyApp.Auth.Application.Contracts.DTOs;

public record LoginDto(
    [param: Required(ErrorMessage = "Email is required")]
    [param: EmailAddress(ErrorMessage = "Invalid email address")]
    string Email,

    [param: Required(ErrorMessage = "Password is required")]
    [param: StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    string Password
);
