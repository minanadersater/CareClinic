using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicBooking.Infrastructure.Data;

/// <summary>
/// Seeds roles, an initial admin account, and reference data (specialties).
/// The admin password is read from configuration/user-secrets/environment
/// variables only -- never hard-coded, per section 28 of the spec.
/// Call SeedAsync from Program.cs after building the app, before app.Run().
/// </summary>
public static class SeedData
{
    public static readonly string[] Roles = { "Admin", "Doctor", "Patient" };

    public static async Task SeedAsync(IServiceProvider services, bool isDevelopment = false)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();
        var config = services.GetRequiredService<IConfiguration>();

        // 1) Roles
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }

        // 2) Initial admin account, from configuration only.
        var adminEmail = config["SeedAdmin:Email"];
        var adminPassword = config["SeedAdmin:Password"];

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin is null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = config["SeedAdmin:FullName"] ?? "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                EnsureIdentitySuccess(result, $"creating the seed admin '{adminEmail}'");
                existingAdmin = adminUser;
            }

            // Keep the configured seed account usable when it was created by an
            // earlier run with another password or was locked after failed logins.
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
            EnsureIdentitySuccess(
                await userManager.ResetPasswordAsync(existingAdmin, resetToken, adminPassword),
                $"setting the seed admin password for '{adminEmail}'");

            existingAdmin.EmailConfirmed = true;
            existingAdmin.IsActive = true;
            existingAdmin.LockoutEnd = null;
            existingAdmin.AccessFailedCount = 0;
            EnsureIdentitySuccess(await userManager.UpdateAsync(existingAdmin), $"updating the seed admin '{adminEmail}'");

            if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
            {
                EnsureIdentitySuccess(await userManager.AddToRoleAsync(existingAdmin, "Admin"), $"assigning the Admin role to '{adminEmail}'");
            }
        }

        // 3) Default specialties (section 28).
        if (!await db.Specialties.AnyAsync())
        {
            db.Specialties.AddRange(
                new Specialty { Name = "Cardiology", IsActive = true },
                new Specialty { Name = "Dermatology", IsActive = true },
                new Specialty { Name = "Pediatrics", IsActive = true },
                new Specialty { Name = "Dentistry", IsActive = true },
                new Specialty { Name = "Orthopedics", IsActive = true }
            );
            await db.SaveChangesAsync();
        }

        // Demo accounts and clinical data are only created in Development.
        // Their shared password is supplied through User Secrets, never source control.
        if (isDevelopment)
        {
            await SeedDemoDatasetAsync(userManager, db, config);
        }
    }

    private static void EnsureIdentitySuccess(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Identity failed while {operation}: {errors}");
        }
    }

    private static async Task SeedDemoDatasetAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IConfiguration config)
    {
        var demoPassword = config["SeedDemo:Password"];
        if (string.IsNullOrWhiteSpace(demoPassword))
        {
            return;
        }

        var specialtyIds = await db.Specialties
            .Where(s => !s.IsDeleted)
            .ToDictionaryAsync(s => s.Name, s => s.Id);

        var doctors = new (string Name, string Email, string Phone, Gender Gender, string Specialty, string Clinic, string Address)[]
        {
            ("د. أحمد محمود", "demo.doctor1@clinicbooking.local", "+201000000101", Gender.Male, "Cardiology", "عيادة القلب", "مدينة نصر، القاهرة"),
            ("د. سارة إبراهيم", "demo.doctor2@clinicbooking.local", "+201000000102", Gender.Female, "Dermatology", "عيادة الجلدية", "المعادي، القاهرة"),
            ("د. عمر خالد", "demo.doctor3@clinicbooking.local", "+201000000103", Gender.Male, "Pediatrics", "عيادة الأطفال", "الدقي، الجيزة"),
            ("د. منى علي", "demo.doctor4@clinicbooking.local", "+201000000104", Gender.Female, "Dentistry", "عيادة الأسنان", "مصر الجديدة، القاهرة"),
            ("د. يوسف حسن", "demo.doctor5@clinicbooking.local", "+201000000105", Gender.Male, "Orthopedics", "عيادة العظام", "الهرم، الجيزة")
        };

        var doctorProfiles = new List<Doctor>();
        for (var i = 0; i < doctors.Length; i++)
        {
            var spec = doctors[i];
            var user = await EnsureDemoUserAsync(userManager, spec.Email, spec.Name, demoPassword, "Doctor");
            var doctor = await db.Doctors.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (doctor is null)
            {
                doctor = new Doctor
                {
                    UserId = user.Id,
                    FullName = spec.Name,
                    Email = spec.Email,
                    PhoneNumber = spec.Phone,
                    Gender = spec.Gender,
                    Bio = $"طبيب {spec.Specialty} — بيانات تجريبية." ,
                    ConsultationFee = 250 + (i * 50),
                    ClinicName = spec.Clinic,
                    ClinicAddress = spec.Address,
                    AppointmentDurationMinutes = 30,
                    IsActive = true
                };
                db.Doctors.Add(doctor);
                await db.SaveChangesAsync();
            }

            if (specialtyIds.TryGetValue(spec.Specialty, out var specialtyId) &&
                !await db.DoctorSpecialties.AnyAsync(x => x.DoctorId == doctor.Id && x.SpecialtyId == specialtyId))
            {
                db.DoctorSpecialties.Add(new DoctorSpecialty { DoctorId = doctor.Id, SpecialtyId = specialtyId });
            }

            if (!await db.DoctorSchedules.AnyAsync(s => s.DoctorId == doctor.Id))
            {
                foreach (WeekDay day in Enum.GetValues<WeekDay>())
                {
                    db.DoctorSchedules.Add(new DoctorSchedule
                    {
                        DoctorId = doctor.Id,
                        DayOfWeek = day,
                        StartTime = TimeSpan.FromHours(9),
                        EndTime = TimeSpan.FromHours(17),
                        IsActive = true
                    });
                }
            }
            doctorProfiles.Add(doctor);
        }

        var patients = new (string Name, string Email, string Phone, Gender Gender)[]
        {
            ("مريم سمير", "demo.patient1@clinicbooking.local", "+201000000201", Gender.Female),
            ("كريم عادل", "demo.patient2@clinicbooking.local", "+201000000202", Gender.Male),
            ("نور الدين", "demo.patient3@clinicbooking.local", "+201000000203", Gender.Other),
            ("ليلى مصطفى", "demo.patient4@clinicbooking.local", "+201000000204", Gender.Female),
            ("علي نبيل", "demo.patient5@clinicbooking.local", "+201000000205", Gender.Male)
        };

        var patientProfiles = new List<Patient>();
        for (var i = 0; i < patients.Length; i++)
        {
            var spec = patients[i];
            var user = await EnsureDemoUserAsync(userManager, spec.Email, spec.Name, demoPassword, "Patient");
            var patient = await db.Patients.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient is null)
            {
                patient = new Patient
                {
                    UserId = user.Id,
                    FullName = spec.Name,
                    Email = spec.Email,
                    PhoneNumber = spec.Phone,
                    Gender = spec.Gender,
                    DateOfBirth = new DateTime(1988 + i, 1 + i, 10 + i),
                    Address = "القاهرة — عنوان تجريبي",
                };
                db.Patients.Add(patient);
                await db.SaveChangesAsync();
            }
            patientProfiles.Add(patient);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var appointmentSeeds = new (int Doctor, int Patient, int DayOffset, int Hour, AppointmentStatus Status)[]
        {
            (0, 0, 1, 9, AppointmentStatus.Pending),
            (1, 1, 2, 10, AppointmentStatus.Confirmed),
            (2, 2, 3, 11, AppointmentStatus.Pending),
            (3, 3, 4, 12, AppointmentStatus.Confirmed),
            (4, 4, 5, 13, AppointmentStatus.Pending),
            (0, 1, -5, 9, AppointmentStatus.Completed),
            (1, 2, -4, 10, AppointmentStatus.Completed),
            (2, 3, -3, 11, AppointmentStatus.Completed),
            (3, 4, -2, 12, AppointmentStatus.Cancelled),
            (4, 0, 6, 14, AppointmentStatus.Confirmed)
        };

        var appointmentProfiles = new List<Appointment>();
        foreach (var seed in appointmentSeeds)
        {
            var date = today.AddDays(seed.DayOffset);
            var start = TimeSpan.FromHours(seed.Hour);
            var doctor = doctorProfiles[seed.Doctor];
            var patient = patientProfiles[seed.Patient];
            var appointment = await db.Appointments.IgnoreQueryFilters().FirstOrDefaultAsync(a =>
                a.DoctorId == doctor.Id && a.PatientId == patient.Id &&
                a.AppointmentDate == date && a.StartTime == start);

            if (appointment is null)
            {
                appointment = new Appointment
                {
                    DoctorId = doctor.Id,
                    PatientId = patient.Id,
                    AppointmentDate = date,
                    StartTime = start,
                    EndTime = start.Add(TimeSpan.FromMinutes(doctor.AppointmentDurationMinutes)),
                    Status = seed.Status,
                    PatientNotes = "موعد تجريبي — لا يحتوي على بيانات مريض حقيقية."
                };
                db.Appointments.Add(appointment);
                await db.SaveChangesAsync();
            }
            appointmentProfiles.Add(appointment);
        }

        foreach (var appointment in appointmentProfiles)
        {
            if (!await db.Payments.AnyAsync(p => p.AppointmentId == appointment.Id))
            {
                var doctor = doctorProfiles.First(d => d.Id == appointment.DoctorId);
                var isPaid = appointment.Status == AppointmentStatus.Completed || appointment.Status == AppointmentStatus.Confirmed;
                var method = appointment.Status switch
                {
                    AppointmentStatus.Completed => PaymentMethod.CreditCard,
                    AppointmentStatus.Confirmed => PaymentMethod.InstaPay,
                    _ => PaymentMethod.Cash
                };

                db.Payments.Add(new Payment
                {
                    AppointmentId = appointment.Id,
                    Amount = doctor.ConsultationFee,
                    Currency = "EGP",
                    Method = method,
                    Status = isPaid ? PaymentStatus.Paid : PaymentStatus.Pending,
                    TransactionId = isPaid ? $"TXN-EGP-{100000 + appointment.Id}" : null,
                    PaidAt = isPaid ? DateTime.UtcNow.AddDays(-2) : null,
                    PaymentNotes = isPaid ? "تم الدفع إلكترونياً بنجاح" : "الدفع نقداً في العيادة",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync();

        for (var i = 5; i < appointmentProfiles.Count; i++)
        {
            var appointment = appointmentProfiles[i];
            if (appointment.Status != AppointmentStatus.Completed ||
                await db.MedicalRecords.AnyAsync(m => m.AppointmentId == appointment.Id))
            {
                continue;
            }

            var record = new MedicalRecord
            {
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                Diagnosis = "متابعة دورية (بيانات تجريبية)",
                Symptoms = "لا توجد أعراض حادة (بيانات تجريبية)",
                Notes = "سجل تجريبي لأغراض العرض فقط.",
                TreatmentPlan = "متابعة حسب الحاجة.",
                RecordDate = DateTime.UtcNow.AddDays(-Math.Abs(appointment.AppointmentDate.DayNumber - today.DayNumber))
            };
            db.MedicalRecords.Add(record);
            await db.SaveChangesAsync();

            var prescription = new Prescription
            {
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                MedicalRecordId = record.Id,
                Notes = "وصفة تجريبية للعرض فقط.",
                Items =
                {
                    new PrescriptionItem
                    {
                        MedicineName = "Demo Medicine",
                        Dosage = "قرص واحد",
                        Frequency = "مرة يوميًا",
                        Duration = "5 أيام",
                        Instructions = "بيانات تجريبية — لا تستخدم كوصفة طبية."
                    }
                }
            };
            db.Prescriptions.Add(prescription);
        }
        await db.SaveChangesAsync();

        foreach (var appointment in appointmentProfiles.Where(a => a.Status == AppointmentStatus.Completed))
        {
            if (!await db.Reviews.AnyAsync(r => r.AppointmentId == appointment.Id))
            {
                db.Reviews.Add(new Review
                {
                    AppointmentId = appointment.Id,
                    DoctorId = appointment.DoctorId,
                    PatientId = appointment.PatientId,
                    Rating = 4,
                    Comment = "تجربة جيدة (بيانات تجريبية)."
                });
            }
        }

        foreach (var appointment in appointmentProfiles)
        {
            var patient = patientProfiles.Single(p => p.Id == appointment.PatientId);
            var doctor = doctorProfiles.Single(d => d.Id == appointment.DoctorId);
            var patientNotificationType = appointment.Status switch
            {
                AppointmentStatus.Confirmed => NotificationType.AppointmentConfirmed,
                AppointmentStatus.Cancelled => NotificationType.AppointmentCancelled,
                _ => NotificationType.AppointmentBooked
            };

            if (!await db.Notifications.AnyAsync(n =>
                    n.UserId == patient.UserId && n.AppointmentId == appointment.Id && n.Type == patientNotificationType))
            {
                db.Notifications.Add(new Notification
                {
                    UserId = patient.UserId,
                    AppointmentId = appointment.Id,
                    Type = patientNotificationType,
                    Title = "تحديث موعد تجريبي",
                    Message = "هذا إشعار تجريبي مرتبط بموعد في بيانات العرض."
                });
            }

            if (!await db.Notifications.AnyAsync(n =>
                    n.UserId == doctor.UserId && n.AppointmentId == appointment.Id && n.Type == NotificationType.NewAppointmentForDoctor))
            {
                db.Notifications.Add(new Notification
                {
                    UserId = doctor.UserId,
                    AppointmentId = appointment.Id,
                    Type = NotificationType.NewAppointmentForDoctor,
                    Title = "موعد تجريبي جديد",
                    Message = "هذا إشعار تجريبي مرتبط بموعد في بيانات العرض."
                });
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> EnsureDemoUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                IsActive = true
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create demo account {email}: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            await userManager.ResetPasswordAsync(user, resetToken, password);
            user.EmailConfirmed = true;
            user.IsActive = true;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            await userManager.UpdateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not assign role {role} to demo account {email}: {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");
            }
        }
        return user;
    }
}
