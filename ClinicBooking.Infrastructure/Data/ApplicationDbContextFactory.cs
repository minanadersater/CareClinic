using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClinicBooking.Infrastructure.Data;

/// <summary>
/// Lets 'dotnet ef migrations add ...' run with ClinicBooking.Infrastructure as
/// the startup project. When running from ClinicBooking.Web instead, the
/// Program.cs-configured DbContext (with appsettings.json connection string)
/// is used and this factory is not invoked.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Fallback connection string for design-time migration generation only.
        // Real runtime connection string comes from ClinicBooking.Web/appsettings.json.
        var connectionString = Environment.GetEnvironmentVariable("CLINICBOOKING_CONNECTION")
            ?? "Server=(localdb)\\mssqllocaldb;Database=ClinicBookingDb;Trusted_Connection=True;MultipleActiveResultSets=true";

        optionsBuilder.UseSqlServer(connectionString,
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
