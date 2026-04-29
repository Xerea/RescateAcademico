using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class AlumnosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AlumnosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Administrador,Tutor,Autoridad")]
        public async Task<IActionResult> Index(string? busqueda, string? filtroRiesgo, string? carrera, int? semestre)
        {
            var query = _context.Alumnos.AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(a => 
                    a.Matricula.Contains(busqueda) ||
                    a.Nombre.Contains(busqueda) ||
                    a.Apellidos.Contains(busqueda));
            }

            if (!string.IsNullOrEmpty(filtroRiesgo) && filtroRiesgo != "Todos")
            {
                query = query.Where(a => a.RiesgoAcademico == filtroRiesgo);
            }

            if (!string.IsNullOrEmpty(carrera))
            {
                query = query.Where(a => a.Carrera == carrera);
            }

            if (semestre.HasValue)
            {
                query = query.Where(a => a.SemestreActual == semestre.Value);
            }

            ViewBag.Busqueda = busqueda;
            ViewBag.FiltroRiesgo = filtroRiesgo;
            ViewBag.FiltroCarrera = carrera;
            ViewBag.FiltroSemestre = semestre;
            ViewBag.Carreras = await _context.Alumnos.Select(a => a.Carrera).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToListAsync();
            ViewBag.Semestres = await _context.Alumnos.Select(a => a.SemestreActual).Distinct().OrderBy(s => s).ToListAsync();

            var alumnos = await query.OrderBy(a => a.Apellidos).ToListAsync();
            return View(alumnos);
        }

        [Authorize(Roles = "Administrador,Tutor,Autoridad")]
        public async Task<IActionResult> Detalles(string id)
        {
            var alumno = await _context.Alumnos
                .Include(a => a.Calificaciones)
                    .ThenInclude(c => c.Materia)
                .Include(a => a.TutoresAsignados)
                    .ThenInclude(at => at.Tutor)
                .Include(a => a.Postulaciones)
                    .ThenInclude(p => p.Proyecto)
                .FirstOrDefaultAsync(a => a.Matricula == id);

            if (alumno == null) return NotFound();

            return View(alumno);
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
                TempData["Error"] = "No tienes un perfil de alumno asociado";
                return RedirectToAction("Index", "Home");
            }

            return View(alumno);
        }

        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> MisTutorados(string? grupo)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var tutor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null) return NotFound();

            var grupos = await _context.Grupos
                .Where(g => g.ProfesorId == tutor.Id)
                .Include(g => g.Alumnos)
                .ToListAsync();

            var alumnosIds = grupos.SelectMany(g => g.Alumnos).Select(a => a.Matricula).ToList();
            var query = _context.Alumnos.Where(a => alumnosIds.Contains(a.Matricula)).AsQueryable();

            if (!string.IsNullOrEmpty(grupo) && grupo != "Todos")
                query = query.Where(a => a.Grupo != null && a.Grupo.Clave == grupo);

            var alumnos = await query.OrderBy(a => a.Apellidos).ToListAsync();
            ViewBag.Grupos = grupos.Select(g => g.Clave).ToList();
            ViewBag.GrupoSeleccionado = grupo;
            return View(alumnos);
        }

        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> DetallesTutorado(string matricula)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var tutor = await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == userId);

            var asignacion = await _context.AsignacionesTutor
                .Include(at => at.Alumno)
                    .ThenInclude(a => a!.Calificaciones)
                        .ThenInclude(c => c.Materia)
                .Include(at => at.Alumno)
                    .ThenInclude(a => a!.Postulaciones)
                        .ThenInclude(p => p!.Proyecto)
                .Include(at => at.Tutor)
                .FirstOrDefaultAsync(at => at.AlumnoMatricula == matricula && at.TutorId == tutor!.Id);

            if (asignacion?.Alumno == null) return NotFound();

            return View(asignacion.Alumno);
        }

        [Authorize(Roles = "Administrador,Tutor,Autoridad")]
        public async Task<IActionResult> Timeline(string matricula)
        {
            var alumno = await _context.Alumnos
                .Include(a => a.Calificaciones)
                    .ThenInclude(c => c.Materia)
                .Include(a => a.Dictamenes)
                .Include(a => a.ReportesCosecovi)
                .FirstOrDefaultAsync(a => a.Matricula == matricula);

            if (alumno == null) return NotFound();

            var intervenciones = await _context.IntervencionesTutoria
                .Include(i => i.Tutor)
                .Where(i => i.AlumnoMatricula == matricula)
                .OrderByDescending(i => i.Fecha)
                .ToListAsync();

            var planes = await _context.PlanesMejora
                .Include(p => p.Tutor)
                .Where(p => p.AlumnoMatricula == matricula)
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            ViewBag.Alumno = alumno;
            ViewBag.Intervenciones = intervenciones;
            ViewBag.Planes = planes;

            return View(alumno);
        }
    }
}
