# Clinic Booking & Management System

An ASP.NET Core MVC application for clinic appointment booking and day-to-day clinic management. The solution uses a layered architecture with Domain, Application, Infrastructure, Web, and Tests projects.

- **Runtime:** .NET 8, ASP.NET Core MVC (areas)
- **Data:** EF Core 8 with SQL Server (LocalDB by default)
- **Identity:** ASP.NET Core Identity with cookie authentication and role-based policies
- **Mapping:** AutoMapper
- **Logging:** Serilog (console)
- **UI:** Bootstrap 5.3 and Font Awesome, served from local `wwwroot/lib` (no external CDN)

## Project structure

```
ClinicBooking.sln
├─ ClinicBooking.Domain          # Entities, enums, BaseEntity (no dependencies)
├─ ClinicBooking.Application     # DTOs, service interfaces, AutoMapper profile, policy/options
├─ ClinicBooking.Infrastructure  # EF Core DbContext, migrations, entity configs, services, Identity, email
├─ ClinicBooking.Web             # MVC host: Areas (Admin/Doctor/Patient), controllers, views, view models
└─ ClinicBooking.Tests           # xUnit integration tests running against a throwaway LocalDB database
```

The Web project is split into three areas plus the public site:

| Area | Screens |
| --- | --- |
| **Admin** | Dashboard, Doctors, Specialties, Patients, Appointments (incl. cash collection), Audit Log |
| **Doctor** | Dashboard, Appointments, Weekly Schedule, Medical Records |
| **Patient** | Dashboard, Find a Doctor / Booking, My Appointments, Medical Records, Notifications |
| **Public** | Home, Account (register, login, forgot/reset password) |

## Current features

- Patient registration and sign-in through ASP.NET Core Identity.
- Self-service password reset: a "forgot password" flow emails a time-limited reset link.
- Admin screens for doctors, specialties, patients, appointments, and cash collection.
- Doctor screens for weekly availability, appointment management, medical records, and prescriptions.
- Patient screens for doctor selection, appointment booking, appointment history, and medical records.
- Patients can search the doctor list by name, filter by specialty and consultation-fee range, and sort by name or fee.
- The doctor's appointments screen supports status/date tabs (All, Today, Upcoming, Past, Pending, Confirmed, Completed, Cancelled), patient-name search, a date range, sorting, pagination, and headline stats (today, pending, collected, outstanding).
- Upcoming appointments get an automatic reminder (email plus an in-app notification) a configurable number of hours before they start. Patients have a notifications inbox with an unread badge in the navigation bar.
- Patients can cancel or reschedule an appointment online up to a configurable cutoff (default: 2 hours) before it starts. Rescheduling keeps the same payment and returns the appointment to Pending so the clinic re-confirms the new time.
- Appointment availability and server-side booking checks, including time-slot alignment, past appointments, and overlapping appointments.
- SQL Server persistence with EF Core migrations and a filtered index to prevent duplicate doctor/time bookings.
- An immutable audit trail: every created, updated, or deleted domain entity is recorded with the acting user, IP address, and the changed fields, and can be reviewed by admins under **Admin → Audit Log**.
- Local browser assets in `ClinicBooking.Web/wwwroot/lib` and `wwwroot/webfonts`; no external CDN is required for the site's styles, scripts, or icons.

Electronic payments are not available. The application accepts cash at the clinic and allows staff to record collection. A payment provider must be integrated before enabling online payments.

## Architecture notes

- **Request flow:** controller → application service interface (`ClinicBooking.Application`) → implementation (`ClinicBooking.Infrastructure`) → EF Core.
- **Data access:** a generic `IRepository<T>` plus `IUnitOfWork`, implemented in Infrastructure. Entity configuration lives in `IEntityTypeConfiguration` classes.
- **Soft delete and query filters:** entities derive from `BaseEntity` (`IsDeleted`, `CreatedAt`, `UpdatedAt`); query filters hide soft-deleted records and cascade through required relationships (for example, a soft-deleted doctor hides their appointments).
- **Concurrency:** `Appointment.RowVersion` is a SQL Server `rowversion`, and the `IX_Appointments_NoDoubleBooking` filtered unique index (ignoring cancelled rows) is the database-level guard against double-booking.
- **Audit trail:** `ApplicationDbContext.SaveChanges` inspects the change tracker and writes an `AuditLog` row for each created/updated/deleted entity, capturing the acting user, IP address, and changed fields.
- **Background work:** a hosted service (`AppointmentReminderBackgroundService`) runs the reminder job on a timer; the rules live in a testable service.

## Requirements

- .NET 8 SDK
- SQL Server (LocalDB is configured by default for development)
- `dotnet-ef` tool if you need to apply migrations from the CLI

## Run locally

From the solution directory:

```powershell
dotnet restore
dotnet build
dotnet ef database update --project ClinicBooking.Infrastructure --startup-project ClinicBooking.Web
dotnet run --project ClinicBooking.Web
```

The app listens on **https://localhost:60930** and **http://localhost:60931** (see `ClinicBooking.Web/Properties/launchSettings.json`).

The repository already contains the initial and payment-module migrations. Apply them with `database update`; do not create another initial migration for an existing database.

The connection string is in `ClinicBooking.Web/appsettings.json` (default: LocalDB database `ClinicBookingDb`). Change it for a different SQL Server instance. Do not put real credentials in tracked settings files.

## Configuration reference

All settings live in `ClinicBooking.Web/appsettings.json` (and `appsettings.Development.json` for development), overridable with User Secrets or environment variables.

| Section | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string. |
| `Email` | SMTP settings and sender address. When `SmtpHost` is empty the app uses a log-only sender. |
| `AppointmentPolicy:CancellationCutoffHours` | Hours before the start time during which patients may no longer cancel/reschedule online. |
| `AppointmentReminder` | Enables and tunes the reminder background job. |
| `Serilog` | Log level configuration. |
| `SeedAdmin` | Optional admin account created/synchronized on startup. |
| `SeedDemo:Password` | Password used for the development demo accounts (Development only). |

## Email

Password-reset and reminder emails are sent through the `IEmailSender` abstraction. Configure the `Email` section in `ClinicBooking.Web/appsettings.json` (or User Secrets) to use a real SMTP server:

```powershell
cd ClinicBooking.Web
dotnet user-secrets set "Email:SmtpHost" "smtp.example.com"
dotnet user-secrets set "Email:SmtpPort" "587"
dotnet user-secrets set "Email:SmtpUserName" "<user>"
dotnet user-secrets set "Email:SmtpPassword" "<password>"
dotnet user-secrets set "Email:FromAddress" "no-reply@example.com"
```

When `Email:SmtpHost` is empty, the app falls back to a log-only sender: no mail is sent, but the full message (including the reset link) is written to the application log. This makes the forgot-password and reminder flows usable in local development without an SMTP server.

## Appointment change policy

Patients may cancel or reschedule an appointment online only while it is still far enough ahead. The cutoff is configured in `ClinicBooking.Web/appsettings.json`:

```json
"AppointmentPolicy": {
  "CancellationCutoffHours": 2
}
```

Within the window, the appointment is cancelled or moved and, when rescheduled, reset to `Pending` for clinic re-confirmation. Outside the window the online actions are hidden and the server rejects the request; staff can still change the appointment from the doctor/admin screens. The cutoff is enforced in the service layer (`AppointmentService`), not only in the UI.

## Appointment reminders

A background job reminds patients about upcoming appointments. It runs on a timer, finds appointments that are still ahead but start within the configured window, and sends each patient an email plus an in-app notification. Reminders are sent once per appointment. Configuration lives in `ClinicBooking.Web/appsettings.json`:

```json
"AppointmentReminder": {
  "Enabled": true,
  "HoursBeforeAppointment": 24,
  "CheckIntervalMinutes": 30
}
```

- `Enabled` turns the background job on or off.
- `HoursBeforeAppointment` is how far ahead of the appointment the reminder goes out.
- `CheckIntervalMinutes` is how often the job looks for appointments that are due.

Reminder rules live in `AppointmentReminderService` (Infrastructure) so they are unit-tested; the hosted `AppointmentReminderBackgroundService` only provides the timer. Duplicate reminders are prevented by checking for an existing reminder notification, so no extra database column is needed. Patients see their notifications under **My Account → Notifications**, with an unread count shown on the bell in the navigation bar.

## Notifications

Notifications are stored in the `Notifications` table and surfaced in the patient area. Patients can open their inbox, view the related appointment, mark a single item as read, or mark all as read. The navigation bell shows the unread count. Notifications are created by the reminder background job; the development demo data also seeds a few sample notifications for doctors and patients.

## Doctor appointments screen

The doctor's appointments list (`Doctor/Appointment/Index`) is filterable and paginated:

- **Tabs:** All, Today, Upcoming, Past, and one tab per status (Pending, Confirmed, Completed, Cancelled), each with a count.
- **Search:** by patient name (partial, case-insensitive).
- **Date range:** from/to date.
- **Sort:** newest first (default), oldest first, or patient name.
- **Stats cards:** today's appointments, pending count, collected amount, and outstanding amount.
- **Pagination:** 10 per page, preserving the active filters.

Filtering, sorting, pagination, and stats are computed in `AppointmentService` (`SearchAppointmentsForDoctorAsync`, `GetAppointmentStatsForDoctorAsync`) so they are unit-tested and the whole list is never loaded into memory.

## Seed an administrator

The application creates an administrator only when these settings are supplied. In development, use User Secrets from the Web project directory and choose a strong, unique password:

```powershell
cd ClinicBooking.Web
dotnet user-secrets init
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com"
dotnet user-secrets set "SeedAdmin:Password" "<your-strong-unique-password>"
dotnet user-secrets set "SeedAdmin:FullName" "Clinic Administrator"
```

The seed routine synchronizes the configured password for that administrator at startup. Keep these settings in a secret store outside development.

If you already ran an earlier version of this project, rotate the old admin credential in the existing database. Set `SeedAdmin:Email` to `admin@clinicbooking.local` with a new password in User Secrets and start the app once in Development; the seeder will reset that account. The demo accounts use an intentionally public password in Development and are refreshed to it on startup. Never use them outside a private development database.

## Development demo data

The demo dataset is created only in Development. The development profile includes a public demo password, which is displayed with the demo patient email on the home page. These accounts and their data are fictional and must not be used outside local development.

| Role | Accounts | Notes |
| --- | --- | --- |
| Doctor | `demo.doctor1@clinicbooking.local` … `demo.doctor5@clinicbooking.local` | Active, 30-minute slots, 09:00–17:00 daily |
| Patient | `demo.patient1@clinicbooking.local` … `demo.patient5@clinicbooking.local` | With appointments, records, and prescriptions |
| Admin | none by default | Create one with the `SeedAdmin` settings above |

All demo accounts share the password from `SeedDemo:Password` in `appsettings.Development.json` (`ClinicDemo2026!`).

To use a private password instead, override it with User Secrets:

```powershell
dotnet user-secrets set "SeedDemo:Password" "<your-strong-unique-password>"
```

The sample patients, doctors, appointments, and records are fictional demonstration data. The demo password is synchronized for those accounts on each Development startup.

## Tests

The `ClinicBooking.Tests` project contains automated tests for the booking, payment, policy, reminder, notification, search, and audit rules:

```powershell
dotnet test
```

The tests run against a throwaway SQL Server LocalDB database (`ClinicBookingTests_<guid>`) that is created and dropped per run, so the real provider is exercised — including the filtered unique index that prevents double-booking. LocalDB must be installed and running (it is by default on the development machine). No existing database is touched.

Covered cases include: valid booking creates a pending appointment and a pending cash payment; double-booking (same doctor/slot, and same patient/time) is rejected; electronic payments, past dates, out-of-hours slots, and inactive doctors are rejected; cancelling releases the slot for rebooking; appointment confirm/complete transitions; the cancel/reschedule cutoff policy (allowed inside the window, rejected outside, and reschedule returns the appointment to Pending while keeping its payment); doctor search by partial name, specialty, and fee range, plus fee sorting; the doctor appointment list (status/date tabs, patient-name search, date range, sorting, pagination, stats, and doctor scoping); appointment reminders (sent once per upcoming appointment, skipped for cancelled/completed/past/out-of-window appointments, and no duplicate on a second run); notification inbox read/unread handling; marking cash as collected (and rejecting a second collection, non-cash rows, and unknown payments); refunds; creating a medical record with a prescription; and the audit trail recording created/updated/deleted entities with the acting user, IP address, and changed fields.

## Security

- Authentication uses ASP.NET Core Identity cookies (`LoginPath` `/Account/Login`, 8-hour sliding expiration).
- Authorization uses role policies: `RequireAdmin`, `RequireDoctor`, and `RequirePatient` on the corresponding areas.
- Password rules: minimum length 8, at least one uppercase letter and one non-alphanumeric character; unique email required.
- State-changing actions are protected with anti-forgery tokens (`[ValidateAntiForgeryToken]`).
- Secrets (SMTP credentials, seed admin password) are read from configuration/User Secrets/environment variables only and are never hard-coded.

## Not included

Online payment processing, email confirmation at registration (`SignIn.RequireConfirmedAccount` is `false`), real-time notifications over SignalR, review submission/management screens (the `Review` entity and demo data exist but there is no UI), and deployment/container setup are not implemented.
