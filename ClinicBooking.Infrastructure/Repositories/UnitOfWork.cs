using ClinicBooking.Application.Common.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Infrastructure.Data;

namespace ClinicBooking.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;

    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;

        Doctors           = new Repository<Doctor>(db);
        Patients          = new Repository<Patient>(db);
        Specialties       = new Repository<Specialty>(db);
        DoctorSpecialties = new Repository<DoctorSpecialty>(db);
        DoctorSchedules   = new Repository<DoctorSchedule>(db);
        Appointments      = new Repository<Appointment>(db);
        MedicalRecords    = new Repository<MedicalRecord>(db);
        Prescriptions     = new Repository<Prescription>(db);
        PrescriptionItems = new Repository<PrescriptionItem>(db);
        Reviews           = new Repository<Review>(db);
        Notifications     = new Repository<Notification>(db);
        AuditLogs         = new Repository<AuditLog>(db);
    }

    public IRepository<Doctor>          Doctors           { get; }
    public IRepository<Patient>         Patients          { get; }
    public IRepository<Specialty>       Specialties       { get; }
    public IRepository<DoctorSpecialty> DoctorSpecialties { get; }
    public IRepository<DoctorSchedule>  DoctorSchedules   { get; }
    public IRepository<Appointment>     Appointments      { get; }
    public IRepository<MedicalRecord>   MedicalRecords    { get; }
    public IRepository<Prescription>    Prescriptions     { get; }
    public IRepository<PrescriptionItem> PrescriptionItems { get; }
    public IRepository<Review>          Reviews           { get; }
    public IRepository<Notification>    Notifications     { get; }
    public IRepository<AuditLog>        AuditLogs         { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public void Dispose() => _db.Dispose();
}

