using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RescateAcademico.Controllers
{
    [Authorize] // HU-RA-01 & HU-RA-03: Solo matriculados activos (logueados)
    public class DashboardController : Controller
    {
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(15);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
