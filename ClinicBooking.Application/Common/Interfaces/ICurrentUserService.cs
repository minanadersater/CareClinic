namespace ClinicBooking.Application.Common.Interfaces;

/// <summary>
/// Exposes the identity of the caller for the current request so the
/// infrastructure layer can stamp audit entries without depending on ASP.NET Core.
/// Returns nulls for system/background work (e.g. startup seeding).
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? IpAddress { get; }
}
