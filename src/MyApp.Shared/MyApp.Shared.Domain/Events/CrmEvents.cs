namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Crm lead created event.
/// </summary>
/// <param name="LeadId">The lead Id.</param>
/// <param name="Title">The title.</param>
/// <param name="OwnerUsername">The owner Username.</param>
/// <param name="Source">The source.</param>
public record CrmLeadCreatedEvent(
    Guid LeadId,
    string Title,
    string OwnerUsername,
    string? Source
);

/// <summary>
/// Crm lead updated event.
/// </summary>
/// <param name="LeadId">The lead Id.</param>
/// <param name="Title">The title.</param>
/// <param name="OwnerUsername">The owner Username.</param>
/// <param name="Source">The source.</param>
public record CrmLeadUpdatedEvent(
    Guid LeadId,
    string Title,
    string OwnerUsername,
    string? Source
);

/// <summary>
/// Crm lead qualified event.
/// </summary>
/// <param name="LeadId">The lead Id.</param>
/// <param name="CustomerId">The customer Id.</param>
public record CrmLeadQualifiedEvent(
    Guid LeadId,
    Guid CustomerId
);

/// <summary>
/// Crm opportunity created event.
/// </summary>
/// <param name="OpportunityId">The opportunity Id.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="Name">The name.</param>
/// <param name="OwnerUsername">The owner Username.</param>
public record CrmOpportunityCreatedEvent(
    Guid OpportunityId,
    Guid CustomerId,
    string Name,
    string OwnerUsername
);

/// <summary>
/// Crm opportunity stage changed event.
/// </summary>
/// <param name="OpportunityId">The opportunity Id.</param>
/// <param name="OldStage">The old Stage.</param>
/// <param name="NewStage">The new Stage.</param>
public record CrmOpportunityStageChangedEvent(
    Guid OpportunityId,
    string OldStage,
    string NewStage
);

/// <summary>
/// Crm opportunity won event.
/// </summary>
/// <param name="OpportunityId">The opportunity Id.</param>
/// <param name="CustomerId">The customer Id.</param>
public record CrmOpportunityWonEvent(
    Guid OpportunityId,
    Guid CustomerId
);

/// <summary>
/// Crm opportunity lost event.
/// </summary>
/// <param name="OpportunityId">The opportunity Id.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="Reason">The reason.</param>
public record CrmOpportunityLostEvent(
    Guid OpportunityId,
    Guid CustomerId,
    string Reason
);

/// <summary>
/// Crm activity created event.
/// </summary>
/// <param name="ActivityId">The activity Id.</param>
/// <param name="Type">The type.</param>
/// <param name="Subject">The subject.</param>
/// <param name="DueAt">The due At.</param>
/// <param name="AssignedToUsername">The assigned To Username.</param>
public record CrmActivityCreatedEvent(
    Guid ActivityId,
    string Type,
    string Subject,
    DateTimeOffset DueAt,
    string AssignedToUsername
);

/// <summary>
/// Crm activity completed event.
/// </summary>
/// <param name="ActivityId">The activity Id.</param>
/// <param name="CompletedAt">The completed At.</param>
public record CrmActivityCompletedEvent(
    Guid ActivityId,
    DateTimeOffset CompletedAt
);

