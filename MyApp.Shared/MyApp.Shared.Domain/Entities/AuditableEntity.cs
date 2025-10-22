namespace MyApp.Shared.Domain.Entities
{
    public class AuditableEntity<T> : DomainEntity<T>, IAuditableEntity<T> where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
