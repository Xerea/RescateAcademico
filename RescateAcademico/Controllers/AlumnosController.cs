using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;
using RescateAcademico.Services;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class AlumnosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StudentAccessService _studentAccessService;

        public AlumnosController(ApplicationDbContext context, StudentAccessService studentAccessService)
        {
            _context = context;
            _studentAccessService = studentAccessService;
        }

        [Authorize(Roles = "Administrador,Tutor,Autoridad")]
        public async Task<IActionResult> Index(string? busqueda, string? filtroRiesgo, string? carrera, int? semestre, string? grupo)
        {
            var query = _studentAccessService.ApplyVisibleStudents(_context.Alumnos
                .Include(a => a.Grupo)
                .AsQueryable());

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

            if (!string.IsNullOrEmpty(grupo))
            {
                query = query.Where(a => a.Grupo != null && a.Grupo.Clave == grupo);
            }

            ViewBag.Busqueda = busqueda;
            ViewBag.FiltroRiesgo = filtroRiesgo;
            ViewBag.FiltroCarrera = carrera;
            ViewBag.FiltroSemestre = semestre;
            ViewBag.FiltroGrupo = grupo;
            var visibleForFilters = _studentAccessService.ApplyVisibleStudents(_context.Alumnos.AsQueryable());
            ViewBag.Carreras = await visibleForFilters.Select(a => a.Carrera).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToListAsync();
            ViewBag.Semestres = await visibleForFilters.Select(a => a.SemestreActual).Distinct().OrderBy(s => s).ToListAsync();
            ViewBag.Grupos = await visibleForFilters
                .Where(a => a.Grupo != null)
                .Select(a => a.Grupo!.Clave)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

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

            if (!await _studentAccessService.CanAccessAlumnoAsync(id))
                return Forbid();

            return View(alumno);
        }

        public IActionResult MiPerfil()
        {
            return RedirectToAction("MiPerfil", "PerfilAcademico");
        }

        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> MisTutorados(string? grupo)
        {
            var tutor = await _studentAccessService.GetCurrentTutorAsync();

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
            if (!await _studentAccessService.CanAccessAlumnoAsync(matricula))
                return Forbid();
            return RedirectToAction("Detalles", new { id = matricula });
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

            if (!await _studentAccessService.CanAccessAlumnoAsync(matricula))
                return Forbid();

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

        [Authorize(Roles = "Administrador,Tutor,Autoridad")]
        public async Task<IActionResult> QuickView(string matricula)
        {
            var alumno = await _context.Alumnos
                .Include(a => a.Grupo)
                .Include(a => a.ReportesCosecovi)
                .FirstOrDefaultAsync(a => a.Matricula == matricula);

            if (alumno == null) return NotFound();

            if (!await _studentAccessService.CanAccessAlumnoAsync(matricula))
                return Forbid();

            var ultimoCosecovi = alumno.ReportesCosecovi
                .OrderByDescending(r => r.FechaIncidente)
                .FirstOrDefault();

            return Json(new
            {
                alumno.Matricula,
                alumno.Nombre,
                alumno.Apellidos,
                alumno.Carrera,
                alumno.SemestreActual,
                Grupo = alumno.Grupo?.Clave,
                alumno.PromedioGlobal,
                alumno.RiesgoAcademico,
                alumno.CargaAcademicaActual,
                alumno.MateriasReprobadas,
                alumno.Ausencias,
                alumno.EtsPresentados,
                alumno.Recursamientos,
                CosecoviReportes = alumno.ReportesCosecovi.Count,
                CosecoviAbiertos = alumno.ReportesCosecovi.Count(r => r.Estado != "Cerrado"),
                CosecoviUltimoTipo = ultimoCosecovi?.TipoIncidente,
                CosecoviUltimaGravedad = ultimoCosecovi?.Gravedad,
                CosecoviUltimoEstado = ultimoCosecovi?.Estado,
                CosecoviUltimaFecha = ultimoCosecovi?.FechaIncidente.ToString("dd/MM/yyyy"),
                FechaUltimaActualizacion = alumno.FechaUltimaActualizacion?.ToString("dd/MM/yyyy")
            });
        }

    }
}
