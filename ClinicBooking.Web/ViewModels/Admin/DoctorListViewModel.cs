namespace ClinicBooking.Web.ViewModels.Admin;

public class DoctorListViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal ConsultationFee { get; set; }
    public IEnumerable<string> Specialties { get; set; } = new List<string>();
}

