using ClinicBooking.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicBooking.Tests;

[Collection("Database")]
public class AuditLogTests
{
    private readonly TestDatabase _db;

    public AuditLogTests(TestDatabase db) => _db = db;

    [Fact]
    public async Task CreatingEntity_WritesCreatedAuditLogWithUserAndIp()
    {
        var user = new TestCurrentUserService("user-123", "10.0.0.5");
        using var ctx = _db.CreateContext(user);

        var doctor = TestData.NewDoctor();
        ctx.Doctors.Add(doctor);
        await ctx.SaveChangesAsync();

        var log = await ctx.AuditLogs.SingleAsync(a =>
            a.EntityName == "Doctor" && a.Action == "Created" && a.EntityId == doctor.Id.ToString());

        Assert.Equal("user-123", log.UserId);
        Assert.Equal("10.0.0.5", log.IpAddress);
        Assert.Null(log.Details);
    }

    [Fact]
    public async Task UpdatingEntity_WritesUpdatedAuditLogListingChangedFields()
    {
        var user = new TestCurrentUserService("user-123", "10.0.0.5");
        using var ctx = _db.CreateContext(user);

        var doctor = TestData.NewDoctor();
        ctx.Doctors.Add(doctor);
        await ctx.SaveChangesAsync();

        ctx.ChangeTracker.Clear();
        var tracked = await ctx.Doctors.SingleAsync(d => d.Id == doctor.Id);
        tracked.FullName = "Dr. Renamed";
        await ctx.SaveChangesAsync();

        var log = await ctx.AuditLogs.SingleAsync(a =>
            a.EntityName == "Doctor" && a.Action == "Updated" && a.EntityId == doctor.Id.ToString());

        Assert.NotNull(log.Details);
        Assert.Contains("FullName", log.Details!);
        Assert.DoesNotContain("UpdatedAt", log.Details!);
    }

    [Fact]
    public async Task DeletingEntity_WritesDeletedAuditLog()
    {
        var user = new TestCurrentUserService("user-123", "10.0.0.5");
        using var ctx = _db.CreateContext(user);

        var doctor = TestData.NewDoctor();
        ctx.Doctors.Add(doctor);
        await ctx.SaveChangesAsync();

        ctx.ChangeTracker.Clear();
        var tracked = await ctx.Doctors.SingleAsync(d => d.Id == doctor.Id);
        ctx.Doctors.Remove(tracked);
        await ctx.SaveChangesAsync();

        var log = await ctx.AuditLogs.SingleAsync(a =>
            a.EntityName == "Doctor" && a.Action == "Deleted" && a.EntityId == doctor.Id.ToString());

        Assert.Equal("user-123", log.UserId);
    }

    [Fact]
    public async Task AuditTrail_DoesNotAuditItsOwnWrites()
    {
        var user = new TestCurrentUserService("user-123", "10.0.0.5");
        using var ctx = _db.CreateContext(user);

        var doctor = TestData.NewDoctor();
        ctx.Doctors.Add(doctor);
        await ctx.SaveChangesAsync();

        // Only the Doctor insert should be recorded - never an AuditLog insert.
        var auditAboutAudit = await ctx.AuditLogs
            .AnyAsync(a => a.EntityName == "AuditLog");

        Assert.False(auditAboutAudit);
    }
}
