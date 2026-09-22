namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>
/// Lead dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Title">The title.</param>
/// <param name="Source">The source.</param>
/// <param name="ContactName">The contact Name.</param>
/// <param name="ContactEmail">The contact Email.</param>
/// <param name="ContactPhone">The contact Phone.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="Status">The status.</param>
/// <param name="OwnerUsername">The owner Username.</param>
/// <param name="CreatedAt">The created At.</param>
/// <param name="UpdatedAt">The updated At.</param>
public sealed record LeadDto(
    Guid Id,
    string Title,
    string? Source,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    Guid? CustomerId,
    string Status,
    string OwnerUsername,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>
/// Creates a lead dto.
/// </summary>
/// <param name="Title">The title.</param>
/// <param name="OwnerUsername">The owner Username.</param>
/// <param name="Source">The source.</param>
/// <param name="ContactName">The contact Name.</param>
/// <param name="ContactEmail">The contact Email.</param>
/// <param name="ContactPhone">The contact Phone.</param>
public sealed record CreateLeadDto(
    string Title,
    string OwnerUsername,
    string? Source,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone
);

/// <summary>
/// Updates the lead dto.
/// </summary>
/// <param name="Title">The title.</param>
/// <param name="Source">The source.</param>
/// <param name="ContactName">The contact Name.</param>
/// <param name="ContactEmail">The contact Email.</param>
/// <param name="ContactPhone">The contact Phone.</param>
public sealed record UpdateLeadDto(
    string Title,
    string? Source,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone
);

/// <summary>
/// Qualify lead dto.
/// </summary>
/// <param name="CustomerId">The customer Id.</param>
public sealed record QualifyLeadDto(
    Guid CustomerId
);

