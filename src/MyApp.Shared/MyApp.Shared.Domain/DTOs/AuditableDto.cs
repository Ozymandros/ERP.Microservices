namespace MyApp.Shared.Domain.DTOs
{
    public abstract record AuditableDto<T>(
        T Id, 
        DateTime CreatedAt = default, 
        string CreatedBy = "", 
        DateTime? UpdatedAt = null, 
        string? UpdatedBy = null) 
        : BaseDto<T>(Id), IAuditableDto<T> 
        where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        public DateTime CreatedAt { get; set; } = CreatedAt;
        public string CreatedBy { get; set; } = CreatedBy ?? string.Empty;
        public DateTime? UpdatedAt { get; set; } = UpdatedAt;
        public string? UpdatedBy { get; set; } = UpdatedBy;
    }
}
