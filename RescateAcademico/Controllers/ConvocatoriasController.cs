using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Filters;
using RescateAcademico.Models;
using RescateAcademico.Services;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class ConvocatoriasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StudentAccessService _studentAccessService;
        private readonly ConvocatoriaEligibilityService _eligibilityService;

        public ConvocatoriasController(
            ApplicationDbContext context,
            StudentAccessService studentAccessService,
            ConvocatoriaEligibilityService eligibilityService)
        {
            _context = context;
            _studentAccessService = studentAccessService;
            _eligibilityService = eligibilityService;
        }

        public async Task<IActionResult> Index(string? tipo, string? area, string? busqueda)
        {
            var query = _context.Convocatorias
                .Where(c => c.EstaActiva && c.ValidadaPorAcademia)
                .AsQueryable();

            if (User.IsInRole("Alumno"))
            {
                query = query.Where(c => c.PostulacionesActuales < c.CupoMaximo && c.FechaCierre >= DateTime.Now);
            }

            if (!string.IsNullOrEmpty(tipo) && tipo != "Todos")
                query = query.Where(c => c.Tipo == tipo);

            if (!string.IsNullOrEmpty(area))
                query = query.Where(c => c.Area != null && c.Area.Contains(area));

            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(c => c.Titulo.Contains(busqueda) || (c.Descripcion != null && c.Descripcion.Contains(busqueda)));

            var convocatorias = await query.OrderByDescending(c => c.FechaPublicacion).ToListAsync();

            if (User.IsInRole("Alumno"))
            {
                var alumno = await _studentAccessService.GetCurrentAlumnoAsync();
                if (alumno != null)
                {
                    var proyectoIdsPostulados = await _context.Postulaciones
                        .Where(p => p.AlumnoId == alumno.Matricula)
                        .Select(p => p.ProyectoId)
                        .ToListAsync();

                    var elegibles = new HashSet<int>();
                    foreach (var convocatoria in convocatorias)
                    {
                        var resultado = await _eligibilityService.EvaluarAsync(alumno, convocatoria);
                        if (resultado.IsEligible)
                        {
                            elegibles.Add(convocatoria.Id);
                        }
                    }

                    ViewBag.AlumnoPromedio = alumno.PromedioGlobal;
                    ViewBag.AlumnoSemestre = alumno.SemestreActual;
                    ViewBag.AlumnoCarrera = alumno.Carrera;
                    ViewBag.ConvocatoriasElegibles = elegibles;
                    ViewBag.ProyectosPostulados = proyectoIdsPostulados.ToHashSet();
                }
            }

            return View(convocatorias);
        }

        public async Task<IActionResult> Details(int id)
        {
            var convocatoria = await _context.Convocatorias
                .Include(c => c.Proyecto)
                .Include(c => c.Postulaciones)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (convocatoria == null) return NotFound();
            return View(convocatoria);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create()
        {
            await CargarProyectosAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([Bind("Titulo,Descripcion,Tipo,ProyectoId,CupoMaximo,FechaCierre,Requisitos,PromedioMinimo,SemestreMinimo,CarreraRequerida,Modalidad,Ubicacion,Horario,RequisitosTecnicos,Area")] Convocatoria convocatoria)
        {
            await ValidarConvocatoriaAsync(convocatoria);
            if (ModelState.IsValid)
            {
                convocatoria.FechaPublicacion = DateTime.Now;
                _context.Convocatorias.Add(convocatoria);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Convocatoria creada exitosamente";
                return RedirectToAction("Todas");
            }
            await CargarProyectosAsync();
            return View(convocatoria);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            var convocatoria = await _context.Convocatorias.FindAsync(id);
            if (convocatoria == null) return NotFound();
            await CargarProyectosAsync();
            return View(convocatoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit([Bind("Id,Titulo,Descripcion,Tipo,ProyectoId,CupoMaximo,FechaCierre,Requisitos,PromedioMinimo,SemestreMinimo,CarreraRequerida,Modalidad,Ubicacion,Horario,RequisitosTecnicos,Area,EstaActiva,ValidadaPorAcademia")] Convocatoria convocatoria)
        {
            await ValidarConvocatoriaAsync(convocatoria);
            if (ModelState.IsValid)
            {
                _context.Update(convocatoria);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Convocatoria actualizada";
                return RedirectToAction("Todas");
            }
            await CargarProyectosAsync();
            return View(convocatoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        [AuditLog(Accion = "Cerrar", Tabla = "Convocatorias")]
        public async Task<IActionResult> Delete(int id)
        {
            var convocatoria = await _context.Convocatorias.FindAsync(id);
            if (convocatoria != null)
            {
                convocatoria.EstaActiva = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Convocatoria cerrada";
            }
            return RedirectToAction("Todas");
        }

        [Authorize(Roles = "Administrador,Autoridad")]
        public async Task<IActionResult> Todas()
        {
            var convocatorias = await _context.Convocatorias
                .Include(c => c.Postulaciones)
                .OrderByDescending(c => c.FechaPublicacion)
                .ToListAsync();
            return View("Todas", convocatorias);
        }

        private async Task ValidarConvocatoriaAsync(Convocatoria convocatoria)
        {
            if (!convocatoria.ProyectoId.HasValue)
                ModelState.AddModelError(nameof(convocatoria.ProyectoId), "Selecciona el proyecto asociado a la convocatoria.");
            else if (!await _context.Proyectos.AnyAsync(p => p.Id == convocatoria.ProyectoId.Value && p.EstaActivo))
                ModelState.AddModelError(nameof(convocatoria.ProyectoId), "El proyecto seleccionado no está disponible.");
            else if (await _context.Convocatorias.AnyAsync(c => c.Id != convocatoria.Id && c.ProyectoId == convocatoria.ProyectoId && c.EstaActiva))
                ModelState.AddModelError(nameof(convocatoria.ProyectoId), "Este proyecto ya tiene una convocatoria activa.");
            if (convocatoria.CupoMaximo < 1)
                ModelState.AddModelError(nameof(convocatoria.CupoMaximo), "El cupo debe ser al menos uno.");
            if (convocatoria.FechaCierre.Date < DateTime.Today)
                ModelState.AddModelError(nameof(convocatoria.FechaCierre), "La fecha de cierre no puede ser anterior a hoy.");
            if (convocatoria.PromedioMinimo is < 0 or > 10)
                ModelState.AddModelError(nameof(convocatoria.PromedioMinimo), "El promedio mínimo debe estar entre 0 y 10.");
            if (convocatoria.SemestreMinimo is < 1 or > 6)
                ModelState.AddModelError(nameof(convocatoria.SemestreMinimo), "El semestre mínimo debe estar entre 1 y 6.");
        }

        private async Task CargarProyectosAsync()
        {
            ViewBag.Proyectos = await _context.Proyectos
                .Where(p => p.EstaActivo)
                .OrderBy(p => p.Titulo)
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Titulo })
                .ToListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        [AuditLog(Accion = "Validar", Tabla = "Convocatorias")]
        public async Task<IActionResult> Validar(int id)
        {
            var convocatoria = await _context.Convocatorias.FindAsync(id);
            if (convocatoria != null)
            {
                convocatoria.ValidadaPorAcademia = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Convocatoria validada por academia";
            }
            return RedirectToAction("Todas");
        }
    }
}
