using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Infrastructure.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.FullName).IsRequired().HasMaxLength(150);
        builder.Property(p => p.PhoneNumber).IsRequired().HasMaxLength(30);
        builder.Property(p => p.Email).IsRequired().HasMaxLength(256);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
