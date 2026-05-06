using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Services;
using System.Text;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Administrador,Tutor,Autoridad")]
    public class ExportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StudentAccessService _studentAccessService;

        public ExportController(ApplicationDbContext context, StudentAccessService studentAccessService)
        {
            _context = context;
            _studentAccessService = studentAccessService;
        }

        [HttpGet]
        public async Task<IActionResult> AlumnosCSV()
        {
            var alumnos = await _studentAccessService.ApplyVisibleStudents(_context.Alumnos)
                .OrderBy(a => a.Apellidos)
                .ThenBy(a => a.Nombre)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Matricula,Nombre,Apellidos,Carrera,Semestre,PromedioGlobal,RiesgoAcademico,MateriasReprobadas,Ausencias,ParcialesBajos,Estatus");

            foreach (var a in alumnos)
            {
                csv.AppendLine($"{Escape(a.Matricula)},{Escape(a.Nombre)},{Escape(a.Apellidos)},{Escape(a.Carrera)},{a.SemestreActual},{a.PromedioGlobal:F2},{Escape(a.RiesgoAcademico)},{a.MateriasReprobadas ?? 0},{a.Ausencias ?? 0},{a.ParcialesBajos ?? 0},{Escape(a.Estatus)}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"alumnos_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> PostulacionesCSV()
        {
            var matriculasVisibles = await _studentAccessService.GetVisibleMatriculasAsync();
            var postulaciones = await _context.Postulaciones
                .Include(p => p.Alumno)
                .Include(p => p.Proyecto)
                .Where(p => matriculasVisibles.Contains(p.AlumnoId))
                .OrderByDescending(p => p.FechaSolicitud)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Alumno,Matricula,Proyecto,FechaSolicitud,Estado,DocumentoAdjunto");

            foreach (var p in postulaciones)
            {
                var nombreAlumno = $"{p.Alumno?.Nombre} {p.Alumno?.Apellidos}".Trim();
                var tieneDoc = !string.IsNullOrEmpty(p.DocumentoNombre) ? "Si" : "No";
                csv.AppendLine($"{p.Id},{Escape(nombreAlumno)},{Escape(p.AlumnoId)},{Escape(p.Proyecto?.Titulo)},{p.FechaSolicitud:yyyy-MM-dd HH:mm},{Escape(p.Estado)},{tieneDoc}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"postulaciones_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> AlumnosEnRiesgoCSV()
        {
            var alumnos = await _studentAccessService.ApplyVisibleStudents(_context.Alumnos)
                .Where(a => a.RiesgoAcademico == "Rojo" || a.RiesgoAcademico == "Amarillo")
                .OrderByDescending(a => (double)a.PromedioGlobal)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Matricula,Nombre,Apellidos,Carrera,Semestre,PromedioGlobal,RiesgoAcademico,MateriasReprobadas,Ausencias,ParcialesBajos,Correo");

            foreach (var a in alumnos)
            {
                csv.AppendLine($"{Escape(a.Matricula)},{Escape(a.Nombre)},{Escape(a.Apellidos)},{Escape(a.Carrera)},{a.SemestreActual},{a.PromedioGlobal:F2},{Escape(a.RiesgoAcademico)},{a.MateriasReprobadas ?? 0},{a.Ausencias ?? 0},{a.ParcialesBajos ?? 0},{Escape(a.Correo)}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"alumnos_riesgo_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ReporteImprimible()
        {
            var alumnos = await _studentAccessService.ApplyVisibleStudents(_context.Alumnos)
                .Where(a => a.RiesgoAcademico == "Rojo" || a.RiesgoAcademico == "Amarillo")
                .OrderBy(a => a.RiesgoAcademico)
                .ThenByDescending(a => (double)a.PromedioGlobal)
                .ToListAsync();

            var totalAlumnos = await _studentAccessService.ApplyVisibleStudents(_context.Alumnos).CountAsync();
            var enRojo = alumnos.Count(a => a.RiesgoAcademico == "Rojo");
            var enAmarillo = alumnos.Count(a => a.RiesgoAcademico == "Amarillo");

            ViewBag.TotalAlumnos = totalAlumnos;
            ViewBag.EnRojo = enRojo;
            ViewBag.EnAmarillo = enAmarillo;
            ViewBag.FechaReporte = DateTime.Now;

            return View(alumnos);
        }

        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if ("=+-@".Contains(value[0]))
            {
                value = "'" + value;
            }
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}
