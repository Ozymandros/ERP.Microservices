namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>
/// Represents the Lead Dto data record.
/// </summary>
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
/// Represents the Create Lead Dto data record.
/// </summary>
public sealed record CreateLeadDto(
    string Title,
    string OwnerUsername,
    string? Source,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone
);

/// <summary>
/// Represents the Update Lead Dto data record.
/// </summary>
public sealed record UpdateLeadDto(
    string Title,
    string? Source,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone
);

/// <summary>
/// Represents the Qualify Lead Dto data record.
/// </summary>
public sealed record QualifyLeadDto(
    Guid CustomerId
);

