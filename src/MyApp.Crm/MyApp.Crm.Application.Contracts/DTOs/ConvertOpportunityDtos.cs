using MyApp.Sales.Application.Contracts.DTOs;

namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>Data transfer object for converting an opportunity to a sales quote.</summary>
/// <param name="ValidityDays">The validity Days.</param>
/// <param name="Lines">The lines.</param>
/// <param name="OrderDate">The order Date.</param>
public sealed record ConvertOpportunityToQuoteDto(
    int ValidityDays,
    List<CreateUpdateSalesOrderLineDto> Lines,
    DateTime? OrderDate
);

/// <summary>Request data transfer object for marking an opportunity as won.</summary>
/// <param name="Note">The note.</param>
/// <param name="ConvertToQuote">The convert To Quote.</param>
/// <param name="Quote">The quote.</param>
public sealed record MarkOpportunityWonRequest(
    string? Note,
    bool ConvertToQuote,
    ConvertOpportunityToQuoteDto? Quote
);

