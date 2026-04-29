namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Event raised when a CRM lead is created.
/// </summary>
public record CrmLeadCreatedEvent(
    Guid LeadId,
    string Title,
    string OwnerUsername,
    string? Source
);

/// <summary>
/// Event raised when a CRM lead is updated.
/// </summary>
public record CrmLeadUpdatedEvent(
    Guid LeadId,
    string Title,
    string OwnerUsername,
    string? Source
);

/// <summary>
/// Event raised when a CRM lead is qualified into a customer.
/// </summary>
public record CrmLeadQualifiedEvent(
    Guid LeadId,
    Guid CustomerId
);

/// <summary>
/// Event raised when a CRM opportunity is created.
/// </summary>
public record CrmOpportunityCreatedEvent(
    Guid OpportunityId,
    Guid CustomerId,
    string Name,
    string OwnerUsername
);

/// <summary>
/// Event raised when a CRM opportunity stage changes.
/// </summary>
public record CrmOpportunityStageChangedEvent(
    Guid OpportunityId,
    string OldStage,
    string NewStage
);

/// <summary>
/// Event raised when a CRM opportunity is won.
/// </summary>
public record CrmOpportunityWonEvent(
    Guid OpportunityId,
    Guid CustomerId
);

/// <summary>
/// Event raised when a CRM opportunity is lost.
/// </summary>
public record CrmOpportunityLostEvent(
    Guid OpportunityId,
    Guid CustomerId,
    string Reason
);

/// <summary>
/// Event raised when a CRM activity is created.
/// </summary>
public record CrmActivityCreatedEvent(
    Guid ActivityId,
    string Type,
    string Subject,
    DateTimeOffset DueAt,
    string AssignedToUsername
);

/// <summary>
/// Event raised when a CRM activity is completed.
/// </summary>
public record CrmActivityCompletedEvent(
    Guid ActivityId,
    DateTimeOffset CompletedAt
);

