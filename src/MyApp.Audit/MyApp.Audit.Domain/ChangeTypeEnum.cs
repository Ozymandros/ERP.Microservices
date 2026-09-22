namespace MyApp.Audit.Domain
{
    /// <summary>
    /// Represents the type of change applied to a domain entity during an audited operation.
    /// </summary>
    public enum ChangeTypeEnum
    {
        /// <summary>The entity was newly created.</summary>
        Created = 0,
        /// <summary>One or more properties of the entity were modified.</summary>
        Updated = 1,
        /// <summary>The entity was removed.</summary>
        Deleted = 2,
    }
}