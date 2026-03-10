using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RescateAcademico.Controllers
{
    [Authorize] // HU-RA-01 & HU-RA-03: Solo matriculados activos (logueados)
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
