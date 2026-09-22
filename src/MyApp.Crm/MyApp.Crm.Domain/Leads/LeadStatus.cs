namespace MyApp.Crm.Domain.Leads;

/// <summary>
/// Defines the Lead Status enumeration values.
/// </summary>
public enum LeadStatus
{
    /// <summary>The lead is newly created and has not yet been evaluated.</summary>
    New = 1,
    /// <summary>The lead has been qualified and converted to a customer opportunity.</summary>
    Qualified = 2,
    /// <summary>The lead has been disqualified and will not be pursued further.</summary>
    Disqualified = 3
}

