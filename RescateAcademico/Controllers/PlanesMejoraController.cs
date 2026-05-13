using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;
using RescateAcademico.Services;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Administrador,Tutor,Autoridad")]
    public class PlanesMejoraController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StudentAccessService _studentAccessService;
        private readonly NotificationService _notificationService;
        private readonly RiskEvaluationService _riskEvaluationService;

        public PlanesMejoraController(
            ApplicationDbContext context,
            StudentAccessService studentAccessService,
            NotificationService notificationService,
            RiskEvaluationService riskEvaluationService)
        {
            _context = context;
            _studentAccessService = studentAccessService;
            _notificationService = notificationService;
            _riskEvaluationService = riskEvaluationService;
        }

        public async Task<IActionResult> Index(string? matricula)
        {
            var query = _context.PlanesMejora
                .Include(p => p.Alumno)
                .Include(p => p.Tutor)
                .AsQueryable();

            if (!string.IsNullOrEmpty(matricula))
                query = query.Where(p => p.AlumnoMatricula == matricula);

            var matriculasVisibles = await _studentAccessService.GetVisibleMatriculasAsync();
            query = query.Where(p => matriculasVisibles.Contains(p.AlumnoMatricula));

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
            if (!await _studentAccessService.CanAccessAlumnoAsync(plan.AlumnoMatricula)) return Forbid();
            return View(plan);
        }

        [Authorize(Roles = "Administrador,Tutor")]
        public async Task<IActionResult> Crear(string? matricula)
        {
            if (string.IsNullOrEmpty(matricula))
            {
                TempData["Info"] = "Selecciona un alumno para crear un plan de mejora.";
                return RedirectToAction("Index", "Profesor");
            }

            var alumno = await _context.Alumnos
                .Include(a => a.Calificaciones).ThenInclude(c => c.Materia)
                .FirstOrDefaultAsync(a => a.Matricula == matricula);

            if (alumno == null || !await _studentAccessService.CanAccessAlumnoAsync(matricula))
            {
                TempData["Error"] = "Alumno no encontrado o no tienes acceso.";
                return RedirectToAction("Index", "Profesor");
            }

            var sugerencias = _riskEvaluationService.GenerarSugerencias(alumno);
            var factores = _riskEvaluationService.ObtenerFactoresRiesgo(alumno);

            var model = new PlanMejoraViewModel
            {
                AlumnoMatricula = matricula,
                Recomendaciones = BuildPlanSummary(alumno, sugerencias, factores),
                FechaCierre = DateTime.Now.AddDays(30),
                NotificarAlumno = true,
                AsesoriaAcademica = factores.Any(f => f.Contains("Promedio") || f.Contains("Materias")),
                ControlAusencias = factores.Any(f => f.Contains("Ausencias")),
                RegularizacionETS = factores.Any(f => f.Contains("Recursamientos")),
                TutoriaPersonalizada = factores.Any(f => f.Contains("Recursamientos")),
                ApoyoPsicologico = false
            };

            if (User.IsInRole("Administrador"))
            {
                ViewBag.Tutores = await _context.Tutores.Where(t => t.EstaActivo).ToListAsync();
            }
            else
            {
                var tutor = await _studentAccessService.GetCurrentTutorAsync();
                if (tutor != null) model.TutorId = tutor.Id;
                ViewBag.TutorNombre = tutor?.Nombre + " " + tutor?.Apellidos;
            }

            ViewBag.AlumnoSeleccionado = alumno;
            ViewBag.FactoresRiesgo = factores;

            return View(model);
        }

        private async Task ReloadCrearViewBag(string matricula)
        {
            var alumno = await _context.Alumnos
                .Include(a => a.Calificaciones).ThenInclude(c => c.Materia)
                .FirstOrDefaultAsync(a => a.Matricula == matricula);
            ViewBag.AlumnoSeleccionado = alumno;
            if (alumno != null)
                ViewBag.FactoresRiesgo = _riskEvaluationService.ObtenerFactoresRiesgo(alumno);

            if (User.IsInRole("Administrador"))
                ViewBag.Tutores = await _context.Tutores.Where(t => t.EstaActivo).ToListAsync();
            else
            {
                var tutor = await _studentAccessService.GetCurrentTutorAsync();
                ViewBag.TutorNombre = tutor?.Nombre + " " + tutor?.Apellidos;
            }
        }

        private static string BuildPlanSummary(Alumno alumno, List<string> sugerencias, List<string> factores)
        {
            var sb = new StringBuilder();
            var nombre = $"{alumno.Nombre} {alumno.Apellidos}";
            sb.AppendLine($"Plan de mejora academica para {nombre}, {alumno.Carrera} (Semestre {alumno.SemestreActual}).");
            sb.AppendLine();
            if (alumno.PromedioGlobal < 6m)
                sb.AppendLine($"Presenta promedio critico de {alumno.PromedioGlobal:F2}. Se requiere intervencion inmediata.");
            else if (alumno.PromedioGlobal < 7m)
                sb.AppendLine($"Promedio actual de {alumno.PromedioGlobal:F2}, por debajo del minimo recomendado.");
            if (alumno.MateriasReprobadas > 0)
                sb.AppendLine($"Acumula {alumno.MateriasReprobadas} materia(s) reprobada(s).");
            if (alumno.Ausencias > 0)
                sb.AppendLine($"Registro de {alumno.Ausencias} inasistencia(s).");
            sb.AppendLine();
            sb.AppendLine("Estrategia sugerida:");
            foreach (var s in sugerencias.Take(3))
                sb.AppendLine($"- {s}");
            return sb.ToString().Trim();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Tutor")]
        public async Task<IActionResult> Crear(PlanMejoraViewModel model)
        {
            if (!User.IsInRole("Administrador"))
            {
                var tutor = await _studentAccessService.GetCurrentTutorAsync();
                if (tutor != null) model.TutorId = tutor.Id;
            }

            if (!ModelState.IsValid)
            {
                await ReloadCrearViewBag(model.AlumnoMatricula);
                return View(model);
            }

            if (!await _studentAccessService.CanAccessAlumnoAsync(model.AlumnoMatricula)) return Forbid();

            if (!model.EsPersonalizado)
            {
                var types = new List<string>();
                if (model.AsesoriaAcademica) types.Add("Asesoria academica");
                if (model.ControlAusencias) types.Add("Control de ausencias");
                if (model.RegularizacionETS) types.Add("Regularizacion ETS");
                if (model.ApoyoPsicologico) types.Add("Apoyo psicologico");
                if (model.TutoriaPersonalizada) types.Add("Tutoria personalizada");

                model.Metas = model.Recomendaciones;
                model.AccionesTomadas = types.Count > 0
                    ? $"Intervenciones programadas: {string.Join(", ", types)}. Fecha de inicio: {DateTime.Now:dd/MM/yyyy}."
                    : $"Plan generado automaticamente. Inicio: {DateTime.Now:dd/MM/yyyy}.";
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
                _notificationService.Add(
                    alumno.UserId,
                    "Nuevo Plan de Mejora Asignado",
                    "Se ha creado un plan de mejora personalizado para ti. Revisa las recomendaciones y metas establecidas.",
                    "Informacion",
                    $"/PlanesMejora/Detalles/{plan.Id}");
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Plan de mejora creado exitosamente";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Administrador,Tutor")]
        public async Task<IActionResult> Editar(int id)
        {
            var plan = await _context.PlanesMejora
                .Include(p => p.Alumno)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null) return NotFound();
            if (!await _studentAccessService.CanAccessAlumnoAsync(plan.AlumnoMatricula)) return Forbid();

            if (User.IsInRole("Administrador"))
            {
                ViewBag.Tutores = await _context.Tutores.Where(t => t.EstaActivo).ToListAsync();
            }
            else
            {
                var tutor = await _studentAccessService.GetCurrentTutorAsync();
                ViewBag.TutorNombre = tutor?.Nombre + " " + tutor?.Apellidos;
            }

            ViewBag.AlumnoSeleccionado = plan.Alumno;

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
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Tutor")]
        public async Task<IActionResult> Editar(PlanMejoraViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (User.IsInRole("Administrador"))
                    ViewBag.Tutores = await _context.Tutores.Where(t => t.EstaActivo).ToListAsync();
                return View(model);
            }

            var plan = await _context.PlanesMejora.FindAsync(model.Id);
            if (plan == null) return NotFound();
            if (!await _studentAccessService.CanAccessAlumnoAsync(plan.AlumnoMatricula)) return Forbid();

            plan.TutorId = User.IsInRole("Administrador") ? model.TutorId : plan.TutorId;
            plan.Recomendaciones = model.Recomendaciones;
            plan.Metas = model.Metas;
            plan.AccionesTomadas = model.AccionesTomadas;
            plan.FechaCierre = model.FechaCierre;
            plan.Estado = User.IsInRole("Administrador") ? model.Estado : plan.Estado;

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
            if (!await _studentAccessService.CanAccessAlumnoAsync(plan.AlumnoMatricula)) return Forbid();
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

        public bool AsesoriaAcademica { get; set; }
        public bool ControlAusencias { get; set; }
        public bool RegularizacionETS { get; set; }
        public bool ApoyoPsicologico { get; set; }
        public bool TutoriaPersonalizada { get; set; }
        public bool EsPersonalizado { get; set; }
        public bool NotificarAlumno { get; set; } = true;
    }
}
