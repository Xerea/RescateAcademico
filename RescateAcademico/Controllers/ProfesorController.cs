using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;
using RescateAcademico.Services;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Tutor")]
    public class ProfesorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StudentAccessService _studentAccessService;
        private readonly RiskEvaluationService _riskEvaluationService;

        public ProfesorController(ApplicationDbContext context, StudentAccessService studentAccessService, RiskEvaluationService riskEvaluationService)
        {
            _context = context;
            _studentAccessService = studentAccessService;
            _riskEvaluationService = riskEvaluationService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var profesor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);
            if (profesor == null) return NotFound();

            var grupos = await _context.Grupos
                .Where(g => g.ProfesorId == profesor.Id)
                .Include(g => g.Alumnos)
                .ToListAsync();

            var alumnosIds = grupos.SelectMany(g => g.Alumnos).Select(a => a.Matricula).ToList();
            var alumnos = await _context.Alumnos
                .Include(a => a.Grupo)
                .Where(a => alumnosIds.Contains(a.Matricula))
                .ToListAsync();
            var activePlanMatriculas = await _context.PlanesMejora
                .Where(p => alumnosIds.Contains(p.AlumnoMatricula) && p.Estado == "Activo")
                .Select(p => p.AlumnoMatricula)
                .ToListAsync();
            var intervencionesRecientes = await _context.IntervencionesTutoria
                .Where(i => i.TutorId == profesor.Id && i.Fecha >= DateTime.Now.AddDays(-30))
                .Select(i => i.AlumnoMatricula)
                .Distinct()
                .ToListAsync();

            var vm = new ProfesorDashboardViewModel
            {
                ProfesorNombre = $"{profesor.Nombre} {profesor.Apellidos}",
                TotalEstudiantes = alumnos.Count,
                EnRiesgoRojo = alumnos.Count(a => a.RiesgoAcademico == "Rojo"),
                EnRiesgoAmarillo = alumnos.Count(a => a.RiesgoAcademico == "Amarillo"),
                EnRiesgoVerde = alumnos.Count(a => a.RiesgoAcademico == "Verde" || string.IsNullOrEmpty(a.RiesgoAcademico)),
                IntervencionesPendientes = await _context.IntervencionesTutoria.CountAsync(i => i.TutorId == profesor.Id && i.RequiereSeguimiento),
                PlanesActivos = await _context.PlanesMejora.CountAsync(p => p.TutorId == profesor.Id && p.Estado == "Activo"),
                PromedioGeneral = alumnos.Any() ? alumnos.Average(a => (double)a.PromedioGlobal) : 0,
                Grupos = grupos.Select(g => new GrupoResumen
                {
                    Clave = g.Clave,
                    Carrera = g.Carrera,
                    Semestre = g.Semestre,
                    Turno = g.Turno,
                    TotalAlumnos = g.Alumnos.Count,
                    EnRiesgo = g.Alumnos.Count(a => a.RiesgoAcademico == "Rojo" || a.RiesgoAcademico == "Amarillo")
                }).ToList(),
                Estudiantes = alumnos.OrderBy(a => a.Apellidos).ToList(),
                ExistingPlanMatriculas = activePlanMatriculas
            };

            int predAlta = 0, predMedia = 0, predBaja = 0;
            foreach (var a in alumnos)
            {
                var prob = _riskEvaluationService.CalcularProbabilidadDesercion(a);
                if (prob >= 0.55m) predAlta++;
                else if (prob >= 0.35m) predMedia++;
                else predBaja++;

                if (prob >= 0.35m)
                {
                    vm.CopilotoCasos.Add(new CopilotoCaso
                    {
                        Matricula = a.Matricula,
                        Nombre = $"{a.Nombre} {a.Apellidos}",
                        Grupo = a.Grupo?.Clave ?? "",
                        Carrera = a.Carrera ?? "",
                        Promedio = a.PromedioGlobal,
                        Probabilidad = prob,
                        Riesgo = a.RiesgoAcademico ?? "Verde",
                        TienePlanActivo = activePlanMatriculas.Contains(a.Matricula),
                        TieneIntervencionReciente = intervencionesRecientes.Contains(a.Matricula),
                        SiguienteAccion = activePlanMatriculas.Contains(a.Matricula)
                            ? "Revisar avance del plan"
                            : intervencionesRecientes.Contains(a.Matricula)
                                ? "Programar seguimiento"
                                : "Generar análisis y abrir intervención"
                    });
                }
            }
            vm.PrediccionesAltas = predAlta;
            vm.PrediccionesMedias = predMedia;
            vm.PrediccionesBajas = predBaja;
            vm.CopilotoCasos = vm.CopilotoCasos
                .OrderByDescending(c => c.Probabilidad)
                .ThenBy(c => c.Promedio)
                .Take(4)
                .ToList();

            return View(vm);
        }

        public async Task<IActionResult> MisEstudiantes(string? riesgo, string? grupo, string? busqueda)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var profesor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);
            if (profesor == null) return NotFound();

            var grupos = await _context.Grupos
                .Where(g => g.ProfesorId == profesor.Id)
                .Include(g => g.Alumnos)
                .ToListAsync();

            // Enforce group-first hierarchy: redirect to Index if no group selected
            if (string.IsNullOrEmpty(grupo) || grupo == "Todos")
            {
                return RedirectToAction("Index");
            }

            var alumnosIds = grupos.SelectMany(g => g.Alumnos).Select(a => a.Matricula).ToList();
            var query = _context.Alumnos.Where(a => alumnosIds.Contains(a.Matricula)).AsQueryable();

            if (!string.IsNullOrEmpty(riesgo) && riesgo != "Todos")
                query = query.Where(a => a.RiesgoAcademico == riesgo);

            query = query.Where(a => a.Grupo != null && a.Grupo.Clave == grupo);

            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(a => a.Matricula.Contains(busqueda) || a.Nombre.Contains(busqueda) || a.Apellidos.Contains(busqueda));

            var alumnos = await query.OrderBy(a => a.Apellidos).ToListAsync();
            ViewBag.Grupos = grupos.Select(g => g.Clave).ToList();
            ViewBag.FiltroRiesgo = riesgo;
            ViewBag.FiltroGrupo = grupo;
            ViewBag.Busqueda = busqueda;
            return View(alumnos);
        }

        [Authorize(Roles = "Tutor,Autoridad")]
        public async Task<IActionResult> HistorialAcademico(string boleta)
        {
            var alumno = await _context.Alumnos
                .Include(a => a.Calificaciones)
                    .ThenInclude(c => c.Materia)
                .Include(a => a.Dictamenes)
                .Include(a => a.ReportesCosecovi)
                .FirstOrDefaultAsync(a => a.Matricula == boleta);

            if (alumno == null) return NotFound();

            if (User.IsInRole("Tutor"))
            {
                if (!await _studentAccessService.CanAccessAlumnoAsync(boleta)) return Forbid();
            }

            var historial = alumno.Calificaciones
                .Where(c => c.Materia != null)
                .GroupBy(c => c.CicloEscolar ?? "Sin Periodo")
                .OrderBy(g => g.Key)
                .Select(g => new HistorialPeriodo
                {
                    Periodo = g.Key,
                    Materias = g.Select(c => new MateriaHistorial
                    {
                        Clave = c.Materia!.Clave,
                        Nombre = c.Materia.Nombre,
                        Calificacion = c.Valor ?? 0,
                        Aprobada = c.Aprobada,
                        Tipo = c.Tipo,
                        VecesCursada = c.VecesCursada ?? 0
                    }).OrderBy(m => m.Clave).ToList()
                }).ToList();

            var reprobadas = alumno.Calificaciones
                .Where(c => !c.Aprobada && c.Materia != null)
                .Select(c => c.Materia!.Nombre)
                .Distinct()
                .ToList();

            ViewBag.Alumno = alumno;
            ViewBag.Reprobadas = reprobadas;
            ViewBag.Dictamenes = alumno.Dictamenes.OrderByDescending(d => d.FechaEmision).ToList();
            ViewBag.Cosecovi = alumno.ReportesCosecovi.OrderByDescending(r => r.FechaReporte).ToList();

            ViewBag.ProbabilidadDesercion = _riskEvaluationService.CalcularProbabilidadDesercion(alumno);
            ViewBag.NivelPredictivo = _riskEvaluationService.CalcularNivelPredictivo(ViewBag.ProbabilidadDesercion);
            ViewBag.FactoresRiesgo = _riskEvaluationService.ObtenerFactoresRiesgo(alumno);
            ViewBag.Sugerencias = _riskEvaluationService.GenerarSugerencias(alumno);

            return View(historial);
        }

        public async Task<IActionResult> MateriasReprobadas(string? grupo)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var profesor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);
            if (profesor == null) return NotFound();

            var grupos = await _context.Grupos
                .AsNoTracking()
                .Where(g => g.ProfesorId == profesor.Id)
                .OrderBy(g => g.Clave)
                .ToListAsync();

            var query = _context.Calificaciones
                .AsNoTracking()
                .Where(c => !c.Aprobada
                    && c.Alumno != null
                    && c.Alumno.Grupo != null
                    && c.Alumno.Grupo.ProfesorId == profesor.Id);

            if (!string.IsNullOrWhiteSpace(grupo) && grupo != "Todos")
            {
                query = query.Where(c => c.Alumno!.Grupo != null && c.Alumno.Grupo.Clave == grupo);
            }

            var califReprobadas = await query
                .OrderBy(c => c.Alumno!.Apellidos)
                .ThenBy(c => c.Alumno!.Nombre)
                .ThenBy(c => c.Materia!.Clave)
                .Select(c => new MateriaReprobadaViewModel
                {
                    AlumnoMatricula = c.AlumnoMatricula,
                    AlumnoNombre = c.Alumno != null ? c.Alumno.Nombre + " " + c.Alumno.Apellidos : "",
                    Grupo = c.Alumno != null && c.Alumno.Grupo != null ? c.Alumno.Grupo.Clave : "",
                    MateriaClave = c.Materia != null ? c.Materia.Clave : "",
                    MateriaNombre = c.Materia != null ? c.Materia.Nombre : "",
                    Calificacion = c.Valor ?? 0,
                    VecesCursada = c.VecesCursada ?? 0
                })
                .ToListAsync();

            ViewBag.Grupos = grupos.Select(g => g.Clave).ToList();
            ViewBag.GrupoSeleccionado = grupo;
            return View(califReprobadas);
        }
    }

    public class ProfesorDashboardViewModel
    {
        public string ProfesorNombre { get; set; } = "";
        public int TotalEstudiantes { get; set; }
        public int EnRiesgoRojo { get; set; }
        public int EnRiesgoAmarillo { get; set; }
        public int EnRiesgoVerde { get; set; }
        public int IntervencionesPendientes { get; set; }
        public int PlanesActivos { get; set; }
        public double PromedioGeneral { get; set; }
        public int PrediccionesAltas { get; set; }
        public int PrediccionesMedias { get; set; }
        public int PrediccionesBajas { get; set; }
        public List<GrupoResumen> Grupos { get; set; } = new();
        public List<string> ExistingPlanMatriculas { get; set; } = new();
        public List<Alumno> Estudiantes { get; set; } = new();
        public List<CopilotoCaso> CopilotoCasos { get; set; } = new();
    }

    public class CopilotoCaso
    {
        public string Matricula { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Grupo { get; set; } = "";
        public string Carrera { get; set; } = "";
        public decimal Promedio { get; set; }
        public decimal Probabilidad { get; set; }
        public string Riesgo { get; set; } = "";
        public string SiguienteAccion { get; set; } = "";
        public bool TienePlanActivo { get; set; }
        public bool TieneIntervencionReciente { get; set; }
    }

    public class GrupoResumen
    {
        public string Clave { get; set; } = "";
        public string? Carrera { get; set; }
        public int Semestre { get; set; }
        public string Turno { get; set; } = "";
        public int TotalAlumnos { get; set; }
        public int EnRiesgo { get; set; }
    }

    public class HistorialPeriodo
    {
        public string Periodo { get; set; } = "";
        public List<MateriaHistorial> Materias { get; set; } = new();
    }

    public class MateriaHistorial
    {
        public string Clave { get; set; } = "";
        public string Nombre { get; set; } = "";
        public decimal Calificacion { get; set; }
        public bool Aprobada { get; set; }
        public string? Tipo { get; set; }
        public int VecesCursada { get; set; }
    }

    public class MateriaReprobadaViewModel
    {
        public string AlumnoMatricula { get; set; } = "";
        public string AlumnoNombre { get; set; } = "";
        public string Grupo { get; set; } = "";
        public string MateriaClave { get; set; } = "";
        public string MateriaNombre { get; set; } = "";
        public decimal Calificacion { get; set; }
        public int VecesCursada { get; set; }
    }
}
