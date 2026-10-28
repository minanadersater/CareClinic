using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Interfaces;

public interface ISpecialtyService
{
    Task<IEnumerable<Specialty>> GetAllAsync();
    Task<IEnumerable<Specialty>> GetActiveAsync();
    Task<Specialty?> GetByIdAsync(int id);
    Task AddAsync(Specialty specialty);
    Task UpdateAsync(Specialty specialty);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(string name, int excludeId = 0);
}

