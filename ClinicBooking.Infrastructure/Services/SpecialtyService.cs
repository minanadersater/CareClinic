using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Services;

public class SpecialtyService : ISpecialtyService
{
    private readonly ApplicationDbContext _context;

    public SpecialtyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Specialty>> GetAllAsync()
    {
        return await _context.Specialties
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Specialty>> GetActiveAsync()
    {
        return await _context.Specialties
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Specialty?> GetByIdAsync(int id)
    {
        return await _context.Specialties.FindAsync(id);
    }

    public async Task AddAsync(Specialty specialty)
    {
        specialty.CreatedAt = DateTime.UtcNow;
        specialty.UpdatedAt = DateTime.UtcNow;
        _context.Specialties.Add(specialty);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Specialty specialty)
    {
        specialty.UpdatedAt = DateTime.UtcNow;
        _context.Specialties.Update(specialty);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var specialty = await _context.Specialties.FindAsync(id);
        if (specialty != null)
        {
            _context.Specialties.Remove(specialty);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string name, int excludeId = 0)
    {
        return await _context.Specialties
            .AnyAsync(s => s.Name.ToLower() == name.ToLower() && s.Id != excludeId);
    }
}
