using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Infrastructure.Configurations;

public class DoctorSpecialtyConfiguration : IEntityTypeConfiguration<DoctorSpecialty>
{
    public void Configure(EntityTypeBuilder<DoctorSpecialty> builder)
    {
        builder.ToTable("DoctorSpecialties");

        builder.HasKey(ds => new { ds.DoctorId, ds.SpecialtyId });

        builder.HasOne(ds => ds.Doctor)
            .WithMany(d => d.DoctorSpecialties)
            .HasForeignKey(ds => ds.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ds => ds.Specialty)
            .WithMany(s => s.DoctorSpecialties)
            .HasForeignKey(ds => ds.SpecialtyId)
            .OnDelete(DeleteBehavior.Cascade);

        // section 23: index on DoctorSpecialties.SpecialtyId
        builder.HasIndex(ds => ds.SpecialtyId);
    }
}
