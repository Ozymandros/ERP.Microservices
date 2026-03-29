using MyApp.Sales.Application.Contracts.DTOs;

namespace MyApp.Crm.Application.Contracts.DTOs;

public sealed record ConvertOpportunityToQuoteDto(
    int ValidityDays,
    List<CreateUpdateSalesOrderLineDto> Lines,
    DateTime? OrderDate
);

public sealed record MarkOpportunityWonRequest(
    string? Note,
    bool ConvertToQuote,
    ConvertOpportunityToQuoteDto? Quote
);

