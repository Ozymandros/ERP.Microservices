namespace MyApp.Shared.Domain.Events;

/// <summary>Raised when a project is created.</summary>
/// <param name="ProjectId">The project id.</param>
/// <param name="ProjectNumber">The project number.</param>
/// <param name="CustomerId">The bill-to customer id.</param>
/// <param name="CurrencyCode">The project currency.</param>
public record ProjectCreatedEvent(
    Guid ProjectId,
    string ProjectNumber,
    Guid CustomerId,
    string CurrencyCode
);

/// <summary>Raised when a project changes status.</summary>
/// <param name="ProjectId">The project id.</param>
/// <param name="ProjectNumber">The project number.</param>
/// <param name="OldStatus">The previous status.</param>
/// <param name="NewStatus">The new status.</param>
public record ProjectStatusChangedEvent(
    Guid ProjectId,
    string ProjectNumber,
    string OldStatus,
    string NewStatus
);

/// <summary>A single billable line requested for invoicing by a project.</summary>
/// <param name="PlanningLineId">The originating planning line id.</param>
/// <param name="Description">The line description.</param>
/// <param name="Quantity">The quantity (decimal, e.g. hours).</param>
/// <param name="UnitPrice">The unit price in the request currency.</param>
/// <param name="Discount">The line discount amount.</param>
/// <param name="TaxRate">The tax rate as a percentage (e.g. 21 for 21%), same convention as Billing.</param>
public record ProjectInvoiceLineEvent(
    Guid PlanningLineId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal TaxRate
);

/// <summary>
/// Raised by Projects to ask Billing to create a customer invoice from billable planning lines.
/// Billing creates a Draft invoice by default (auto-issue only when explicitly configured)
/// and links it with SourceType = "Project", SourceId = <paramref name="RequestId"/>.
/// </summary>
/// <param name="RequestId">Correlation id of the invoice request (stored in Projects).</param>
/// <param name="ProjectId">The project id.</param>
/// <param name="ProjectNumber">The project number.</param>
/// <param name="CustomerId">The bill-to customer id.</param>
/// <param name="Currency">ISO 4217 currency of the request.</param>
/// <param name="Lines">The lines to invoice.</param>
public record ProjectInvoiceRequestedEvent(
    Guid RequestId,
    Guid ProjectId,
    string ProjectNumber,
    Guid CustomerId,
    string Currency,
    List<ProjectInvoiceLineEvent> Lines
);
