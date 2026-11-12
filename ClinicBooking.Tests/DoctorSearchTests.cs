using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Infrastructure.Data;
using ClinicBooking.Infrastructure.Services;
using ClinicBooking.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicBooking.Tests;

[Collection("Database")]
public class DoctorSearchTests
{
    private readonly TestDatabase _db;

    public DoctorSearchTests(TestDatabase db) => _db = db;

    private static DoctorService NewService(ApplicationDbContext ctx) =>
        new(ctx, TestData.CreateUserManager(ctx), TestData.CreateMapper());

    private static Doctor NamedDoctor(string name, decimal fee, bool isActive = true)
    {
        var doctor = TestData.NewDoctor(fee, isActive: isActive);
        doctor.FullName = name;
        return doctor;
    }

    [Fact]
    public async Task SearchActiveDoctors_WithNoFilter_ExcludesInactiveDoctors()
    {
        using var ctx = _db.CreateContext();
        var token = Guid.NewGuid().ToString("N")[..8];
        var active = NamedDoctor($"Dr. Active {token}", 200m);
        var inactive = NamedDoctor($"Dr. Inactive {token}", 300m, isActive: false);

        ctx.Doctors.AddRange(active, inactive);
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        var result = await service.SearchActiveDoctorsAsync(new DoctorSearchFilter());

        Assert.Contains(result, d => d.Id == active.Id);
        Assert.DoesNotContain(result, d => d.Id == inactive.Id);
    }

    [Fact]
    public async Task SearchActiveDoctors_ByPartialName_ReturnsMatchesOnly()
    {
        using var ctx = _db.CreateContext();
        var token = Guid.NewGuid().ToString("N")[..8];
        var match = NamedDoctor($"Dr. Cardio {token}", 200m);
        var other = NamedDoctor($"Dr. Unrelated {Guid.NewGuid():N}", 250m);

        ctx.Doctors.AddRange(match, other);
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        var result = await service.SearchActiveDoctorsAsync(new DoctorSearchFilter { SearchTerm = token });

        Assert.Single(result);
        Assert.Equal(match.Id, result[0].Id);
    }

    [Fact]
    public async Task SearchActiveDoctors_BySpecialty_ReturnsOnlyDoctorsWithThatSpecialty()
    {
        using var ctx = _db.CreateContext();
        var token = Guid.NewGuid().ToString("N")[..8];
        var specialty = new Specialty { Name = $"Cardiology {token}" };
        var otherSpecialty = new Specialty { Name = $"Dermatology {token}" };
        ctx.Specialties.AddRange(specialty, otherSpecialty);
        await ctx.SaveChangesAsync();

        var cardio = NamedDoctor($"Dr. Heart {token}", 300m);
        var skin = NamedDoctor($"Dr. Skin {token}", 300m);
        ctx.Doctors.AddRange(cardio, skin);
        await ctx.SaveChangesAsync();

        ctx.DoctorSpecialties.AddRange(
            new DoctorSpecialty { DoctorId = cardio.Id, SpecialtyId = specialty.Id },
            new DoctorSpecialty { DoctorId = skin.Id, SpecialtyId = otherSpecialty.Id });
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        var result = await service.SearchActiveDoctorsAsync(new DoctorSearchFilter { SpecialtyId = specialty.Id });

        Assert.Single(result);
        Assert.Equal(cardio.Id, result[0].Id);
        Assert.Contains(specialty.Name, result[0].Specialties);
    }

    [Fact]
    public async Task SearchActiveDoctors_ByFeeRange_ReturnsDoctorsWithinRange()
    {
        using var ctx = _db.CreateContext();
        var token = Guid.NewGuid().ToString("N")[..8];
        var cheap = NamedDoctor($"Dr. Cheap {token}", 100m);
        var middle = NamedDoctor($"Dr. Middle {token}", 300m);
        var expensive = NamedDoctor($"Dr. Expensive {token}", 600m);

        ctx.Doctors.AddRange(cheap, middle, expensive);
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        var result = await service.SearchActiveDoctorsAsync(new DoctorSearchFilter
        {
            SearchTerm = token,
            MinFee = 200m,
            MaxFee = 400m
        });

        Assert.Single(result);
        Assert.Equal(middle.Id, result[0].Id);
    }

    [Fact]
    public async Task SearchActiveDoctors_SortByFeeAscending_OrdersCheapestFirst()
    {
        using var ctx = _db.CreateContext();
        var token = Guid.NewGuid().ToString("N")[..8];
        var expensive = NamedDoctor($"Dr. E {token}", 600m);
        var cheap = NamedDoctor($"Dr. C {token}", 100m);
        var middle = NamedDoctor($"Dr. M {token}", 300m);

        ctx.Doctors.AddRange(expensive, cheap, middle);
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        var result = await service.SearchActiveDoctorsAsync(new DoctorSearchFilter
        {
            SearchTerm = token,
            Sort = DoctorSortOrder.FeeAscending
        });

        Assert.Equal(new[] { 100m, 300m, 600m }, result.Select(d => d.ConsultationFee).ToArray());
    }

    [Fact]
    public async Task SearchActiveDoctors_SortByFeeDescending_OrdersMostExpensiveFirst()
    {
        using var ctx = _db.CreateContext();
        var token = Guid.NewGuid().ToString("N")[..8];
        var cheap = NamedDoctor($"Dr. C {token}", 100m);
        var expensive = NamedDoctor($"Dr. E {token}", 600m);
        var middle = NamedDoctor($"Dr. M {token}", 300m);

        ctx.Doctors.AddRange(cheap, expensive, middle);
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        var result = await service.SearchActiveDoctorsAsync(new DoctorSearchFilter
        {
            SearchTerm = token,
            Sort = DoctorSortOrder.FeeDescending
        });

        Assert.Equal(new[] { 600m, 300m, 100m }, result.Select(d => d.ConsultationFee).ToArray());
    }

    [Fact]
    public async Task SearchActiveDoctors_CombinesNameSpecialtyAndFeeFilters()
    {
        using var ctx = _db.CreateContext();
        var token = Guid.NewGuid().ToString("N")[..8];
        var specialty = new Specialty { Name = $"Neurology {token}" };
        ctx.Specialties.Add(specialty);
        await ctx.SaveChangesAsync();

        var target = NamedDoctor($"Dr. Target {token}", 250m);
        var wrongSpecialty = NamedDoctor($"Dr. Other {token}", 250m);
        var wrongFee = NamedDoctor($"Dr. Pricey {token}", 900m);
        ctx.Doctors.AddRange(target, wrongSpecialty, wrongFee);
        await ctx.SaveChangesAsync();

        ctx.DoctorSpecialties.AddRange(
            new DoctorSpecialty { DoctorId = target.Id, SpecialtyId = specialty.Id },
            new DoctorSpecialty { DoctorId = wrongFee.Id, SpecialtyId = specialty.Id });
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        var result = await service.SearchActiveDoctorsAsync(new DoctorSearchFilter
        {
            SearchTerm = token,
            SpecialtyId = specialty.Id,
            MaxFee = 500m
        });

        Assert.Single(result);
        Assert.Equal(target.Id, result[0].Id);
    }
}
