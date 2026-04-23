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
            _predictionService.EntrenarModelo();
            var predicciones = await _predictionService.PredecirTodosAsync();
            ViewBag.ModeloEntrenado = _predictionService.IsTrained;
            return View(predicciones);
        }

        public async Task<IActionResult> Detalle(string matricula)
        {
            var alumno = await _context.Alumnos.FindAsync(matricula);
            if (alumno == null) return NotFound();

            if (!_predictionService.IsTrained)
                _predictionService.EntrenarModelo();

            var prediccion = _predictionService.Predecir(alumno);

            // Get historical predictions
            var historial = await _context.PrediccionesDesercion
                .Where(p => p.AlumnoMatricula == matricula)
                .OrderByDescending(p => p.FechaPrediccion)
                .ToListAsync();

            ViewBag.Alumno = alumno;
            ViewBag.Historial = historial;
            ViewBag.ModeloEntrenado = _predictionService.IsTrained;

            return View(prediccion);
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult Reentrenar()
        {
            _predictionService.EntrenarModelo();
            TempData["Success"] = _predictionService.IsTrained
                ? "Modelo ML.NET reentrenado exitosamente con datos actuales."
                : "No hay suficientes datos para entrenar el modelo. Usando heurísticas.";
            return RedirectToAction("Index");
        }
    }
}
