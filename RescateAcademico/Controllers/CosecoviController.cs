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
    [Authorize(Roles = "Administrador,Autoridad,Tutor")]
    public class CosecoviController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly StudentAccessService _studentAccessService;

        public CosecoviController(
            ApplicationDbContext context,
            NotificationService notificationService,
            StudentAccessService studentAccessService)
        {
            _context = context;
            _notificationService = notificationService;
            _studentAccessService = studentAccessService;
        }

        public static readonly string[] TiposIncidente =
        {
            "Conducta disruptiva", "Agresion fisica", "Agresion verbal", "Rina",
            "Consumo de sustancias", "Portacion de objeto prohibido", "Acoso o violencia",
            "Dano a instalaciones", "Robo o sustraccion", "Amenaza", "Otro"
        };

        public static readonly string[] Gravedades =
        {
            "Baja", "Media", "Alta", "Critica"
        };

        public static readonly string[] Turnados =
        {
            "Sin turno", "Direccion", "Seguridad", "Red de Genero", "Oficina del Abogado General",
            "Servicios Educativos", "Comite COSECOVI"
        };

        public static readonly string[] Estados =
        {
            "Registrado", "En seguimiento", "Escalado", "Cerrado"
        };

        public async Task<IActionResult> Index(string? periodo, string? estado, string? tipo, string? gravedad)
        {
            if (User.IsInRole("Tutor"))
            {
                return RedirectToAction(nameof(Alumno));
            }

            var query = _context.ReportesCosecovi
                .Include(r => r.Alumno)
                .AsQueryable();

            if (!string.IsNullOrEmpty(periodo))
                query = query.Where(r => r.Periodo == periodo);
            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                query = query.Where(r => r.Estado == estado);
            if (!string.IsNullOrEmpty(tipo) && tipo != "Todos")
                query = query.Where(r => r.TipoIncidente == tipo);
            if (!string.IsNullOrEmpty(gravedad) && gravedad != "Todas")
                query = query.Where(r => r.Gravedad == gravedad);

            var reportes = await query
                .OrderByDescending(r => r.FechaIncidente)
                .ToListAsync();

            ViewBag.Periodos = await _context.ReportesCosecovi
                .Select(r => r.Periodo)
                .Distinct()
                .OrderByDescending(p => p)
                .ToListAsync();
            ViewBag.FiltroPeriodo = periodo;
            ViewBag.FiltroEstado = estado;
            ViewBag.FiltroTipo = tipo;
            ViewBag.FiltroGravedad = gravedad;

            return View(reportes);
        }

        [Authorize(Roles = "Administrador,Autoridad")]
        public async Task<IActionResult> Dashboard()
        {
            var reportes = await _context.ReportesCosecovi
                .Include(r => r.Alumno)
                .ToListAsync();

            var vm = new CosecoviDashboardViewModel
            {
                Total = reportes.Count,
                Registrados = reportes.Count(r => r.Estado == "Registrado"),
                EnSeguimiento = reportes.Count(r => r.Estado == "En seguimiento"),
                Escalados = reportes.Count(r => r.Estado == "Escalado"),
                Cerrados = reportes.Count(r => r.Estado == "Cerrado"),
                AltaPrioridad = reportes.Count(r => r.Gravedad is "Alta" or "Critica" && r.Estado != "Cerrado"),
                PorTipoIncidente = reportes
                    .Where(r => !string.IsNullOrEmpty(r.TipoIncidente))
                    .GroupBy(r => r.TipoIncidente)
                    .ToDictionary(g => g.Key, g => g.Count()),
                Recientes = reportes
                    .OrderByDescending(r => r.FechaIncidente)
                    .Take(6)
                    .ToList()
            };

            vm.Reincidentes = reportes
                .GroupBy(r => r.AlumnoMatricula)
                .Where(g => g.Count() >= 2)
                .Select(g => new CasoReincidente
                {
                    Matricula = g.Key,
                    Nombre = g.First().Alumno != null
                        ? $"{g.First().Alumno!.Nombre} {g.First().Alumno!.Apellidos}"
                        : g.Key,
                    Carrera = g.First().Alumno?.Carrera ?? "-",
                    TotalReportes = g.Count(),
                    UltimoReporte = g.Max(r => r.FechaIncidente),
                    GravedadMaxima = OrdenGravedad(g.Select(r => r.Gravedad).DefaultIfEmpty("Baja").MaxBy(OrdenGravedad) ?? "Baja")
                })
                .OrderByDescending(c => c.TotalReportes)
                .ThenByDescending(c => c.UltimoReporte)
                .ToList();

            return View(vm);
        }

        [Authorize(Roles = "Administrador,Autoridad")]
        public async Task<IActionResult> Crear(string? matricula)
        {
            await CargarCombosAsync();
            var model = new ReporteCosecovi
            {
                AlumnoMatricula = matricula ?? string.Empty,
                Periodo = PeriodoActual(),
                FechaIncidente = DateTime.Now,
                Estado = "Registrado"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Autoridad")]
        [AuditLog(Accion = "Crear", Tabla = "ReportesCosecovi")]
        public async Task<IActionResult> Crear(
            [Bind("AlumnoMatricula,Periodo,FechaIncidente,TipoIncidente,Gravedad,Lugar,SituacionObservada,MedidasTomadas,AccionesPropuestas,Canalizacion,TutorNotificado,PadreTutorCitado,Estado")] ReporteCosecovi reporte)
        {
            if (!await _context.Alumnos.AnyAsync(a => a.Matricula == reporte.AlumnoMatricula))
            {
                ModelState.AddModelError(nameof(reporte.AlumnoMatricula), "Selecciona un alumno valido.");
            }

            ValidarCatalogos(reporte);

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync();
                return View(reporte);
            }

            reporte.FechaReporte = DateTime.Now;
            reporte.ElaboradoPor = User.Identity?.Name;
            reporte.Estado = string.IsNullOrEmpty(reporte.Estado) ? "Registrado" : reporte.Estado;

            _context.ReportesCosecovi.Add(reporte);
            await _context.SaveChangesAsync();

            await NotificarTutoresAsync(reporte);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Incidente COSECOVI registrado.";
            return RedirectToAction(nameof(Detalle), new { id = reporte.Id });
        }

        [Authorize(Roles = "Administrador,Autoridad")]
        public async Task<IActionResult> Editar(int id)
        {
            var reporte = await _context.ReportesCosecovi
                .Include(r => r.Alumno)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (reporte == null) return NotFound();

            await CargarCombosAsync();
            return View(reporte);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Autoridad")]
        [AuditLog(Accion = "Editar", Tabla = "ReportesCosecovi")]
        public async Task<IActionResult> Editar(
            int id,
            [Bind("FechaIncidente,TipoIncidente,Gravedad,Lugar,SituacionObservada,MedidasTomadas,AccionesPropuestas,Canalizacion,TutorNotificado,PadreTutorCitado,Estado")] ReporteCosecovi form)
        {
            var reporte = await _context.ReportesCosecovi.FindAsync(id);
            if (reporte == null) return NotFound();

            ValidarCatalogos(form);
            if (!ModelState.IsValid)
            {
                form.Id = id;
                form.AlumnoMatricula = reporte.AlumnoMatricula;
                form.Periodo = reporte.Periodo;
                form.Alumno = await _context.Alumnos.FindAsync(reporte.AlumnoMatricula);
                await CargarCombosAsync();
                return View(form);
            }

            reporte.FechaIncidente = form.FechaIncidente;
            reporte.TipoIncidente = form.TipoIncidente;
            reporte.Gravedad = form.Gravedad;
            reporte.Lugar = form.Lugar;
            reporte.SituacionObservada = form.SituacionObservada;
            reporte.MedidasTomadas = form.MedidasTomadas;
            reporte.AccionesPropuestas = form.AccionesPropuestas;
            reporte.Canalizacion = form.Canalizacion;
            reporte.TutorNotificado = form.TutorNotificado;
            reporte.PadreTutorCitado = form.PadreTutorCitado;
            reporte.Estado = string.IsNullOrEmpty(form.Estado) ? reporte.Estado : form.Estado;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Reporte actualizado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var reporte = await _context.ReportesCosecovi
                .Include(r => r.Alumno)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (reporte == null) return NotFound();

            if (!await PuedeVerReporteAsync(reporte))
                return Forbid();

            ViewBag.HistorialAlumno = await ReportesVisibles()
                .Where(r => r.AlumnoMatricula == reporte.AlumnoMatricula && r.Id != reporte.Id)
                .OrderByDescending(r => r.FechaIncidente)
                .ToListAsync();

            return View(reporte);
        }

        public async Task<IActionResult> Alumno(string? matricula)
        {
            if (string.IsNullOrWhiteSpace(matricula))
            {
                var visible = await _studentAccessService.GetVisibleMatriculasAsync();
                if (visible.Count == 1)
                {
                    matricula = visible[0];
                }
            }

            var query = ReportesVisibles();
            if (!string.IsNullOrWhiteSpace(matricula))
            {
                if (!await _studentAccessService.CanAccessAlumnoAsync(matricula))
                    return Forbid();

                query = query.Where(r => r.AlumnoMatricula == matricula);
            }

            var reportes = await query
                .OrderByDescending(r => r.FechaIncidente)
                .ToListAsync();

            ViewBag.Matricula = matricula;
            ViewBag.Alumno = string.IsNullOrWhiteSpace(matricula)
                ? null
                : await _context.Alumnos.Include(a => a.Grupo).FirstOrDefaultAsync(a => a.Matricula == matricula);

            return View(reportes);
        }

        private IQueryable<ReporteCosecovi> ReportesVisibles()
        {
            var visibleStudents = _studentAccessService.ApplyVisibleStudents(_context.Alumnos.AsQueryable());
            return _context.ReportesCosecovi
                .Include(r => r.Alumno)
                .Where(r => visibleStudents.Any(a => a.Matricula == r.AlumnoMatricula));
        }

        private async Task<bool> PuedeVerReporteAsync(ReporteCosecovi reporte)
        {
            if (User.IsInRole("Administrador") || User.IsInRole("Autoridad"))
                return true;

            return await _studentAccessService.CanAccessAlumnoAsync(reporte.AlumnoMatricula);
        }

        private static string PeriodoActual()
        {
            var now = DateTime.Now;
            var sufijo = now.Month <= 6 ? "B" : "A";
            return $"{now.Year}-{sufijo}";
        }

        private async Task CargarCombosAsync()
        {
            ViewBag.Alumnos = await _context.Alumnos
                .OrderBy(a => a.Apellidos)
                .Select(a => new SelectListItem
                {
                    Value = a.Matricula,
                    Text = $"{a.Matricula} - {a.Nombre} {a.Apellidos}"
                })
                .ToListAsync();
            ViewBag.TiposIncidente = TiposIncidente;
            ViewBag.Gravedades = Gravedades;
            ViewBag.Turnados = Turnados;
            ViewBag.Estados = Estados;
        }

        private void ValidarCatalogos(ReporteCosecovi reporte)
        {
            if (!TiposIncidente.Contains(reporte.TipoIncidente))
                ModelState.AddModelError(nameof(reporte.TipoIncidente), "Selecciona un tipo de incidente valido.");
            if (!Gravedades.Contains(reporte.Gravedad))
                ModelState.AddModelError(nameof(reporte.Gravedad), "Selecciona una gravedad valida.");
            if (!string.IsNullOrWhiteSpace(reporte.Canalizacion) && !Turnados.Contains(reporte.Canalizacion))
                ModelState.AddModelError(nameof(reporte.Canalizacion), "Selecciona un destino valido.");
            if (!Estados.Contains(reporte.Estado))
                ModelState.AddModelError(nameof(reporte.Estado), "Selecciona un estado valido.");
        }

        private async Task NotificarTutoresAsync(ReporteCosecovi reporte)
        {
            var alumno = await _context.Alumnos
                .FirstOrDefaultAsync(a => a.Matricula == reporte.AlumnoMatricula);
            if (alumno == null) return;

            var tutores = await _context.Grupos
                .Where(g => g.Alumnos.Any(a => a.Matricula == reporte.AlumnoMatricula) && g.Profesor != null)
                .Select(g => g.Profesor!)
                .Distinct()
                .ToListAsync();

            foreach (var tutor in tutores)
            {
                if (!string.IsNullOrEmpty(tutor?.UserId))
                {
                    _notificationService.Add(
                        tutor.UserId,
                        $"Reporte COSECOVI: {alumno.Nombre} {alumno.Apellidos}",
                        $"{reporte.TipoIncidente} ({reporte.Gravedad}). Estado: {reporte.Estado}.",
                        reporte.Gravedad is "Alta" or "Critica" ? "Advertencia" : "Info",
                        $"/Cosecovi/Detalle/{reporte.Id}");
                }
            }
        }

        private static int OrdenGravedad(string gravedad)
        {
            return gravedad switch
            {
                "Critica" => 4,
                "Alta" => 3,
                "Media" => 2,
                _ => 1
            };
        }
    }

    public class CosecoviDashboardViewModel
    {
        public int Total { get; set; }
        public int Registrados { get; set; }
        public int EnSeguimiento { get; set; }
        public int Escalados { get; set; }
        public int Cerrados { get; set; }
        public int AltaPrioridad { get; set; }
        public Dictionary<string, int> PorTipoIncidente { get; set; } = new();
        public List<ReporteCosecovi> Recientes { get; set; } = new();
        public List<CasoReincidente> Reincidentes { get; set; } = new();
    }

    public class CasoReincidente
    {
        public string Matricula { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
        public int TotalReportes { get; set; }
        public DateTime UltimoReporte { get; set; }
        public int GravedadMaxima { get; set; }
    }
}
