# Clinic Booking & Management System

ASP.NET Core MVC clinic appointment platform. See the original spec for full
functional/business requirements. This scaffold covers **Phase 1 (Foundation)**
and **Phase 2 (Identity)**.

## What's implemented so far

- **Solution structure**: `ClinicBooking.Domain` → `ClinicBooking.Application` →
  `ClinicBooking.Infrastructure` → `ClinicBooking.Web`, with the dependency
  direction enforced (Domain has zero project/package dependencies).
- **Domain entities**: Doctor, Patient, Specialty, DoctorSpecialty,
  DoctorSchedule, Appointment, MedicalRecord, Prescription, PrescriptionItem,
  Review, Notification, AuditLog.
- **EF Core + SQL Server**: `ApplicationDbContext` (extends `IdentityDbContext`),
  Fluent API configurations for every entity, including:
  - A **unique filtered index** on `(DoctorId, AppointmentDate, StartTime)` that
    excludes cancelled appointments — this is the database-level double-booking
    guard called out in the spec's concurrency rule.
  - A `rowversion` concurrency token on `Appointment`.
  - Unique index on `Specialty.Name`, one-review-per-appointment, and the
    search indexes listed in the spec (section 23).
- **Identity**: `ApplicationUser` / `ApplicationRole` (kept in Infrastructure so
  Domain stays framework-free), cookie authentication, roles (`Admin`,
  `Doctor`, `Patient`), password policy, authorization policies.
- **Patient self-registration**, **login**, **logout**, **access denied** —
  Doctor accounts are intentionally *not* self-service; they'll be created by
  an Admin in Phase 3, since doctors need to be vetted before receiving
  bookings.
- **Seeding**: roles, default specialties, and an optional admin account read
  entirely from configuration (never hard-coded).

## Not built yet (upcoming phases)

Phase 3 (Doctor & Specialty management), Phase 4 (Scheduling/slot generation),
Phase 5 (Booking + double-booking protection at the service layer), Phase 6
(Medical records/Prescriptions UI), Phase 7 (Notifications, SignalR, audit
logs, caching, Docker, tests). The `Application` project currently only has
its package references wired up — no services/DTOs/validators yet, since
those land with Phase 3+.

## Running this locally

> This project was scaffolded in a sandbox with no `dotnet` SDK and no
> network access to nuget.org, so none of this has been restored, built, or
> migrated yet. Do that on your own machine:

1. **Prerequisites**: .NET 8 SDK, SQL Server (LocalDB is fine for dev).

2. **Restore & build**
   ```bash
   cd ClinicBooking
   dotnet restore
   dotnet build
   ```

3. **Set the connection string** (already defaults to LocalDB in
   `ClinicBooking.Web/appsettings.json`) — change it if you're pointing at a
   real SQL Server instance.

4. **(Optional) Seed an admin account** without committing credentials:
   ```bash
   cd ClinicBooking.Web
   dotnet user-secrets init
   dotnet user-secrets set "SeedAdmin:Email" "admin@example.com"
   dotnet user-secrets set "SeedAdmin:Password" "ChangeMe123!"
   ```
   Leave these unset and no admin account will be created — the app still
   runs fine without one.

5. **Create and apply the first migration**
   ```bash
   cd ..
   dotnet ef migrations add InitialCreate --project ClinicBooking.Infrastructure --startup-project ClinicBooking.Web
   dotnet ef database update --project ClinicBooking.Infrastructure --startup-project ClinicBooking.Web
   ```
   (If `dotnet ef` isn't installed: `dotnet tool install --global dotnet-ef`.)

6. **Run**
   ```bash
   dotnet run --project ClinicBooking.Web
   ```
   Browse to the printed `https://localhost:xxxx` URL. You should be able to
   register a patient account, log in, and log out.

## Project layout

```
ClinicBooking.sln
ClinicBooking.Domain/          # Entities, Enums, Common — no dependencies
ClinicBooking.Application/     # DTOs, Services, Validators, Mappings (Phase 3+)
ClinicBooking.Infrastructure/  # DbContext, EF configurations, Identity, Seeding
ClinicBooking.Web/             # Controllers, Views, ViewModels, Program.cs
```
