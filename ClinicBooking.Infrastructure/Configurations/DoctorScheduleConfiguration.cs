using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Infrastructure.Configurations;

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("DoctorSchedules");

        builder.HasOne(ds => ds.Doctor)
            .WithMany(d => d.DoctorSchedules)
            .HasForeignKey(ds => ds.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        // section 23: index on DoctorSchedules.DoctorId + DayOfWeek
        builder.HasIndex(ds => new { ds.DoctorId, ds.DayOfWeek });
    }
}
