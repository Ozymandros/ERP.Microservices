namespace MyApp.Crm.Application.Contracts.DTOs;

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

public sealed record CreateActivityDto(
    string Subject,
    string Type,
    DateTimeOffset DueAt,
    string AssignedToUsername,
    Guid? LeadId,
    Guid? OpportunityId,
    Guid? CustomerId
);

public sealed record CompleteActivityDto(
    string? Note
);

