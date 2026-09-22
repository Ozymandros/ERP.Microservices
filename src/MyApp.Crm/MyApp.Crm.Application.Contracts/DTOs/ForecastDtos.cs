namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>
/// Forecast by stage dto.
/// </summary>
/// <param name="Stage">The stage.</param>
/// <param name="Count">The count.</param>
/// <param name="SumExpectedAmount">The sum Expected Amount.</param>
/// <param name="WeightedAmount">The weighted Amount.</param>
public sealed record ForecastByStageDto(
    string Stage,
    int Count,
    decimal? SumExpectedAmount,
    decimal WeightedAmount
);

/// <summary>
/// Forecast summary dto.
/// </summary>
/// <param name="OwnerUsername">The owner Username.</param>
/// <param name="FromExpectedCloseDate">The from Expected Close Date.</param>
/// <param name="ToExpectedCloseDate">The to Expected Close Date.</param>
/// <param name="TotalCount">The total Count.</param>
/// <param name="TotalExpectedAmount">The total Expected Amount.</param>
/// <param name="TotalWeightedAmount">The total Weighted Amount.</param>
/// <param name="ByStage">The by Stage.</param>
public sealed record ForecastSummaryDto(
    string OwnerUsername,
    DateOnly? FromExpectedCloseDate,
    DateOnly? ToExpectedCloseDate,
    int TotalCount,
    decimal? TotalExpectedAmount,
    decimal TotalWeightedAmount,
    IReadOnlyList<ForecastByStageDto> ByStage
);

