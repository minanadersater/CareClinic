using ClinicBooking.Application.Common.Interfaces;
using ClinicBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Tests.Infrastructure;

/// <summary>
/// Creates a throwaway SQL Server LocalDB database for the test run and drops it
/// afterwards. The real provider is used on purpose so that provider-specific
/// guarantees (the filtered unique index that prevents double-booking, the
/// rowversion concurrency token, decimal/date column types) are exercised exactly
/// as they are in production.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly string _databaseName;
    private readonly string _connectionString;

    public TestDatabase()
    {
        _databaseName = "ClinicBookingTests_" + Guid.NewGuid().ToString("N");
        _connectionString =
            $"Server=(localdb)\\mssqllocaldb;Database={_databaseName};" +
            "Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a new, independent <see cref="ApplicationDbContext"/> over the shared
    /// test database. Each test should use its own context to keep the change
    /// tracker (and the audit trail) isolated.
    /// </summary>
    public ApplicationDbContext CreateContext(ICurrentUserService? currentUser = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .EnableSensitiveDataLogging()
            .Options;

        return new ApplicationDbContext(options, currentUser);
    }

    public void Dispose()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
    }
}
