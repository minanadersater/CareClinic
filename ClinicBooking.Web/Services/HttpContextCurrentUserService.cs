using System.Security.Claims;
using ClinicBooking.Application.Common.Interfaces;

namespace ClinicBooking.Web.Services;

/// <summary>
/// Reads the current caller from the HTTP context. Registered as scoped so it
/// follows the request; returns null outside a request (background/system work).
/// </summary>
public class HttpContextCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
}
