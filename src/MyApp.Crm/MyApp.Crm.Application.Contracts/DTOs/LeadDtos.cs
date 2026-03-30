namespace MyApp.Crm.Application.Contracts.DTOs;

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

public sealed record CreateLeadDto(
    string Title,
    string OwnerUsername,
    string? Source,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone
);

public sealed record UpdateLeadDto(
    string Title,
    string? Source,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone
);

public sealed record QualifyLeadDto(
    Guid CustomerId
);

