namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>
/// Opportunity dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="LeadId">The lead Id.</param>
/// <param name="Name">The name.</param>
/// <param name="Stage">The stage.</param>
/// <param name="Probability">The probability.</param>
/// <param name="ExpectedAmount">The expected Amount.</param>
/// <param name="ExpectedCloseDate">The expected Close Date.</param>
/// <param name="ConvertedSalesQuoteId">The converted Sales Quote Id.</param>
/// <param name="ConvertedSalesQuoteNumber">The converted Sales Quote Number.</param>
/// <param name="OwnerUsername">The owner Username.</param>
/// <param name="CreatedAt">The created At.</param>
/// <param name="UpdatedAt">The updated At.</param>
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
/// Creates an opportunity dto.
/// </summary>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="Name">The name.</param>
/// <param name="OwnerUsername">The owner Username.</param>
/// <param name="LeadId">The lead Id.</param>
public sealed record CreateOpportunityDto(
    Guid CustomerId,
    string Name,
    string OwnerUsername,
    Guid? LeadId
);

/// <summary>
/// Updates the opportunity forecast dto.
/// </summary>
/// <param name="Probability">The probability.</param>
/// <param name="ExpectedAmount">The expected Amount.</param>
/// <param name="ExpectedCloseDate">The expected Close Date.</param>
public sealed record UpdateOpportunityForecastDto(
    decimal Probability,
    decimal? ExpectedAmount,
    DateOnly? ExpectedCloseDate
);

/// <summary>
/// Move opportunity stage dto.
/// </summary>
/// <param name="Stage">The stage.</param>
public sealed record MoveOpportunityStageDto(
    string Stage
);

/// <summary>
/// Mark opportunity lost dto.
/// </summary>
/// <param name="Reason">The reason.</param>
public sealed record MarkOpportunityLostDto(
    string Reason
);

