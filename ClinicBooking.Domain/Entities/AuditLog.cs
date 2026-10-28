namespace ClinicBooking.Domain.Entities;

/// <summary>
/// Records important administrative and security-sensitive actions.
/// Intentionally does not inherit BaseEntity (no soft-delete/update semantics
/// apply to an immutable audit trail) and must never store sensitive medical
/// content in Details.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    public string? UserId { get; set; } // FK -> AspNetUsers.Id, nullable for system actions
    public string Action { get; set; } = default!;
    public string EntityName { get; set; } = default!;
    public string? EntityId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
}
