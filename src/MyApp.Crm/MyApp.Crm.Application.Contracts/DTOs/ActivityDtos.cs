namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>Data transfer object for activity information.</summary>
public sealed record ActivityDto(
    Guid Id,
    string Subject,
    string Type,
    string Status,
    DateTimeOffset DueAt,
    DateTimeOffset? CompletedAt,
    string AssignedToUsername,
    Guid? LeadId,
    Guid? OpportunityId,
    Guid? CustomerId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Data transfer object for creating an activity.</summary>
public sealed record CreateActivityDto(
    string Subject,
    string Type,
    DateTimeOffset DueAt,
    string AssignedToUsername,
    Guid? LeadId,
    Guid? OpportunityId,
    Guid? CustomerId
);

/// <summary>Data transfer object for completing an activity.</summary>
public sealed record CompleteActivityDto(
    string? Note
);

