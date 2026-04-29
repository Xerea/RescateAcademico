using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new DashboardStats();

            if (User.IsInRole("Administrador") || User.IsInRole("Autoridad"))
            {
                stats.TotalAlumnos = await _context.Alumnos.CountAsync();
                stats.TotalProyectos = await _context.Proyectos.CountAsync(p => p.EstaActivo);
                stats.TotalConvocatorias = await _context.Convocatorias.CountAsync(c => c.EstaActiva);
                stats.TotalPostulaciones = await _context.Postulaciones.CountAsync();
                stats.PostulacionesPendientes = await _context.Postulaciones.CountAsync(p => p.Estado == "En Revisión");
                stats.AlumnosEnRiesgo = await _context.Alumnos.CountAsync(a => a.RiesgoAcademico == "Rojo" || a.RiesgoAcademico == "Amarillo");
                stats.TotalTutores = await _context.Tutores.CountAsync(t => t.EstaActivo);

                // Chart data
                stats.RiesgoVerde = await _context.Alumnos.CountAsync(a => a.RiesgoAcademico == "Verde" || string.IsNullOrEmpty(a.RiesgoAcademico));
                stats.RiesgoAmarillo = await _context.Alumnos.CountAsync(a => a.RiesgoAcademico == "Amarillo");
                stats.RiesgoRojo = await _context.Alumnos.CountAsync(a => a.RiesgoAcademico == "Rojo");
                stats.AlumnosPorCarrera = await _context.Alumnos
                    .Where(a => !string.IsNullOrEmpty(a.Carrera))
                    .GroupBy(a => a.Carrera!)
                    .Select(g => new { Carrera = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Carrera)
                    .ToListAsync()
                    .ContinueWith(t => t.Result.Select(x => (x.Carrera, x.Count)).ToList());
                stats.AlumnosPorSemestre = await _context.Alumnos
                    .GroupBy(a => a.SemestreActual)
                    .Select(g => new { Semestre = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Semestre)
                    .ToListAsync()
                    .ContinueWith(t => t.Result.Select(x => (x.Semestre, x.Count)).ToList());
            }
            else if (User.IsInRole("Tutor"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var tutor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);
                if (tutor != null)
                {
                    var assignedMatriculas = await _context.AsignacionesTutor
                        .Where(a => a.TutorId == tutor.Id && a.EstaActiva)
                        .Select(a => a.AlumnoMatricula)
                        .ToListAsync();

                    stats.TutorAssignedStudents = assignedMatriculas.Count;
                    stats.TutorStudentsAtRisk = await _context.Alumnos
                        .CountAsync(a => assignedMatriculas.Contains(a.Matricula) &&
                            (a.RiesgoAcademico == "Rojo" || a.RiesgoAcademico == "Amarillo"));
                    stats.TutorRecentInterventions = await _context.IntervencionesTutoria
                        .CountAsync(i => i.TutorId == tutor.Id && i.Fecha >= DateTime.Now.AddDays(-30));
                }
            }
            else if (User.IsInRole("Alumno"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.UserId == userId);
                if (alumno != null)
                {
                    stats.AlumnoPromedio = alumno.PromedioGlobal;
                    stats.AlumnoRiesgo = alumno.RiesgoAcademico ?? "Verde";
                    stats.AlumnoSemestre = alumno.SemestreActual;
                    stats.AlumnoPostulaciones = await _context.Postulaciones.CountAsync(p => p.AlumnoId == alumno.Matricula);
                }
            }

            return View(stats);
        }
    }
}
