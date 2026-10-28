using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Infrastructure.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");

        builder.HasIndex(d => d.UserId).IsUnique();

        builder.Property(d => d.FullName).IsRequired().HasMaxLength(150);
        builder.Property(d => d.PhoneNumber).IsRequired().HasMaxLength(30);
        builder.Property(d => d.Email).IsRequired().HasMaxLength(256);
        builder.Property(d => d.ConsultationFee).HasColumnType("decimal(10,2)");
        builder.Property(d => d.Bio).HasMaxLength(2000);

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
