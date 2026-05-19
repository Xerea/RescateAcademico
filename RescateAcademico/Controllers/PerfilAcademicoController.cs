using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Filters;
using RescateAcademico.Models;
using RescateAcademico.Services;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class PerfilAcademicoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RiskEvaluationService _riskEvaluationService;
        private readonly StudentAccessService _studentAccessService;

        public PerfilAcademicoController(
            ApplicationDbContext context,
            RiskEvaluationService riskEvaluationService,
            StudentAccessService studentAccessService)
        {
            _context = context;
            _riskEvaluationService = riskEvaluationService;
            _studentAccessService = studentAccessService;
        }

        public async Task<IActionResult> MiPerfil()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var alumno = await LoadAlumnoPerfilQuery()
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (alumno == null)
            {
                TempData["Error"] = "No tienes un perfil de alumno asociado. Contacta a administracion.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = BuildPerfilViewModel(alumno, esTutor: false);
            await CargarAnalisisInteligente(viewModel);
            return View(viewModel);
        }

        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> VerTutorado(string matricula)
        {
            if (!await _studentAccessService.CanAccessAlumnoAsync(matricula))
            {
                TempData["Error"] = "No tienes acceso a este alumno.";
                return RedirectToAction("MisTutorados", "Alumnos");
            }

            var alumno = await LoadAlumnoPerfilQuery()
                .FirstOrDefaultAsync(a => a.Matricula == matricula);

            if (alumno == null) return NotFound();

            var viewModel = BuildPerfilViewModel(alumno, esTutor: true);
            await CargarAnalisisInteligente(viewModel);
            return View("MiPerfil", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Tutor,Administrador,Autoridad")]
        [AuditLog(Accion = "Guardar Analisis Perfil", Tabla = "Predicciones")]
        public async Task<IActionResult> GuardarAnalisisInteligente(string matricula)
        {
            if (!await _studentAccessService.CanAccessAlumnoAsync(matricula))
            {
                return Forbid();
            }

            var alumno = await _context.Alumnos.FindAsync(matricula);
            if (alumno == null) return NotFound();

            var probabilidad = _riskEvaluationService.CalcularProbabilidadDesercion(alumno);
            var nivel = _riskEvaluationService.CalcularNivelPredictivo(probabilidad);
            var sugerencias = _riskEvaluationService.GenerarSugerencias(alumno);
            var factores = _riskEvaluationService.ObtenerFactoresRiesgo(alumno);

            var hoy = DateTime.Now.Date;
            var prediccionesHoy = await _context.PrediccionesDesercion
                .Where(p => p.AlumnoMatricula == alumno.Matricula && p.FechaPrediccion.Date == hoy)
                .ToListAsync();
            if (prediccionesHoy.Any())
            {
                _context.PrediccionesDesercion.RemoveRange(prediccionesHoy);
            }

            _context.PrediccionesDesercion.Add(new PrediccionDesercion
            {
                AlumnoMatricula = alumno.Matricula,
                ProbabilidadDesercion = probabilidad,
                NivelRiesgo = nivel,
                FactoresDetectados = string.Join("; ", factores),
                Recomendaciones = string.Join(" | ", sugerencias.Take(3)),
                PromedioParcial = alumno.PromedioGlobal,
                MateriasReprobadas = alumno.MateriasReprobadas ?? 0,
                PeriodoEvaluado = DateTime.Now.ToString("yyyy-MM")
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Analisis academico guardado correctamente.";
            if (User.IsInRole("Tutor"))
            {
                return RedirectToAction("VerTutorado", new { matricula });
            }

            return RedirectToAction("Detalles", "Alumnos", new { id = matricula });
        }

        private IQueryable<Alumno> LoadAlumnoPerfilQuery()
        {
            return _context.Alumnos
                .Include(a => a.Calificaciones)
                    .ThenInclude(c => c.Materia)
                .Include(a => a.TutoresAsignados)
                    .ThenInclude(at => at.Tutor)
                .Include(a => a.Postulaciones)
                    .ThenInclude(p => p.Proyecto);
        }

        private PerfilAcademicoViewModel BuildPerfilViewModel(Alumno alumno, bool esTutor)
        {
            var periodosConCalif = alumno.Calificaciones
                .Select(c => c.Periodo)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .OrderByDescending(p => p)
                .ToList();
            var periodoActual = periodosConCalif.FirstOrDefault() ?? "2026-A";

            var materiasPeriodoActual = alumno.Calificaciones
                .Where(c => c.Periodo == periodoActual)
                .ToList();

            List<MateriaSimulacion> materiasSimulacion;
            if (materiasPeriodoActual.Any())
            {
                materiasSimulacion = materiasPeriodoActual
                    .Select(c => new MateriaSimulacion
                    {
                        Nombre = c.Materia?.Nombre ?? $"Materia {c.MateriaId}",
                        CalificacionActual = c.Valor ?? 7.0m,
                        TieneCalificacion = true
                    })
                    .ToList();
            }
            else
            {
                materiasSimulacion = Enumerable.Range(1, alumno.CargaAcademicaActual ?? 5)
                    .Select(i => new MateriaSimulacion
                    {
                        Nombre = $"Materia {i}",
                        CalificacionActual = 7.0m,
                        TieneCalificacion = false
                    })
                    .ToList();
            }

            var califPrevias = alumno.Calificaciones
                .Where(c => c.Periodo != periodoActual)
                .ToList();

            return new PerfilAcademicoViewModel
            {
                Alumno = alumno,
                MateriasAprobadas = alumno.Calificaciones.Count(c => c.Aprobada),
                MateriasReprobadas = alumno.Calificaciones.Count(c => !c.Aprobada && !c.EsETS),
                EtsPresentados = alumno.Calificaciones.Count(c => c.EsETS),
                Recursamientos = alumno.Calificaciones.Count(c => c.VecesCursada > 1),
                PromedioPorSemestre = alumno.Calificaciones
                    .Where(c => !string.IsNullOrEmpty(c.Periodo))
                    .GroupBy(c => c.Periodo)
                    .Select(g => new SemestrePromedio
                    {
                        Periodo = g.Key ?? "",
                        Promedio = g.Average(c => c.Valor ?? 0),
                        Materias = g.Count()
                    })
                    .OrderByDescending(s => s.Periodo)
                    .ToList(),
                NivelRiesgo = _riskEvaluationService.CalcularRiesgo(alumno),
                Sugerencias = _riskEvaluationService.GenerarSugerencias(alumno),
                EsTutor = esTutor,
                MateriasSimulacion = materiasSimulacion,
                SumaPrevia = califPrevias.Sum(c => c.Valor ?? 0),
                TotalMateriasPrevias = califPrevias.Count
            };
        }

        private async Task CargarAnalisisInteligente(PerfilAcademicoViewModel viewModel)
        {
            if (viewModel.Alumno == null)
            {
                return;
            }

            var alumno = viewModel.Alumno;
            var probabilidad = _riskEvaluationService.CalcularProbabilidadDesercion(alumno);

            viewModel.ProbabilidadDesercion = probabilidad;
            viewModel.NivelRiesgoPredictivo = _riskEvaluationService.CalcularNivelPredictivo(probabilidad);

            var convocatorias = await _context.Convocatorias
                .Include(c => c.Proyecto)
                .Where(c => c.EstaActiva && c.ValidadaPorAcademia && c.FechaCierre >= DateTime.Now)
                .OrderBy(c => c.FechaCierre)
                .ToListAsync();

            viewModel.CompatibilidadConvocatorias = convocatorias
                .Select(c => EvaluarCompatibilidad(alumno, c))
                .OrderByDescending(c => c.Puntaje)
                .Take(3)
                .ToList();

            viewModel.UmbralesPromedio = convocatorias
                .Select(c => c.PromedioMinimo)
                .ToList();

            viewModel.ConvocatoriasElegiblesAhora = convocatorias.Count(c =>
                (!c.PromedioMinimo.HasValue || alumno.PromedioGlobal >= c.PromedioMinimo.Value) &&
                (!c.SemestreMinimo.HasValue || alumno.SemestreActual >= c.SemestreMinimo.Value) &&
                (string.IsNullOrEmpty(c.CarreraRequerida) || c.CarreraRequerida == alumno.Carrera));
        }

        private CompatibilidadConvocatoria EvaluarCompatibilidad(Alumno alumno, Convocatoria convocatoria)
        {
            decimal puntaje = 100m;
            var razones = new List<string>();

            if (convocatoria.PromedioMinimo.HasValue)
            {
                if (alumno.PromedioGlobal >= convocatoria.PromedioMinimo.Value)
                    razones.Add("Cumple promedio minimo");
                else
                {
                    puntaje -= 35m;
                    razones.Add("No cumple promedio minimo");
                }
            }

            if (convocatoria.SemestreMinimo.HasValue)
            {
                if (alumno.SemestreActual >= convocatoria.SemestreMinimo.Value)
                    razones.Add("Semestre suficiente");
                else
                {
                    puntaje -= 20m;
                    razones.Add("Semestre inferior al requerido");
                }
            }

            if (!string.IsNullOrWhiteSpace(convocatoria.CarreraRequerida))
            {
                if (string.Equals(alumno.Carrera, convocatoria.CarreraRequerida, StringComparison.OrdinalIgnoreCase))
                    razones.Add("Carrera alineada");
                else
                {
                    puntaje -= 20m;
                    razones.Add("Carrera distinta a la requerida");
                }
            }

            if ((alumno.CargaAcademicaActual ?? 0) >= 7)
            {
                puntaje -= 10m;
                razones.Add("Carga academica alta");
            }

            if ((alumno.MateriasReprobadas ?? 0) >= 3)
            {
                puntaje -= 10m;
                razones.Add("Riesgo academico por reprobacion");
            }

            puntaje = Math.Clamp(puntaje, 0m, 100m);

            return new CompatibilidadConvocatoria
            {
                ConvocatoriaId = convocatoria.Id,
                ProyectoId = convocatoria.ProyectoId,
                Titulo = convocatoria.Titulo,
                Tipo = convocatoria.Tipo,
                Puntaje = puntaje,
                Explicacion = string.Join(". ", razones)
            };
        }
    }

    public class PerfilAcademicoViewModel
    {
        public Alumno? Alumno { get; set; }
        public int MateriasAprobadas { get; set; }
        public int MateriasReprobadas { get; set; }
        public int EtsPresentados { get; set; }
        public int Recursamientos { get; set; }
        public List<SemestrePromedio> PromedioPorSemestre { get; set; } = new();
        public string NivelRiesgo { get; set; } = "Verde";
        public List<string> Sugerencias { get; set; } = new();
        public bool EsTutor { get; set; } = false;
        public decimal ProbabilidadDesercion { get; set; }
        public string NivelRiesgoPredictivo { get; set; } = "Bajo";
        public List<CompatibilidadConvocatoria> CompatibilidadConvocatorias { get; set; } = new();
        public List<MateriaSimulacion> MateriasSimulacion { get; set; } = new();
        public decimal SumaPrevia { get; set; }
        public int TotalMateriasPrevias { get; set; }
        public List<decimal?> UmbralesPromedio { get; set; } = new();
        public int ConvocatoriasElegiblesAhora { get; set; }
    }

    public class MateriaSimulacion
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal CalificacionActual { get; set; }
        public bool TieneCalificacion { get; set; }
    }

    public class SemestrePromedio
    {
        public string Periodo { get; set; } = "";
        public decimal Promedio { get; set; }
        public int Materias { get; set; }
    }

    public class CompatibilidadConvocatoria
    {
        public int? ConvocatoriaId { get; set; }
        public int? ProyectoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public decimal Puntaje { get; set; }
        public string Explicacion { get; set; } = string.Empty;
    }
}
