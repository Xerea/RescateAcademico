using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Services;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Administrador,Autoridad,Tutor")]
    public class PrediccionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DesercionPredictionService _predictionService;

        public PrediccionesController(ApplicationDbContext context, DesercionPredictionService predictionService)
        {
            _context = context;
            _predictionService = predictionService;
        }

        public async Task<IActionResult> Index()
        {
            var predicciones = await _predictionService.PredecirTodosAsync();
            return View(predicciones);
        }

        public async Task<IActionResult> Detalle(string matricula)
        {
            var alumno = await _context.Alumnos.FindAsync(matricula);
            if (alumno == null) return NotFound();

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
        public async Task<IActionResult> AnalisisIA(string matricula)
        {
            var alumno = await _context.Alumnos.FindAsync(matricula);
            if (alumno == null) return NotFound();

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