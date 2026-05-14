using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Dynamic backend for the Ctrl+K global search overlay. Returns alumnos
        /// matching the query (boleta or name). Restricted to roles that have a
        /// legitimate need to look up students.
        /// </summary>
        [Authorize(Roles = "Administrador,Autoridad,Tutor")]
        [HttpGet]
        public async Task<IActionResult> GlobalSearch(string? q)
        {
            q = (q ?? string.Empty).Trim();
            if (q.Length < 2)
            {
                return Json(Array.Empty<object>());
            }

            var lower = q.ToLowerInvariant();
            var alumnos = await _context.Alumnos
                .Where(a => a.Matricula.Contains(q)
                    || (a.Nombre != null && EF.Functions.Like(a.Nombre.ToLower(), "%" + lower + "%"))
                    || (a.Apellidos != null && EF.Functions.Like(a.Apellidos.ToLower(), "%" + lower + "%")))
                .OrderBy(a => a.Apellidos)
                .Take(8)
                .Select(a => new
                {
                    a.Matricula,
                    a.Nombre,
                    a.Apellidos,
                    a.Carrera,
                    a.SemestreActual
                })
                .ToListAsync();

            // Different role → different default destination
            string urlTemplate = User.IsInRole("Tutor")
                ? "/Profesor/HistorialAcademico?boleta={0}"
                : "/Alumnos/Detalles/{0}";

            var results = alumnos.Select(a => new
            {
                label = a.Nombre + " " + a.Apellidos,
                meta = $"{a.Matricula} · Sem {a.SemestreActual}",
                url = string.Format(urlTemplate, Uri.EscapeDataString(a.Matricula)),
                icon = "bi-person-badge",
                group = "Alumnos"
            });

            return Json(results);
        }
    }
}
