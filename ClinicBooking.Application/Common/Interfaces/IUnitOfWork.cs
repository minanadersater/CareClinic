using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Common.Interfaces;

/// <summary>
/// Wraps all repositories and the underlying transaction/save operation.
/// Services use this instead of injecting DbContext directly.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<Doctor>        Doctors        { get; }
    IRepository<Patient>       Patients       { get; }
    IRepository<Specialty>     Specialties    { get; }
    IRepository<DoctorSpecialty> DoctorSpecialties { get; }
    IRepository<DoctorSchedule> DoctorSchedules { get; }
    IRepository<Appointment>   Appointments   { get; }
    IRepository<MedicalRecord> MedicalRecords { get; }
    IRepository<Prescription>  Prescriptions  { get; }
    IRepository<PrescriptionItem> PrescriptionItems { get; }
    IRepository<Review>        Reviews        { get; }
    IRepository<Notification>  Notifications  { get; }
    IRepository<AuditLog>      AuditLogs      { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

