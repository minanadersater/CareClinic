using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public HomeController(
        ILogger<HomeController> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        if (_environment.IsDevelopment())
        {
            var demoPassword = _configuration["SeedDemo:Password"];
            if (!string.IsNullOrWhiteSpace(demoPassword))
            {
                ViewData["DemoPatientEmail"] = "demo.patient1@clinicbooking.local";
                ViewData["DemoPassword"] = demoPassword;
            }
        }

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
