using ClinicBooking.Application.Common.Interfaces;

namespace ClinicBooking.Tests.Infrastructure;

/// <summary>Simple test double for <see cref="ICurrentUserService"/>.</summary>
public sealed class TestCurrentUserService : ICurrentUserService
{
    public TestCurrentUserService(string? userId = null, string? ipAddress = null)
    {
        UserId = userId;
        IpAddress = ipAddress;
    }

    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
}
