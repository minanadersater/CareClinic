using Microsoft.AspNetCore.Identity;

namespace ClinicBooking.Infrastructure.Identity;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
}
