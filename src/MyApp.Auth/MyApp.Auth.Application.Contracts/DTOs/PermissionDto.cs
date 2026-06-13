using MyApp.Shared.Domain.DTOs;

namespace MyApp.Auth.Application.Contracts.DTOs;

/// <summary>
/// Data transfer object for representing a permission with audit information.
/// </summary>
public record PermissionDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>
    /// Gets or sets the module or feature name for this permission.
    /// </summary>
    public string Module { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the action name for this permission.
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of this permission.
    /// </summary>
    public string? Description { get; init; }
}