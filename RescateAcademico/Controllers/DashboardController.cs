using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new DashboardStats();

            if (User.IsInRole("Administrador") || User.IsInRole("Autoridad"))
            {
                stats.TotalAlumnos = await _context.Alumnos.CountAsync();
                stats.TotalProyectos = await _context.Proyectos.CountAsync(p => p.EstaActivo);
                stats.TotalConvocatorias = await _context.Convocatorias.CountAsync(c => c.EstaActiva);
                stats.TotalPostulaciones = await _context.Postulaciones.CountAsync();
                stats.PostulacionesPendientes = await _context.Postulaciones.CountAsync(p => p.Estado == "En Revisión");
                stats.AlumnosEnRiesgo = await _context.Alumnos.CountAsync(a => a.RiesgoAcademico == "Rojo" || a.RiesgoAcademico == "Amarillo");
                stats.TotalTutores = await _context.Tutores.CountAsync(t => t.EstaActivo);
            }

            return View(stats);
        }
    }
}
