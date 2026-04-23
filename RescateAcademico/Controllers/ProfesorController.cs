using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Tutor")]
    public class ProfesorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfesorController(ApplicationDbContext context)
        {
            _context = context;
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
                .Where(a => alumnosIds.Contains(a.Matricula))
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
                Estudiantes = alumnos.OrderBy(a => a.Apellidos).Take(20).ToList()
            };

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

            var alumnosIds = grupos.SelectMany(g => g.Alumnos).Select(a => a.Matricula).ToList();
            var query = _context.Alumnos.Where(a => alumnosIds.Contains(a.Matricula)).AsQueryable();

            if (!string.IsNullOrEmpty(riesgo) && riesgo != "Todos")
                query = query.Where(a => a.RiesgoAcademico == riesgo);

            if (!string.IsNullOrEmpty(grupo) && grupo != "Todos")
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

        public async Task<IActionResult> HistorialAcademico(string boleta)
        {
            var alumno = await _context.Alumnos
                .Include(a => a.Calificaciones)
                    .ThenInclude(c => c.Materia)
                .Include(a => a.Dictamenes)
                .Include(a => a.ReportesCosecovi)
                .FirstOrDefaultAsync(a => a.Matricula == boleta);

            if (alumno == null) return NotFound();

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

            return View(historial);
        }

        public async Task<IActionResult> MateriasReprobadas(string? grupo)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var profesor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);
            if (profesor == null) return NotFound();

            var grupos = await _context.Grupos
                .Where(g => g.ProfesorId == profesor.Id)
                .Include(g => g.Alumnos)
                .ToListAsync();

            var alumnosIds = grupos.SelectMany(g => g.Alumnos).Select(a => a.Matricula).ToList();
            var califReprobadas = await _context.Calificaciones
                .Where(c => !c.Aprobada && alumnosIds.Contains(c.AlumnoMatricula))
                .Include(c => c.Alumno)
                .Include(c => c.Materia)
                .OrderBy(c => c.Alumno!.Apellidos)
                .ToListAsync();

            ViewBag.Grupos = grupos.Select(g => g.Clave).ToList();
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
        public List<GrupoResumen> Grupos { get; set; } = new();
        public List<Alumno> Estudiantes { get; set; } = new();
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
}
