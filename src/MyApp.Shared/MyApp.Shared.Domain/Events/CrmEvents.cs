namespace MyApp.Shared.Domain.Events;

public record CrmLeadCreatedEvent(
    Guid LeadId,
    string Title,
    string OwnerUsername,
    string? Source
);

public record CrmLeadUpdatedEvent(
    Guid LeadId,
    string Title,
    string OwnerUsername,
    string? Source
);

public record CrmLeadQualifiedEvent(
    Guid LeadId,
    Guid CustomerId
);

public record CrmOpportunityCreatedEvent(
    Guid OpportunityId,
    Guid CustomerId,
    string Name,
    string OwnerUsername
);

public record CrmOpportunityStageChangedEvent(
    Guid OpportunityId,
    string OldStage,
    string NewStage
);

public record CrmOpportunityWonEvent(
    Guid OpportunityId,
    Guid CustomerId
);

public record CrmOpportunityLostEvent(
    Guid OpportunityId,
    Guid CustomerId,
    string Reason
);

public record CrmActivityCreatedEvent(
    Guid ActivityId,
    string Type,
    string Subject,
    DateTimeOffset DueAt,
    string AssignedToUsername
);

public record CrmActivityCompletedEvent(
    Guid ActivityId,
    DateTimeOffset CompletedAt
);

