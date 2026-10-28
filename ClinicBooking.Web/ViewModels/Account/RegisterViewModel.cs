using System.ComponentModel.DataAnnotations;

namespace ClinicBooking.Web.ViewModels.Account;

/// <summary>
/// Self-registration is for Patients only. Doctor accounts are created by an
/// Admin (Phase 3 - Doctor Management), since doctors must be vetted before
/// they can receive bookings.
/// </summary>
public class RegisterViewModel
{
    [Required, StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = default!;

    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required, Phone, StringLength(30)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = default!;

    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = default!;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = default!;
}
