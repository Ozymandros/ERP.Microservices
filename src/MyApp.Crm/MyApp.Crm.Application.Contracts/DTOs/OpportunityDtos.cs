namespace MyApp.Crm.Application.Contracts.DTOs;

public sealed record OpportunityDto(
    Guid Id,
    Guid CustomerId,
    Guid? LeadId,
    string Name,
    string Stage,
    decimal Probability,
    decimal? ExpectedAmount,
    DateOnly? ExpectedCloseDate,
    Guid? ConvertedSalesQuoteId,
    string? ConvertedSalesQuoteNumber,
    string OwnerUsername,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record CreateOpportunityDto(
    Guid CustomerId,
    string Name,
    string OwnerUsername,
    Guid? LeadId
);

public sealed record UpdateOpportunityForecastDto(
    decimal Probability,
    decimal? ExpectedAmount,
    DateOnly? ExpectedCloseDate
);

public sealed record MoveOpportunityStageDto(
    string Stage
);

public sealed record MarkOpportunityLostDto(
    string Reason
);

