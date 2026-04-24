namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>Data transfer object for activity information.</summary>
public sealed record ActivityDto(
    /// <summary>The activity ID.</summary>
    Guid Id,
    /// <summary>The activity subject.</summary>
    string Subject,
    /// <summary>The activity type.</summary>
    string Type,
    /// <summary>The activity status.</summary>
    string Status,
    /// <summary>The due date and time.</summary>
    DateTimeOffset DueAt,
    /// <summary>The completion date and time.</summary>
    DateTimeOffset? CompletedAt,
    /// <summary>The username of the assigned user.</summary>
    string AssignedToUsername,
    /// <summary>The linked lead ID.</summary>
    Guid? LeadId,
    /// <summary>The linked opportunity ID.</summary>
    Guid? OpportunityId,
    /// <summary>The linked customer ID.</summary>
    Guid? CustomerId,
    /// <summary>The creation date.</summary>
    DateTime CreatedAt,
    /// <summary>The last update date.</summary>
    DateTime? UpdatedAt
);

/// <summary>Data transfer object for creating an activity.</summary>
public sealed record CreateActivityDto(
    /// <summary>The activity subject.</summary>
    string Subject,
    /// <summary>The activity type.</summary>
    string Type,
    /// <summary>The due date and time.</summary>
    DateTimeOffset DueAt,
    /// <summary>The username to assign the activity to.</summary>
    string AssignedToUsername,
    /// <summary>The linked lead ID.</summary>
    Guid? LeadId,
    /// <summary>The linked opportunity ID.</summary>
    Guid? OpportunityId,
    /// <summary>The linked customer ID.</summary>
    Guid? CustomerId
);

/// <summary>Data transfer object for completing an activity.</summary>
public sealed record CompleteActivityDto(
    /// <summary>An optional completion note.</summary>
    string? Note
);

