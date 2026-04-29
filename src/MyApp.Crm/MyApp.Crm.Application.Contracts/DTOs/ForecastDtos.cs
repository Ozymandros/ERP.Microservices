namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>
/// Represents the Forecast By Stage Dto data record.
/// </summary>
public sealed record ForecastByStageDto(
    string Stage,
    int Count,
    decimal? SumExpectedAmount,
    decimal WeightedAmount
);

/// <summary>
/// Represents the Forecast Summary Dto data record.
/// </summary>
public sealed record ForecastSummaryDto(
    string OwnerUsername,
    DateOnly? FromExpectedCloseDate,
    DateOnly? ToExpectedCloseDate,
    int TotalCount,
    decimal? TotalExpectedAmount,
    decimal TotalWeightedAmount,
    IReadOnlyList<ForecastByStageDto> ByStage
);

