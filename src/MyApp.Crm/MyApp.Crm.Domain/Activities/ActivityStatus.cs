namespace MyApp.Crm.Domain.Activities;

/// <summary>Defines the possible status values for an activity.</summary>
public enum ActivityStatus
{
    /// <summary>The activity is open and pending completion.</summary>
    Open = 1,
    /// <summary>The activity has been completed.</summary>
    Completed = 2,
    /// <summary>The activity has been cancelled.</summary>
    Cancelled = 3
}

