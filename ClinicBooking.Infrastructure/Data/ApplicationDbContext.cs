using ClinicBooking.Application.Common.Interfaces;
using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ClinicBooking.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    private readonly ICurrentUserService? _currentUser;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<DoctorSpecialty> DoctorSpecialties => Set<DoctorSpecialty>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Identity tables (AspNetUsers, AspNetRoles, ...)

        // Applies every IEntityTypeConfiguration<T> found in this assembly
        // (see ClinicBooking.Infrastructure/Configurations/*).
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        var addedAudits = ApplyAuditLog();
        ApplyAuditTimestamps();
        var result = base.SaveChanges();
        FinalizeAuditEntityIds(addedAudits);
        return result;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var addedAudits = ApplyAuditLog();
        ApplyAuditTimestamps();
        return SaveChangesCoreAsync(addedAudits, cancellationToken);
    }

    private async Task<int> SaveChangesCoreAsync(
        List<(AuditLog Log, BaseEntity Entity)> addedAudits,
        CancellationToken cancellationToken)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        FinalizeAuditEntityIds(addedAudits);
        return result;
    }

    /// <summary>
    /// Stamps CreatedAt/UpdatedAt on entities inheriting BaseEntity automatically,
    /// so services never need to set these manually.
    /// </summary>
    private void ApplyAuditTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Creates an immutable AuditLog row for every created/updated/deleted domain
    /// entity (anything deriving from BaseEntity). AuditLog itself does not derive
    /// from BaseEntity, so the trail never audits its own writes.
    /// Returns the entries created for newly-inserted rows so their database keys
    /// can be filled in after the insert (identity columns are not known upfront).
    /// </summary>
    private List<(AuditLog Log, BaseEntity Entity)> ApplyAuditLog()
    {
        var addedAudits = new List<(AuditLog, BaseEntity)>();

        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
        {
            return addedAudits;
        }

        var userId = _currentUser?.UserId;
        var ipAddress = _currentUser?.IpAddress;
        var now = DateTime.UtcNow;
        var logs = new List<AuditLog>(entries.Count);

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => null
            };

            if (action is null)
            {
                continue;
            }

            string? details = null;
            if (entry.State == EntityState.Modified)
            {
                var changed = entry.Properties
                    .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                    .Select(p => p.Metadata.Name)
                    .Where(n => n is not "UpdatedAt" and not "CreatedAt")
                    .ToList();

                if (changed.Count == 0)
                {
                    continue; // no meaningful change, don't pollute the trail
                }

                details = "Changed: " + string.Join(", ", changed);
            }

            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = BuildKeyString(entry.Properties),
                Timestamp = now,
                IpAddress = ipAddress,
                Details = details
            };

            logs.Add(log);

            if (entry.State == EntityState.Added)
            {
                addedAudits.Add((log, entry.Entity));
            }
        }

        if (logs.Count > 0)
        {
            Set<AuditLog>().AddRange(logs);
        }

        return addedAudits;
    }

    /// <summary>
    /// Fills in the database-generated key for audit rows created from inserts.
    /// Runs a second (cheap) save only when new rows were inserted.
    /// </summary>
    private void FinalizeAuditEntityIds(List<(AuditLog Log, BaseEntity Entity)> addedAudits)
    {
        if (addedAudits.Count == 0)
        {
            return;
        }

        foreach (var (log, entity) in addedAudits)
        {
            log.EntityId = BuildKeyString(Entry(entity).Properties);
        }

        base.SaveChanges();
    }

    private static string? BuildKeyString(IEnumerable<PropertyEntry> properties)
    {
        var key = properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .Select(p => p.CurrentValue?.ToString() ?? p.OriginalValue?.ToString())
            .ToArray();

        return key.Length > 0 ? string.Join(",", key) : null;
    }
}
