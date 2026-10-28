using AutoMapper;
using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Doctor
        CreateMap<Doctor, DoctorDto>()
            .ForMember(d => d.Specialties,
                opt => opt.MapFrom(s => s.DoctorSpecialties != null
                    ? s.DoctorSpecialties.Where(ds => ds.Specialty != null).Select(ds => ds.Specialty!.Name).ToList()
                    : new List<string>()));
        CreateMap<UpdateDoctorDto, Doctor>();

        // Patient
        CreateMap<Patient, PatientDto>()
            .ForMember(d => d.AppointmentCount,
                opt => opt.MapFrom(s => s.Appointments != null ? s.Appointments.Count : 0));
        CreateMap<UpdatePatientDto, Patient>();

        // Appointment
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(d => d.DoctorName, opt => opt.MapFrom(s => s.Doctor != null ? s.Doctor.FullName : ""))
            .ForMember(d => d.PatientName, opt => opt.MapFrom(s => s.Patient != null ? s.Patient.FullName : ""))
            .ForMember(d => d.PaymentId, opt => opt.MapFrom(s => s.Payment != null ? s.Payment.Id : (int?)null))
            .ForMember(d => d.PaymentAmount, opt => opt.MapFrom(s => s.Payment != null ? s.Payment.Amount : (decimal?)null))
            .ForMember(d => d.PaymentCurrency, opt => opt.MapFrom(s => s.Payment != null ? s.Payment.Currency : "EGP"))
            .ForMember(d => d.PaymentMethod, opt => opt.MapFrom(s => s.Payment != null ? s.Payment.Method : (PaymentMethod?)null))
            .ForMember(d => d.PaymentStatus, opt => opt.MapFrom(s => s.Payment != null ? s.Payment.Status : (PaymentStatus?)null))
            .ForMember(d => d.TransactionId, opt => opt.MapFrom(s => s.Payment != null ? s.Payment.TransactionId : null))
            .ForMember(d => d.PaidAt, opt => opt.MapFrom(s => s.Payment != null ? s.Payment.PaidAt : null))
            .ForMember(d => d.PaymentNotes, opt => opt.MapFrom(s => s.Payment != null ? s.Payment.PaymentNotes : null));

        // Payment
        CreateMap<Payment, PaymentDto>();

        // Schedule
        CreateMap<DoctorSchedule, DoctorScheduleDto>();
        CreateMap<UpsertScheduleDto, DoctorSchedule>();

        // MedicalRecord
        CreateMap<MedicalRecord, MedicalRecordDto>()
            .ForMember(d => d.DoctorName, opt => opt.MapFrom(s => s.Doctor != null ? s.Doctor.FullName : ""))
            .ForMember(d => d.PatientName, opt => opt.MapFrom(s => s.Patient != null ? s.Patient.FullName : ""));
        CreateMap<CreateMedicalRecordDto, MedicalRecord>();

        // Specialty
        CreateMap<Specialty, SpecialtyDto>();
    }
}
