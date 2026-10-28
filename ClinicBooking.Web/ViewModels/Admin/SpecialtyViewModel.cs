using System.ComponentModel.DataAnnotations;

namespace ClinicBooking.Web.ViewModels.Admin;

public class SpecialtyViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Specialty name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Display(Name = "Is Active?")]
    public bool IsActive { get; set; } = true;
}

