using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;
using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Administrador,Tutor,Autoridad")]
    public class PlanesMejoraController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlanesMejoraController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? matricula)
        {
            var query = _context.PlanesMejora
                .Include(p => p.Alumno)
                .Include(p => p.Tutor)
                .AsQueryable();

            if (!string.IsNullOrEmpty(matricula))
                query = query.Where(p => p.AlumnoMatricula == matricula);

            var planes = await query.OrderByDescending(p => p.FechaCreacion).ToListAsync();
            ViewBag.MatriculaFiltro = matricula;
            return View(planes);
        }

        public async Task<IActionResult> Detalles(int id)
        {
            var plan = await _context.PlanesMejora
                .Include(p => p.Alumno)
                .Include(p => p.Tutor)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null) return NotFound();
            return View(plan);
        }

        [Authorize(Roles = "Administrador,Tutor")]
        public async Task<IActionResult> Crear(string? matricula)
        {
            ViewBag.Alumnos = await _context.Alumnos
                .Where(a => a.RiesgoAcademico == "Rojo" || a.RiesgoAcademico == "Amarillo")
                .OrderBy(a => a.Apellidos)
                .ToListAsync();

            ViewBag.Tutores = await _context.Tutores.Where(t => t.EstaActivo).ToListAsync();

            var model = new PlanMejoraViewModel
            {
                AlumnoMatricula = matricula ?? ""
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Tutor")]
        public async Task<IActionResult> Crear(PlanMejoraViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Alumnos = await _context.Alumnos
                    .Where(a => a.RiesgoAcademico == "Rojo" || a.RiesgoAcademico == "Amarillo")
                    .OrderBy(a => a.Apellidos)
                    .ToListAsync();
                ViewBag.Tutores = await _context.Tutores.Where(t => t.EstaActivo).ToListAsync();
                return View(model);
            }

            var plan = new PlanMejora
            {
                AlumnoMatricula = model.AlumnoMatricula,
                TutorId = model.TutorId,
                Recomendaciones = model.Recomendaciones,
                Metas = model.Metas,
                AccionesTomadas = model.AccionesTomadas,
                FechaCierre = model.FechaCierre,
                Estado = "Activo"
            };

            _context.PlanesMejora.Add(plan);
            await _context.SaveChangesAsync();

            // Notificar al alumno
            var alumno = await _context.Alumnos.FindAsync(model.AlumnoMatricula);
            if (alumno != null && !string.IsNullOrEmpty(alumno.UserId))
            {
                _context.Notificaciones.Add(new Notificacion
                {
                    UserId = alumno.UserId,
                    Titulo = "Nuevo Plan de Mejora Asignado",
                    Mensaje = "Se ha creado un plan de mejora personalizado para ti. Revisa las recomendaciones y metas establecidas.",
                    Tipo = "Informacion",
                    Enlace = $"/PlanesMejora/Detalles/{plan.Id}"
                });
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Plan de mejora creado exitosamente";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Administrador,Tutor")]
        public async Task<IActionResult> Editar(int id)
        {
            var plan = await _context.PlanesMejora.FindAsync(id);
            if (plan == null) return NotFound();

            ViewBag.Tutores = await _context.Tutores.Where(t => t.EstaActivo).ToListAsync();

            var model = new PlanMejoraViewModel
            {
                Id = plan.Id,
                AlumnoMatricula = plan.AlumnoMatricula,
                TutorId = plan.TutorId,
                Recomendaciones = plan.Recomendaciones,
                Metas = plan.Metas,
                AccionesTomadas = plan.AccionesTomadas,
                FechaCierre = plan.FechaCierre,
                Estado = plan.Estado
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Tutor")]
        public async Task<IActionResult> Editar(PlanMejoraViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Tutores = await _context.Tutores.Where(t => t.EstaActivo).ToListAsync();
                return View(model);
            }

            var plan = await _context.PlanesMejora.FindAsync(model.Id);
            if (plan == null) return NotFound();

            plan.TutorId = model.TutorId;
            plan.Recomendaciones = model.Recomendaciones;
            plan.Metas = model.Metas;
            plan.AccionesTomadas = model.AccionesTomadas;
            plan.FechaCierre = model.FechaCierre;
            plan.Estado = model.Estado;

            _context.PlanesMejora.Update(plan);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Plan de mejora actualizado";
            return RedirectToAction("Detalles", new { id = plan.Id });
        }

        public async Task<IActionResult> Seguimiento(int id)
        {
            var plan = await _context.PlanesMejora
                .Include(p => p.Alumno)
                .Include(p => p.Tutor)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null) return NotFound();
            return View(plan);
        }
    }

    public class PlanMejoraViewModel
    {
        public int Id { get; set; }

        [Required]
        public string AlumnoMatricula { get; set; } = string.Empty;

        public int? TutorId { get; set; }

        public string? Recomendaciones { get; set; }

        public string? Metas { get; set; }

        public string? AccionesTomadas { get; set; }

        public DateTime? FechaCierre { get; set; }

        public string Estado { get; set; } = "Activo";
    }
}
