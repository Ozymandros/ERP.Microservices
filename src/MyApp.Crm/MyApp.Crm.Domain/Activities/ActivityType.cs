namespace MyApp.Crm.Domain.Activities;

/// <summary>Defines the types of activities that can be tracked.</summary>
public enum ActivityType
{
    /// <summary>A task activity.</summary>
    Task = 1,
    /// <summary>A phone call activity.</summary>
    Call = 2,
    /// <summary>A meeting activity.</summary>
    Meeting = 3,
    /// <summary>An email activity.</summary>
    Email = 4
}

