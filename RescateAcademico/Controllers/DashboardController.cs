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

        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return View("Index");
        }

        [Authorize(Roles = "Tutor")]
        public IActionResult TutorDashboard()
        {
            return View("Index");
        }

        [Authorize(Roles = "Alumno")]
        public IActionResult StudentDashboard()
        {
            return View("Index");
        }

        [Authorize(Roles = "Authority")]
        public IActionResult AuthorityDashboard()
        {
            return View("Index");
        }
    }
}
