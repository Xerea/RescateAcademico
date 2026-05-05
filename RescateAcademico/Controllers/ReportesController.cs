using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Administrador,Autoridad")]
    public class ReportesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
