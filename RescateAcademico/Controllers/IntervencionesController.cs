using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;
using RescateAcademico.Services;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Administrador,Tutor,Autoridad")]
    public class IntervencionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StudentAccessService _studentAccessService;

        public IntervencionesController(ApplicationDbContext context, StudentAccessService studentAccessService)
        {
            _context = context;
            _studentAccessService = studentAccessService;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.IntervencionesTutoria.Include(i => i.Alumno).AsQueryable();

            if (User.IsInRole("Tutor"))
            {
                var tutor = await _studentAccessService.GetCurrentTutorAsync();
                if (tutor == null) return NotFound();
                query = query.Where(i => i.TutorId == tutor.Id);
            }

            var intervenciones = await query.OrderByDescending(i => i.Fecha).ToListAsync();
            return View(intervenciones);
        }

        public async Task<IActionResult> PorAlumno(string matricula)
        {
            var query = _context.IntervencionesTutoria.Include(i => i.Alumno).AsQueryable();
            query = query.Where(i => i.AlumnoMatricula == matricula);

            if (User.IsInRole("Tutor"))
            {
                var tutor = await _studentAccessService.GetCurrentTutorAsync();
                if (tutor == null || !await _studentAccessService.CanAccessAlumnoAsync(matricula))
                {
                    TempData["Error"] = "No tienes acceso a este alumno.";
                    return RedirectToAction("MisTutorados", "Alumnos");
                }

                query = query.Where(i => i.TutorId == tutor!.Id);
            }

            var intervenciones = await query.OrderByDescending(i => i.Fecha).ToListAsync();
            ViewBag.Alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.Matricula == matricula);
            return View(intervenciones);
        }

        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> Crear(string matricula, int? planMejoraId = null)
        {
            if (!await _studentAccessService.CanAccessAlumnoAsync(matricula))
            {
                TempData["Error"] = "No tienes acceso a este alumno.";
                return RedirectToAction("MisTutorados", "Alumnos");
            }

            ViewBag.Matricula = matricula;
            ViewBag.Alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.Matricula == matricula);

            if (planMejoraId.HasValue)
            {
                var plan = await _context.PlanesMejora
                    .Include(p => p.Alumno)
                    .FirstOrDefaultAsync(p => p.Id == planMejoraId.Value && p.AlumnoMatricula == matricula);
                ViewBag.PlanMejora = plan;
                ViewBag.PlanMejoraId = planMejoraId;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> Crear(
            [Bind("AlumnoMatricula,Tipo,Descripcion,Resultado,RequiereSeguimiento,FechaSeguimiento,NotasSeguimiento,PlanMejoraId")] IntervencionTutoria intervencion,
            bool canalizarCosecovi = false,
            string? cosecoviCanalizacion = null)
        {
            var tutor = await _studentAccessService.GetCurrentTutorAsync();

            if (tutor == null) return NotFound();
            if (!await _studentAccessService.CanAccessAlumnoAsync(intervencion.AlumnoMatricula)) return Forbid();

            intervencion.TutorId = tutor.Id;
            intervencion.Fecha = DateTime.Now;

            _context.IntervencionesTutoria.Add(intervencion);

            // Optional: escalate to COSECOVI in the same action.
            if (canalizarCosecovi && !string.IsNullOrWhiteSpace(cosecoviCanalizacion))
            {
                var now = DateTime.Now;
                var periodo = $"{now.Year}-{(now.Month <= 6 ? "B" : "A")}";
                _context.ReportesCosecovi.Add(new ReporteCosecovi
                {
                    AlumnoMatricula = intervencion.AlumnoMatricula,
                    Periodo = periodo,
                    FechaReporte = now,
                    SituacionObservada = intervencion.Descripcion,
                    Recomendaciones = intervencion.Resultado,
                    Canalizacion = cosecoviCanalizacion,
                    Estado = "Pendiente",
                    ElaboradoPor = User.Identity?.Name
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = canalizarCosecovi && !string.IsNullOrWhiteSpace(cosecoviCanalizacion)
                ? "Intervención registrada y canalizada a COSECOVI."
                : "Intervención registrada exitosamente.";
            if (intervencion.PlanMejoraId.HasValue)
            {
                return RedirectToAction("Detalles", "PlanesMejora", new { id = intervencion.PlanMejoraId.Value });
            }
            return RedirectToAction("PorAlumno", new { matricula = intervencion.AlumnoMatricula });
        }

        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> Seguimiento(int id)
        {
            var tutor = await _studentAccessService.GetCurrentTutorAsync();

            var intervencion = await _context.IntervencionesTutoria
                .FirstOrDefaultAsync(i => i.Id == id && i.TutorId == tutor!.Id);

            if (intervencion == null) return NotFound();

            return View(intervencion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> Seguimiento(int id, DateTime fechaSeguimiento, string notas)
        {
            var tutor = await _studentAccessService.GetCurrentTutorAsync();

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
