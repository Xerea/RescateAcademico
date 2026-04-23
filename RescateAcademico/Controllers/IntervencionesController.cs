using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class IntervencionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IntervencionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var tutor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null) return NotFound();

            var intervenciones = await _context.IntervencionesTutoria
                .Include(i => i.Alumno)
                .Where(i => i.TutorId == tutor.Id)
                .OrderByDescending(i => i.Fecha)
                .ToListAsync();

            return View(intervenciones);
        }

        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> PorAlumno(string matricula)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var tutor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);

            var asignacion = await _context.AsignacionesTutor
                .FirstOrDefaultAsync(a => a.AlumnoMatricula == matricula && a.TutorId == tutor!.Id && a.EstaActiva);

            if (asignacion == null)
            {
                TempData["Error"] = "No tienes acceso a este alumno.";
                return RedirectToAction("MisTutorados", "Alumnos");
            }

            var intervenciones = await _context.IntervencionesTutoria
                .Include(i => i.Alumno)
                .Where(i => i.AlumnoMatricula == matricula && i.TutorId == tutor!.Id)
                .OrderByDescending(i => i.Fecha)
                .ToListAsync();

            ViewBag.Alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.Matricula == matricula);
            return View(intervenciones);
        }

        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> Crear(string matricula)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var tutor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);

            var asignacion = await _context.AsignacionesTutor
                .FirstOrDefaultAsync(a => a.AlumnoMatricula == matricula && a.TutorId == tutor!.Id && a.EstaActiva);

            if (asignacion == null)
            {
                TempData["Error"] = "No tienes acceso a este alumno.";
                return RedirectToAction("MisTutorados", "Alumnos");
            }

            ViewBag.Matricula = matricula;
            ViewBag.Alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.Matricula == matricula);
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> Crear(IntervencionTutoria intervencion)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var tutor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null) return NotFound();

            intervencion.TutorId = tutor.Id;
            intervencion.Fecha = DateTime.Now;

            _context.IntervencionesTutoria.Add(intervencion);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Intervención registrada exitosamente.";
            return RedirectToAction("PorAlumno", new { matricula = intervencion.AlumnoMatricula });
        }

        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> Seguimiento(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var tutor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);

            var intervencion = await _context.IntervencionesTutoria
                .FirstOrDefaultAsync(i => i.Id == id && i.TutorId == tutor!.Id);

            if (intervencion == null) return NotFound();

            return View(intervencion);
        }

        [HttpPost]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> Seguimiento(int id, DateTime fechaSeguimiento, string notas)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var tutor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);

            var intervencion = await _context.IntervencionesTutoria
                .FirstOrDefaultAsync(i => i.Id == id && i.TutorId == tutor!.Id);

            if (intervencion == null) return NotFound();

            intervencion.FechaSeguimiento = fechaSeguimiento;
            intervencion.NotasSeguimiento = notas;
            intervencion.RequiereSeguimiento = false;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Seguimiento registrado.";
            return RedirectToAction("Index");
        }
    }
}
