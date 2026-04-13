using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class PerfilAcademicoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PerfilAcademicoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> MiPerfil()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var alumno = await _context.Alumnos
                .Include(a => a.Calificaciones)
                    .ThenInclude(c => c.Materia)
                .Include(a => a.TutoresAsignados)
                    .ThenInclude(at => at.Tutor)
                .Include(a => a.Postulaciones)
                    .ThenInclude(p => p.Proyecto)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (alumno == null)
            {
                TempData["Error"] = "No tienes un perfil de alumno asociado. Contacta a administración.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new PerfilAcademicoViewModel
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
                NivelRiesgo = CalcularNivelRiesgo(alumno),
                Sugerencias = GenerarSugerencias(alumno)
            };

            await CargarAnalisisInteligente(viewModel);
            return View(viewModel);
        }

        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> VerTutorado(string matricula)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var tutor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);

            var asignacion = await _context.AsignacionesTutor
                .FirstOrDefaultAsync(at => at.AlumnoMatricula == matricula && at.TutorId == tutor!.Id && at.EstaActiva);

            if (asignacion == null)
            {
                TempData["Error"] = "No tienes acceso a este alumno.";
                return RedirectToAction("MisTutorados", "Alumnos");
            }

            var alumno = await _context.Alumnos
                .Include(a => a.Calificaciones)
                    .ThenInclude(c => c.Materia)
                .Include(a => a.TutoresAsignados)
                    .ThenInclude(at => at.Tutor)
                .Include(a => a.Postulaciones)
                    .ThenInclude(p => p.Proyecto)
                .FirstOrDefaultAsync(a => a.Matricula == matricula);

            if (alumno == null) return NotFound();

            var viewModel = new PerfilAcademicoViewModel
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
                NivelRiesgo = CalcularNivelRiesgo(alumno),
                Sugerencias = GenerarSugerencias(alumno),
                EsTutor = true
            };

            await CargarAnalisisInteligente(viewModel);
            return View("MiPerfil", viewModel);
        }

        private async Task CargarAnalisisInteligente(PerfilAcademicoViewModel viewModel)
        {
            if (viewModel.Alumno == null)
            {
                return;
            }

            var alumno = viewModel.Alumno;
            var probabilidad = CalcularProbabilidadDesercion(alumno);

            viewModel.ProbabilidadDesercion = probabilidad;
            viewModel.NivelRiesgoPredictivo = probabilidad >= 0.75m ? "Critico" :
                                              probabilidad >= 0.55m ? "Alto" :
                                              probabilidad >= 0.35m ? "Medio" : "Bajo";

            var convocatorias = await _context.Convocatorias
                .Include(c => c.Proyecto)
                .Where(c => c.EstaActiva && c.ValidadaPorAcademia && c.FechaCierre >= DateTime.Now)
                .OrderBy(c => c.FechaCierre)
                .ToListAsync();

            var compatibles = convocatorias
                .Select(c => EvaluarCompatibilidad(alumno, c))
                .OrderByDescending(c => c.Puntaje)
                .Take(3)
                .ToList();

            viewModel.CompatibilidadConvocatorias = compatibles;

            // Persistencia ligera para demostrar módulo IA en revisión.
            var hoy = DateTime.Now.Date;
            var prediccionesHoy = await _context.PrediccionesDesercion
                .Where(p => p.AlumnoMatricula == alumno.Matricula && p.FechaPrediccion.Date == hoy)
                .ToListAsync();
            if (prediccionesHoy.Any())
            {
                _context.PrediccionesDesercion.RemoveRange(prediccionesHoy);
            }

            var sugerenciasHoy = await _context.SugerenciasIA
                .Where(s => s.AlumnoMatricula == alumno.Matricula && s.Tipo == "ProyectoSugerido" && s.FechaGeneracion.Date == hoy)
                .ToListAsync();
            if (sugerenciasHoy.Any())
            {
                _context.SugerenciasIA.RemoveRange(sugerenciasHoy);
            }

            var prediccion = new PrediccionDesercion
            {
                AlumnoMatricula = alumno.Matricula,
                ProbabilidadDesercion = probabilidad,
                NivelRiesgo = viewModel.NivelRiesgoPredictivo,
                FactoresDetectados = string.Join("; ", ObtenerFactoresRiesgo(alumno)),
                Recomendaciones = string.Join(" | ", viewModel.Sugerencias.Take(3)),
                PromedioParcial = alumno.PromedioGlobal,
                MateriasReprobadas = alumno.MateriasReprobadas ?? viewModel.MateriasReprobadas,
                PeriodoEvaluado = DateTime.Now.ToString("yyyy-MM")
            };
            _context.PrediccionesDesercion.Add(prediccion);

            foreach (var c in compatibles.Where(c => c.ConvocatoriaId.HasValue))
            {
                _context.SugerenciasIA.Add(new SugerenciaIA
                {
                    AlumnoMatricula = alumno.Matricula,
                    ConvocatoriaId = c.ConvocatoriaId,
                    ProyectoId = c.ProyectoId,
                    Tipo = "ProyectoSugerido",
                    Titulo = $"Compatibilidad {c.Puntaje:F1}% con {c.Titulo}",
                    Descripcion = c.Explicacion,
                    Puntuacion = c.Puntaje,
                    Razonamiento = c.Explicacion,
                    Mostrada = true
                });
            }

            await _context.SaveChangesAsync();
        }

        private string CalcularNivelRiesgo(Alumno alumno)
        {
            if (alumno.PromedioGlobal < 6.0m) return "Rojo";
            if (alumno.PromedioGlobal < 7.0m) return "Amarillo";
            return "Verde";
        }

        private List<string> GenerarSugerencias(Alumno alumno)
        {
            var sugerencias = new List<string>();

            if (alumno.PromedioGlobal < 6.0m)
                sugerencias.Add("Tu promedio está por debajo de 6.0. Se recomienda acudir a asesorías académicas.");
            
            if (alumno.PromedioGlobal < 7.0m && alumno.PromedioGlobal >= 6.0m)
                sugerencias.Add("Tu promedio requiere atención. Considera solicitar tutorías.");
            
            if (alumno.MateriasReprobadas > 3)
                sugerencias.Add($"Tienes {alumno.MateriasReprobadas} materias reprobadas. Es importante regularizarlas pronto.");

            if (alumno.CargaAcademicaActual > 6)
                sugerencias.Add("Tu carga académica es alta. Evalúa cuidadosamente antes de sumarte a un proyecto.");

            if (alumno.Recursamientos > 2)
                sugerencias.Add($"Has recursado {alumno.Recursamientos} materias. Enfócate en aprobar desde el primer intento.");

            if (sugerencias.Count == 0)
                sugerencias.Add("¡Excelente! Mantén tu buen desempeño académico.");

            return sugerencias;
        }

        private decimal CalcularProbabilidadDesercion(Alumno alumno)
        {
            decimal score = 0.1m;

            if (alumno.PromedioGlobal < 6m) score += 0.45m;
            else if (alumno.PromedioGlobal < 7m) score += 0.25m;

            var reprobadas = alumno.MateriasReprobadas ?? 0;
            var recursadas = alumno.Recursamientos ?? 0;
            var carga = alumno.CargaAcademicaActual ?? 0;

            score += Math.Min(0.2m, reprobadas * 0.04m);
            score += Math.Min(0.15m, recursadas * 0.05m);
            if (carga >= 7) score += 0.15m;

            return Math.Min(0.95m, score);
        }

        private List<string> ObtenerFactoresRiesgo(Alumno alumno)
        {
            var factores = new List<string>();
            if (alumno.PromedioGlobal < 7m) factores.Add("Promedio bajo");
            if ((alumno.MateriasReprobadas ?? 0) > 2) factores.Add("Múltiples materias reprobadas");
            if ((alumno.Recursamientos ?? 0) > 1) factores.Add("Recursamientos frecuentes");
            if ((alumno.CargaAcademicaActual ?? 0) >= 7) factores.Add("Carga académica alta");
            if (factores.Count == 0) factores.Add("Rendimiento estable");
            return factores;
        }

        private CompatibilidadConvocatoria EvaluarCompatibilidad(Alumno alumno, Convocatoria convocatoria)
        {
            decimal puntaje = 100m;
            var razones = new List<string>();

            if (convocatoria.PromedioMinimo.HasValue)
            {
                if (alumno.PromedioGlobal >= convocatoria.PromedioMinimo.Value)
                    razones.Add("Cumple promedio mínimo");
                else
                {
                    puntaje -= 35m;
                    razones.Add("No cumple promedio mínimo");
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
                razones.Add("Carga académica alta");
            }

            if ((alumno.MateriasReprobadas ?? 0) >= 3)
            {
                puntaje -= 10m;
                razones.Add("Riesgo académico por reprobación");
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
