namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>
/// Represents the Opportunity Dto data record.
/// </summary>
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

/// <summary>
/// Represents the Create Opportunity Dto data record.
/// </summary>
public sealed record CreateOpportunityDto(
    Guid CustomerId,
    string Name,
    string OwnerUsername,
    Guid? LeadId
);

/// <summary>
/// Represents the Update Opportunity Forecast Dto data record.
/// </summary>
public sealed record UpdateOpportunityForecastDto(
    decimal Probability,
    decimal? ExpectedAmount,
    DateOnly? ExpectedCloseDate
);

/// <summary>
/// Represents the Move Opportunity Stage Dto data record.
/// </summary>
public sealed record MoveOpportunityStageDto(
    string Stage
);

/// <summary>
/// Represents the Mark Opportunity Lost Dto data record.
/// </summary>
public sealed record MarkOpportunityLostDto(
    string Reason
);

