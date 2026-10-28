using Microsoft.AspNetCore.Identity;

namespace ClinicBooking.Infrastructure.Identity;

/// <summary>
/// Identity root for authentication. Role-specific profile data lives in the
/// separate Doctor/Patient entities (Domain layer), linked back here by UserId.
/// This keeps Domain free of any ASP.NET Identity dependency.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
