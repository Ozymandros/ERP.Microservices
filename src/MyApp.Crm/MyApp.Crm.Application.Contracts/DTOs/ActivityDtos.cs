namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>Data transfer object for activity information.</summary>
/// <param name="Id">The id.</param>
/// <param name="Subject">The subject.</param>
/// <param name="Type">The type.</param>
/// <param name="Status">The status.</param>
/// <param name="DueAt">The due At.</param>
/// <param name="CompletedAt">The completed At.</param>
/// <param name="AssignedToUsername">The assigned To Username.</param>
/// <param name="LeadId">The lead Id.</param>
/// <param name="OpportunityId">The opportunity Id.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="CreatedAt">The created At.</param>
/// <param name="UpdatedAt">The updated At.</param>
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
/// <param name="Subject">The subject.</param>
/// <param name="Type">The type.</param>
/// <param name="DueAt">The due At.</param>
/// <param name="AssignedToUsername">The assigned To Username.</param>
/// <param name="LeadId">The lead Id.</param>
/// <param name="OpportunityId">The opportunity Id.</param>
/// <param name="CustomerId">The customer Id.</param>
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
/// <param name="Note">The note.</param>
public sealed record CompleteActivityDto(
    string? Note
);

