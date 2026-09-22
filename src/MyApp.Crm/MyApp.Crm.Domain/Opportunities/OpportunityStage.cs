namespace MyApp.Crm.Domain.Opportunities;

/// <summary>
/// Defines the Opportunity Stage enumeration values.
/// </summary>
public enum OpportunityStage
{
    /// <summary>Initial stage where potential opportunities are identified.</summary>
    Prospecting = 1,
    /// <summary>Stage where the opportunity is evaluated and validated.</summary>
    Qualification = 2,
    /// <summary>Stage where a formal proposal is being prepared or has been presented.</summary>
    Proposal = 3,
    /// <summary>Stage where terms are being negotiated with the customer.</summary>
    Negotiation = 4,
    /// <summary>The opportunity has been successfully closed.</summary>
    Won = 5,
    /// <summary>The opportunity has been closed without a sale.</summary>
    Lost = 6
}

