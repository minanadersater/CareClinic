namespace ClinicBooking.Domain.Common;

/// <summary>
/// Base class for all domain entities that use an int surrogate key.
/// ApplicationUser (Identity) uses a string key and does not inherit from this.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
