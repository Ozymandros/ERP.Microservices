using MyApp.Sales.Application.Contracts.DTOs;

namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>Data transfer object for converting an opportunity to a sales quote.</summary>
public sealed record ConvertOpportunityToQuoteDto(
    /// <summary>The number of days the quote is valid.</summary>
    int ValidityDays,
    /// <summary>The list of quote line items.</summary>
    List<CreateUpdateSalesOrderLineDto> Lines,
    /// <summary>The order date for the quote.</summary>
    DateTime? OrderDate
);

/// <summary>Request data transfer object for marking an opportunity as won.</summary>
public sealed record MarkOpportunityWonRequest(
    /// <summary>An optional note about winning the opportunity.</summary>
    string? Note,
    /// <summary>Whether to convert the opportunity to a sales quote.</summary>
    bool ConvertToQuote,
    /// <summary>The quote details if converting to a quote.</summary>
    ConvertOpportunityToQuoteDto? Quote
);

