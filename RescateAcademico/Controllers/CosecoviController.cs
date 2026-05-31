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
    [Authorize(Roles = "Administrador,Autoridad")]
    public class CosecoviController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public CosecoviController(ApplicationDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public static readonly string[] Canalizaciones =
        {
            "Psicología", "Trabajo Social", "Orientación Educativa", "Médico", "Tutoría Académica"
        };

        public static readonly string[] Estados =
        {
            "Pendiente", "Atendido", "Seguimiento", "Cerrado"
        };

        // ── List + filters ───────────────────────────────────────────────
        public async Task<IActionResult> Index(string? periodo, string? estado, string? canalizacion)
        {
            var query = _context.ReportesCosecovi
                .Include(r => r.Alumno)
                .AsQueryable();

            if (!string.IsNullOrEmpty(periodo))
                query = query.Where(r => r.Periodo == periodo);
            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                query = query.Where(r => r.Estado == estado);
            if (!string.IsNullOrEmpty(canalizacion) && canalizacion != "Todas")
                query = query.Where(r => r.Canalizacion == canalizacion);

            var reportes = await query
                .OrderByDescending(r => r.FechaReporte)
                .ToListAsync();

            ViewBag.Periodos = await _context.ReportesCosecovi
                .Select(r => r.Periodo)
                .Distinct()
                .OrderByDescending(p => p)
                .ToListAsync();
            ViewBag.FiltroPeriodo = periodo;
            ViewBag.FiltroEstado = estado;
            ViewBag.FiltroCanalizacion = canalizacion;

            return View(reportes);
        }

        // ── Dashboard ────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            var reportes = await _context.ReportesCosecovi
                .Include(r => r.Alumno)
                .ToListAsync();

            var vm = new CosecoviDashboardViewModel
            {
                Total = reportes.Count,
                Pendientes = reportes.Count(r => r.Estado == "Pendiente"),
                EnSeguimiento = reportes.Count(r => r.Estado == "Seguimiento"),
                Atendidos = reportes.Count(r => r.Estado == "Atendido"),
                Cerrados = reportes.Count(r => r.Estado == "Cerrado"),
                PorCanalizacion = reportes
                    .Where(r => !string.IsNullOrEmpty(r.Canalizacion))
                    .GroupBy(r => r.Canalizacion!)
                    .ToDictionary(g => g.Key, g => g.Count()),
                Recientes = reportes
                    .OrderByDescending(r => r.FechaReporte)
                    .Take(6)
                    .ToList()
            };

            // Casos crónicos: alumnos con 3+ reportes
            vm.CasosCronicos = reportes
                .GroupBy(r => r.AlumnoMatricula)
                .Where(g => g.Count() >= 3)
                .Select(g => new CasoCronico
                {
                    Matricula = g.Key,
                    Nombre = g.First().Alumno != null
                        ? $"{g.First().Alumno!.Nombre} {g.First().Alumno!.Apellidos}"
                        : g.Key,
                    Carrera = g.First().Alumno?.Carrera ?? "—",
                    TotalReportes = g.Count(),
                    UltimoReporte = g.Max(r => r.FechaReporte)
                })
                .OrderByDescending(c => c.TotalReportes)
                .ToList();

            return View(vm);
        }

        // ── Create ───────────────────────────────────────────────────────
        public async Task<IActionResult> Crear(string? matricula)
        {
            await CargarCombosAsync();
            var model = new ReporteCosecovi
            {
                AlumnoMatricula = matricula ?? string.Empty,
                Periodo = PeriodoActual()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog(Accion = "Crear", Tabla = "ReportesCosecovi")]
        public async Task<IActionResult> Crear(
            [Bind("AlumnoMatricula,Periodo,SituacionObservada,Recomendaciones,AccionesPropuestas,Canalizacion,Estado")] ReporteCosecovi reporte)
        {
            if (!await _context.Alumnos.AnyAsync(a => a.Matricula == reporte.AlumnoMatricula))
            {
                ModelState.AddModelError(nameof(reporte.AlumnoMatricula), "Selecciona un alumno válido.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync();
                return View(reporte);
            }

            reporte.FechaReporte = DateTime.Now;
            reporte.ElaboradoPor = User.Identity?.Name;
            if (string.IsNullOrEmpty(reporte.Estado)) reporte.Estado = "Pendiente";

            _context.ReportesCosecovi.Add(reporte);
            await _context.SaveChangesAsync();

            await NotificarTutoresAsync(reporte);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reporte COSECOVI creado y canalizado.";
            return RedirectToAction(nameof(Detalle), new { id = reporte.Id });
        }

        // ── Edit (status + follow-up) ────────────────────────────────────
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
        [AuditLog(Accion = "Editar", Tabla = "ReportesCosecovi")]
        public async Task<IActionResult> Editar(
            int id,
            [Bind("SituacionObservada,Recomendaciones,AccionesPropuestas,Canalizacion,Estado")] ReporteCosecovi form)
        {
            var reporte = await _context.ReportesCosecovi.FindAsync(id);
            if (reporte == null) return NotFound();

            reporte.SituacionObservada = form.SituacionObservada;
            reporte.Recomendaciones = form.Recomendaciones;
            reporte.AccionesPropuestas = form.AccionesPropuestas;
            reporte.Canalizacion = form.Canalizacion;
            reporte.Estado = string.IsNullOrEmpty(form.Estado) ? reporte.Estado : form.Estado;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Reporte actualizado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // ── Detail ───────────────────────────────────────────────────────
        public async Task<IActionResult> Detalle(int id)
        {
            var reporte = await _context.ReportesCosecovi
                .Include(r => r.Alumno)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (reporte == null) return NotFound();

            // Other reports for the same student (case history)
            ViewBag.HistorialAlumno = await _context.ReportesCosecovi
                .Where(r => r.AlumnoMatricula == reporte.AlumnoMatricula && r.Id != reporte.Id)
                .OrderByDescending(r => r.FechaReporte)
                .ToListAsync();

            return View(reporte);
        }

        // ── helpers ──────────────────────────────────────────────────────
        private static string PeriodoActual()
        {
            var now = DateTime.Now;
            var sufijo = now.Month <= 6 ? "B" : "A"; // ene-jun => B, ago-dic => A (IPN-ish)
            return $"{now.Year}-{sufijo}";
        }

        private async Task CargarCombosAsync()
        {
            ViewBag.Alumnos = await _context.Alumnos
                .OrderBy(a => a.Apellidos)
                .Select(a => new SelectListItem
                {
                    Value = a.Matricula,
                    Text = $"{a.Matricula} — {a.Nombre} {a.Apellidos}"
                })
                .ToListAsync();
            ViewBag.Canalizaciones = Canalizaciones;
            ViewBag.Estados = Estados;
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
                        $"Canalización COSECOVI: {alumno.Nombre} {alumno.Apellidos}",
                        $"Tu alumno {alumno.Nombre} {alumno.Apellidos} ({alumno.Matricula}) ha sido canalizado a {reporte.Canalizacion ?? "atención COSECOVI"}.",
                        "Advertencia",
                        $"/Cosecovi/Detalle/{reporte.Id}");
                }
            }
        }
    }

    public class CosecoviDashboardViewModel
    {
        public int Total { get; set; }
        public int Pendientes { get; set; }
        public int EnSeguimiento { get; set; }
        public int Atendidos { get; set; }
        public int Cerrados { get; set; }
        public Dictionary<string, int> PorCanalizacion { get; set; } = new();
        public List<ReporteCosecovi> Recientes { get; set; } = new();
        public List<CasoCronico> CasosCronicos { get; set; } = new();
    }

    public class CasoCronico
    {
        public string Matricula { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
        public int TotalReportes { get; set; }
        public DateTime UltimoReporte { get; set; }
    }
}
