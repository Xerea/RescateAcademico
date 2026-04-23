using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Administrador,Autoridad")]
    public class EstadisticasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EstadisticasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? nivel)
        {
            nivel ??= "Global";

            var stats = new EstadisticasViewModel
            {
                Nivel = nivel,
                FechaActualizacion = DateTime.Now
            };

            IQueryable<Alumno> queryAlumnos = _context.Alumnos;
            if (nivel == "Carrera" && !string.IsNullOrEmpty(Request.Query["carrera"]))
            {
                var carrera = Request.Query["carrera"].ToString();
                queryAlumnos = queryAlumnos.Where(a => a.Carrera == carrera);
                stats.CarreraSeleccionada = carrera;
            }

            var alumnos = await queryAlumnos.ToListAsync();
            stats.TotalAlumnos = alumnos.Count;
            stats.AlumnosActivos = alumnos.Count(a => a.Estatus == "Activo");
            stats.PromedioGeneral = alumnos.Any() ? alumnos.Average(a => a.PromedioGlobal) : 0;
            stats.AlumnosEnRiesgoVerde = alumnos.Count(a => a.RiesgoAcademico == "Verde" || string.IsNullOrEmpty(a.RiesgoAcademico));
            stats.AlumnosEnRiesgoAmarillo = alumnos.Count(a => a.RiesgoAcademico == "Amarillo");
            stats.AlumnosEnRiesgoRojo = alumnos.Count(a => a.RiesgoAcademico == "Rojo");

            var postulaciones = await _context.Postulaciones.ToListAsync();
            stats.TotalPostulaciones = postulaciones.Count;
            stats.PostulacionesAceptadas = postulaciones.Count(p => p.Estado == "Aceptado");
            stats.PostulacionesRechazadas = postulaciones.Count(p => p.Estado == "Rechazado");
            stats.PostulacionesPendientes = postulaciones.Count(p => p.Estado == "En Revisión");

            var carreras = await _context.Carreras.Where(c => c.EstaActiva).ToListAsync();
            stats.Carreras = carreras.Select(c => c.Nombre).ToList();

            var ciclos = await _context.CiclosEscolares.Where(c => c.EstaActivo).ToListAsync();
            stats.CiclosActivos = ciclos;

            return View(stats);
        }

        public async Task<IActionResult> PorCarrera()
        {
            var statsPorCarrera = await _context.Alumnos
                .Where(a => !string.IsNullOrEmpty(a.Carrera))
                .GroupBy(a => a.Carrera)
                .Select(g => new EstadisticaCarrera
                {
                    Carrera = g.Key,
                    TotalAlumnos = g.Count(),
                    PromedioGeneral = (decimal)g.Average(a => (double)a.PromedioGlobal),
                    AlumnosEnRiesgo = g.Count(a => a.RiesgoAcademico == "Rojo" || a.RiesgoAcademico == "Amarillo")
                })
                .OrderByDescending(s => s.TotalAlumnos)
                .ToListAsync();

            return View(statsPorCarrera);
        }

        public async Task<IActionResult> Reporte()
        {
            var stats = await _context.Alumnos
                .Include(a => a.Postulaciones)
                .Include(a => a.Calificaciones)
                .ToListAsync();

            var reporte = new ReporteViewModel
            {
                TotalAlumnos = stats.Count,
                PromedioInstitucional = stats.Any() ? stats.Average(a => a.PromedioGlobal) : 0,
                TotalProyectos = await _context.Proyectos.CountAsync(),
                TotalConvocatorias = await _context.Convocatorias.CountAsync(c => c.EstaActiva),
                TotalPostulaciones = await _context.Postulaciones.CountAsync(),
                PostulacionesAceptadas = await _context.Postulaciones.CountAsync(p => p.Estado == "Aceptado"),
                FechaReporte = DateTime.Now,
                AlumnosPorRiesgo = new Dictionary<string, int>
                {
                    { "Verde", stats.Count(a => string.IsNullOrEmpty(a.RiesgoAcademico) || a.RiesgoAcademico == "Verde") },
                    { "Amarillo", stats.Count(a => a.RiesgoAcademico == "Amarillo") },
                    { "Rojo", stats.Count(a => a.RiesgoAcademico == "Rojo") }
                }
            };

            return View(reporte);
        }
    }

    public class EstadisticasViewModel
    {
        public string Nivel { get; set; } = "Global";
        public string? CarreraSeleccionada { get; set; }
        public int TotalAlumnos { get; set; }
        public int AlumnosActivos { get; set; }
        public decimal PromedioGeneral { get; set; }
        public int AlumnosEnRiesgoVerde { get; set; }
        public int AlumnosEnRiesgoAmarillo { get; set; }
        public int AlumnosEnRiesgoRojo { get; set; }
        public int TotalPostulaciones { get; set; }
        public int PostulacionesAceptadas { get; set; }
        public int PostulacionesRechazadas { get; set; }
        public int PostulacionesPendientes { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public List<string> Carreras { get; set; } = new();
        public List<CicloEscolar> CiclosActivos { get; set; } = new();
    }

    public class EstadisticaCarrera
    {
        public string? Carrera { get; set; }
        public int TotalAlumnos { get; set; }
        public decimal PromedioGeneral { get; set; }
        public int AlumnosEnRiesgo { get; set; }
    }

    public class ReporteViewModel
    {
        public int TotalAlumnos { get; set; }
        public decimal PromedioInstitucional { get; set; }
        public int TotalProyectos { get; set; }
        public int TotalConvocatorias { get; set; }
        public int TotalPostulaciones { get; set; }
        public int PostulacionesAceptadas { get; set; }
        public DateTime FechaReporte { get; set; }
        public Dictionary<string, int> AlumnosPorRiesgo { get; set; } = new();
    }
}
