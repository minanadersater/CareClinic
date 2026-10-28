namespace ClinicBooking.Domain.Enums;

public enum NotificationType
{
    AppointmentBooked = 0,
    AppointmentConfirmed = 1,
    AppointmentCancelled = 2,
    AppointmentRescheduled = 3,
    AppointmentReminder = 4,
    NewAppointmentForDoctor = 5,
    General = 6
}
