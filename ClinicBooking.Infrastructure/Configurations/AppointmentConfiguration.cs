using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Infrastructure.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.Property(a => a.AppointmentDate)
            .HasColumnType("date");

        builder.HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // section 6 (concurrency rule) + section 23:
        // database-level protection against double-booking the same doctor/slot.
        // Filtered so that a Cancelled appointment frees the slot for rebooking,
        // matching "Cancelled appointments release the slot".
        builder.HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.StartTime })
            .IsUnique()
            .HasFilter("[Status] <> 3") // 3 = AppointmentStatus.Cancelled
            .HasDatabaseName("IX_Appointments_NoDoubleBooking");

        // section 23: search indexes
        builder.HasIndex(a => new { a.DoctorId, a.AppointmentDate });
        builder.HasIndex(a => new { a.PatientId, a.AppointmentDate });

        builder.Property(a => a.RowVersion)
            .IsRowVersion();

        builder.Property(a => a.PatientNotes).HasMaxLength(1000);
        builder.Property(a => a.CancellationReason).HasMaxLength(500);

        builder.HasOne(a => a.MedicalRecord)
            .WithOne(m => m.Appointment)
            .HasForeignKey<MedicalRecord>(m => m.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Review)
            .WithOne(r => r.Appointment)
            .HasForeignKey<Review>(r => r.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
