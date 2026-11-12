using System.ComponentModel.DataAnnotations;

namespace ClinicBooking.Web.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;
}
