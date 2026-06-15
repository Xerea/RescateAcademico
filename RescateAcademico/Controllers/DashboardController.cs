using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

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

        public async Task<IActionResult> Index(string? grupo)
        {
            // Tutors have their own dedicated hub at Profesor/Index
            if (User.IsInRole("Tutor"))
            {
                return RedirectToAction("Index", "Profesor");
            }

            var stats = new DashboardStats();

            if (User.IsInRole("Administrador") || User.IsInRole("Autoridad"))
            {
                IQueryable<Alumno> alumnoQuery = _context.Alumnos.AsQueryable();
                IQueryable<Postulacion> postulacionQuery = _context.Postulaciones
                    .Include(p => p.Alumno)
                    .AsQueryable();
                if (!string.IsNullOrEmpty(grupo))
                {
                    alumnoQuery = alumnoQuery.Where(a => a.Grupo != null && a.Grupo.Clave == grupo);
                    postulacionQuery = postulacionQuery.Where(p => p.Alumno != null && p.Alumno.Grupo != null && p.Alumno.Grupo.Clave == grupo);
                }

                stats.TotalAlumnos = await alumnoQuery.CountAsync();
                stats.TotalProyectos = await _context.Proyectos.CountAsync(p => p.EstaActivo);
                stats.TotalConvocatorias = await _context.Convocatorias.CountAsync(c => c.EstaActiva);
                stats.TotalPostulaciones = await postulacionQuery.CountAsync();
                stats.PostulacionesPendientes = await postulacionQuery.CountAsync(p => p.Estado == "En Revisión");
                stats.AlumnosEnRiesgo = await alumnoQuery.CountAsync(a => a.RiesgoAcademico == "Rojo" || a.RiesgoAcademico == "Amarillo");
                var tutoresActivosQuery = _context.Tutores.Where(t => t.EstaActivo);
                if (!string.IsNullOrEmpty(grupo))
                {
                    tutoresActivosQuery = tutoresActivosQuery.Where(t => _context.Grupos.Any(g => g.Clave == grupo && g.ProfesorId == t.Id));
                }
                stats.TotalTutores = await tutoresActivosQuery.CountAsync();

                // Chart data
                stats.RiesgoVerde = await alumnoQuery.CountAsync(a => a.RiesgoAcademico == "Verde" || string.IsNullOrEmpty(a.RiesgoAcademico));
                stats.RiesgoAmarillo = await alumnoQuery.CountAsync(a => a.RiesgoAcademico == "Amarillo");
                stats.RiesgoRojo = await alumnoQuery.CountAsync(a => a.RiesgoAcademico == "Rojo");
                stats.AlumnosPorCarrera = await alumnoQuery
                    .Where(a => !string.IsNullOrEmpty(a.Carrera))
                    .GroupBy(a => a.Carrera!)
                    .Select(g => new { Carrera = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Carrera)
                    .ToListAsync()
                    .ContinueWith(t => t.Result.Select(x => (x.Carrera, x.Count)).ToList());
                stats.AlumnosPorSemestre = await alumnoQuery
                    .GroupBy(a => a.SemestreActual)
                    .Select(g => new { Semestre = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Semestre)
                    .ToListAsync()
                    .ContinueWith(t => t.Result.Select(x => (x.Semestre, x.Count)).ToList());

                // Autoridad-specific stats
                var promedios = await alumnoQuery
                    .Select(a => a.PromedioGlobal)
                    .ToListAsync();
                stats.PromedioGeneral = promedios.Count > 0
                    ? promedios.Average()
                    : 0m;
                stats.AlumnosPromedioCritico = await alumnoQuery.CountAsync(a => a.PromedioGlobal < 6m);
                stats.AlumnosPromedioObservacion = await alumnoQuery.CountAsync(a => a.PromedioGlobal >= 6m && a.PromedioGlobal < 7.5m);
                stats.AlumnosPromedioSolido = await alumnoQuery.CountAsync(a => a.PromedioGlobal >= 7.5m);
                stats.AlumnosConMateriasReprobadas = await alumnoQuery.CountAsync(a => (a.MateriasReprobadas ?? 0) > 0);
                stats.AlumnosConAusencias = await alumnoQuery.CountAsync(a => (a.Ausencias ?? 0) > 0);
                stats.ConvocatoriasProximasACerrar = await _context.Convocatorias
                    .CountAsync(c => c.EstaActiva && c.FechaCierre >= DateTime.Now && c.FechaCierre <= DateTime.Now.AddDays(30));
                var intervencionesQuery = _context.IntervencionesTutoria
                    .Include(i => i.Alumno)
                    .AsQueryable();
                if (!string.IsNullOrEmpty(grupo))
                {
                    intervencionesQuery = intervencionesQuery
                        .Where(i => i.Alumno != null && i.Alumno.Grupo != null && i.Alumno.Grupo.Clave == grupo);
                }
                stats.IntervencionesRecientes = await intervencionesQuery
                    .CountAsync(i => i.Fecha >= DateTime.Now.AddDays(-30));
                stats.TotalGrupos = string.IsNullOrEmpty(grupo) ? await _context.Grupos.CountAsync() : 1;

                ViewBag.Grupos = await _context.Grupos
                    .Select(g => g.Clave)
                    .OrderBy(c => c)
                    .ToListAsync();
                ViewBag.GrupoSeleccionado = grupo;
            }
            else if (User.IsInRole("Tutor"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var tutor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);
                if (tutor != null)
                {
                    var grupos = await _context.Grupos
                        .Where(g => g.ProfesorId == tutor.Id)
                        .Include(g => g.Alumnos)
                        .ToListAsync();

                    var assignedMatriculas = grupos.SelectMany(g => g.Alumnos).Select(a => a.Matricula).ToList();

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
                var alumno = await _context.Alumnos
                    .Include(a => a.Grupo)
                    .FirstOrDefaultAsync(a => a.UserId == userId);
                if (alumno != null)
                {
                    var now = DateTime.Now;

                    stats.AlumnoNombre = alumno.Nombre;
                    stats.AlumnoMatricula = alumno.Matricula;
                    stats.AlumnoCarrera = alumno.Carrera;
                    stats.AlumnoGrupo = alumno.Grupo?.Clave;
                    stats.AlumnoPromedio = alumno.PromedioGlobal;
                    stats.AlumnoRiesgo = alumno.RiesgoAcademico ?? "Verde";
                    stats.AlumnoSemestre = alumno.SemestreActual;
                    stats.AlumnoMateriasReprobadas = alumno.MateriasReprobadas ?? 0;
                    stats.AlumnoAusencias = alumno.Ausencias ?? 0;

                    var postulaciones = _context.Postulaciones.Where(p => p.AlumnoId == alumno.Matricula);
                    stats.AlumnoPostulaciones = await postulaciones.CountAsync();
                    stats.AlumnoPostulacionesPendientes = await postulaciones.CountAsync(p => p.Estado == "En Revisión");
                    stats.AlumnoPostulacionesAceptadas = await postulaciones.CountAsync(p => p.Estado == "Aceptado");

                    stats.AlumnoConvocatoriasDisponibles = await _context.Convocatorias.CountAsync(c =>
                        c.EstaActiva &&
                        c.ValidadaPorAcademia &&
                        c.FechaCierre > now &&
                        c.PostulacionesActuales < c.CupoMaximo &&
                        (!c.PromedioMinimo.HasValue || alumno.PromedioGlobal >= c.PromedioMinimo.Value) &&
                        (!c.SemestreMinimo.HasValue || alumno.SemestreActual >= c.SemestreMinimo.Value) &&
                        (string.IsNullOrEmpty(c.CarreraRequerida) || c.CarreraRequerida == alumno.Carrera));
                }
            }

            return View(stats);
        }
    }
}
