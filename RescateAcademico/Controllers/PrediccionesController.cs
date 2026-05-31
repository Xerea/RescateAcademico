using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        private readonly RiskEvaluationService _riskEvaluationService;
        private readonly IConfiguration _configuration;

        public PrediccionesController(
            ApplicationDbContext context,
            DesercionPredictionService predictionService,
            StudentAccessService studentAccessService,
            RiskEvaluationService riskEvaluationService,
            IConfiguration configuration)
        {
            _context = context;
            _predictionService = predictionService;
            _studentAccessService = studentAccessService;
            _riskEvaluationService = riskEvaluationService;
            _configuration = configuration;
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
            ViewBag.OpenAIConfigurado = !string.IsNullOrEmpty(_configuration["OPENAI_API_KEY"]);
            ViewBag.Factores = _riskEvaluationService.ObtenerFactoresRiesgo(alumno);

            return View(prediccion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("openai")]
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

        /// <summary>
        /// Real-time "what-if" recomputation of the dropout heuristic. Pure math,
        /// no AI cost, returns probability + level for a hypothetical profile.
        /// </summary>
        [HttpGet]
        public IActionResult Simular(decimal promedio, int reprobadas, int ausencias, int recursamientos, int carga)
        {
            var hipotetico = new Models.Alumno
            {
                Matricula = "SIM",
                PromedioGlobal = Math.Clamp(promedio, 0m, 10m),
                MateriasReprobadas = Math.Max(0, reprobadas),
                Ausencias = Math.Max(0, ausencias),
                Recursamientos = Math.Max(0, recursamientos),
                CargaAcademicaActual = Math.Max(0, carga)
            };

            var probabilidad = _riskEvaluationService.CalcularProbabilidadDesercion(hipotetico);
            var nivel = _riskEvaluationService.CalcularNivelPredictivo(probabilidad);
            var semaforo = _riskEvaluationService.CalcularRiesgo(hipotetico);

            return Json(new
            {
                probabilidad,
                porcentaje = Math.Round(probabilidad * 100, 1),
                nivel,
                semaforo,
                color = probabilidad > 0.7m ? "danger" : probabilidad > 0.5m ? "warning" : probabilidad > 0.3m ? "info" : "success"
            });
        }
    }
}
