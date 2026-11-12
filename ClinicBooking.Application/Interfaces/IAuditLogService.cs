using ClinicBooking.Application.Common.DTOs;

namespace ClinicBooking.Application.Interfaces;

public interface IAuditLogService
{
    /// <summary>
    /// Returns the most recent audit entries (newest first).
    /// </summary>
    Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(int take = 200, CancellationToken ct = default);
}
