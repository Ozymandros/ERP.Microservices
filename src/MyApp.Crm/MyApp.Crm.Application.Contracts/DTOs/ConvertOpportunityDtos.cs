using MyApp.Sales.Application.Contracts.DTOs;

namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>Data transfer object for converting an opportunity to a sales quote.</summary>
public sealed record ConvertOpportunityToQuoteDto(
    int ValidityDays,
    List<CreateUpdateSalesOrderLineDto> Lines,
    DateTime? OrderDate
);

/// <summary>Request data transfer object for marking an opportunity as won.</summary>
public sealed record MarkOpportunityWonRequest(
    string? Note,
    bool ConvertToQuote,
    ConvertOpportunityToQuoteDto? Quote
);

