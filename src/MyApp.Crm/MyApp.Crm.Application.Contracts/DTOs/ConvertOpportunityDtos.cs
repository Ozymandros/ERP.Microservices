namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>CRM-owned quote line for opportunity conversion.</summary>
public sealed record CrmQuoteLineDto(Guid ProductId, int Quantity, decimal UnitPrice);

/// <summary>Data transfer object for converting an opportunity to a sales quote.</summary>
public sealed record ConvertOpportunityToQuoteDto(
    int ValidityDays,
    List<CrmQuoteLineDto> Lines,
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

