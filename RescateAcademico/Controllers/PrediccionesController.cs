using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Filters;
using RescateAcademico.Services;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Administrador,Autoridad,Tutor")]
    public class PrediccionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DesercionPredictionService _predictionService;
        private readonly StudentAccessService _studentAccessService;

        public PrediccionesController(
            ApplicationDbContext context,
            DesercionPredictionService predictionService,
            StudentAccessService studentAccessService)
        {
            _context = context;
            _predictionService = predictionService;
            _studentAccessService = studentAccessService;
        }

        public async Task<IActionResult> Index()
        {
            var matriculas = await _studentAccessService.GetVisibleMatriculasAsync();
            var predicciones = await _predictionService.PredecirAsync(matriculas);
            return View(predicciones);
        }

        public async Task<IActionResult> Detalle(string matricula)
        {
            var alumno = await _context.Alumnos.FindAsync(matricula);
            if (alumno == null) return NotFound();
            if (!await _studentAccessService.CanAccessAlumnoAsync(matricula)) return Forbid();

            var prediccion = _predictionService.Predecir(alumno);

            var historial = await _context.PrediccionesDesercion
                .Where(p => p.AlumnoMatricula == matricula)
                .OrderByDescending(p => p.FechaPrediccion)
                .ToListAsync();

            ViewBag.Alumno = alumno;
            ViewBag.Historial = historial;

            return View(prediccion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog(Accion = "Análisis IA", Tabla = "Predicciones")]
        public async Task<IActionResult> AnalisisIA(string matricula)
        {
            var alumno = await _context.Alumnos.FindAsync(matricula);
            if (alumno == null) return NotFound();
            if (!await _studentAccessService.CanAccessAlumnoAsync(matricula)) return Forbid();

            var analisis = await _predictionService.GenerarAnalisisIAAsync(alumno);

            if (analisis == null)
            {
                TempData["Error"] = "El análisis con IA no está disponible en este momento. Verifica que la clave de API esté configurada.";
                return RedirectToAction("Detalle", new { matricula });
            }

            TempData["AnalisisIA"] = System.Text.Json.JsonSerializer.Serialize(analisis);
            return RedirectToAction("Detalle", new { matricula });
        }
    }
}
