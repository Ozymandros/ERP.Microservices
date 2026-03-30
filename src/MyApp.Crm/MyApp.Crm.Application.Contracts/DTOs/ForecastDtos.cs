namespace MyApp.Crm.Application.Contracts.DTOs;

public sealed record ForecastByStageDto(
    string Stage,
    int Count,
    decimal? SumExpectedAmount,
    decimal WeightedAmount
);

public sealed record ForecastSummaryDto(
    string OwnerUsername,
    DateOnly? FromExpectedCloseDate,
    DateOnly? ToExpectedCloseDate,
    int TotalCount,
    decimal? TotalExpectedAmount,
    decimal TotalWeightedAmount,
    IReadOnlyList<ForecastByStageDto> ByStage
);

