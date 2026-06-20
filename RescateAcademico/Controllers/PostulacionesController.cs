using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Data;
using RescateAcademico.Data;
using RescateAcademico.Filters;
using RescateAcademico.Models;
using RescateAcademico.Services;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class PostulacionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StudentAccessService _studentAccessService;
        private readonly ConvocatoriaEligibilityService _eligibilityService;
        private readonly FileStorageService _fileStorageService;
        private readonly NotificationService _notificationService;

        public PostulacionesController(
            ApplicationDbContext context,
            StudentAccessService studentAccessService,
            ConvocatoriaEligibilityService eligibilityService,
            FileStorageService fileStorageService,
            NotificationService notificationService)
        {
            _context = context;
            _studentAccessService = studentAccessService;
            _eligibilityService = eligibilityService;
            _fileStorageService = fileStorageService;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> MisPostulaciones()
        {
            var alumno = await _studentAccessService.GetCurrentAlumnoAsync();

            if (alumno == null)
            {
                TempData["Error"] = "No tienes un perfil de alumno asociado";
                return RedirectToAction("Index", "Home");
            }

            var postulaciones = await _context.Postulaciones
                .Include(p => p.Proyecto)
                .Where(p => p.AlumnoId == alumno.Matricula)
                .OrderByDescending(p => p.FechaSolicitud)
                .ToListAsync();

            return View(postulaciones);
        }

        [HttpGet]
        public async Task<IActionResult> Postularse(int convocatoriaId)
        {
            var alumno = await _studentAccessService.GetCurrentAlumnoAsync();

            if (alumno == null)
            {
                TempData["Error"] = "No tienes un perfil de alumno asociado";
                return RedirectToAction("Index", "Home");
            }

            var convocatoria = await _context.Convocatorias.FindAsync(convocatoriaId);
            if (convocatoria == null)
            {
                TempData["Error"] = "Convocatoria no encontrada";
                return RedirectToAction("Index", "Convocatorias");
            }
            if (!convocatoria.ProyectoId.HasValue)
            {
                TempData["Error"] = "Esta convocatoria no tiene un proyecto asociado y no puede recibir postulaciones.";
                return RedirectToAction("Index", "Convocatorias");
            }

            var elegibilidad = await _eligibilityService.EvaluarAsync(alumno, convocatoria);
            if (!elegibilidad.IsEligible)
            {
                TempData["Error"] = elegibilidad.Message;
                return RedirectToAction("Index", "Convocatorias");
            }

            ViewBag.PromedioAlumno = alumno.PromedioGlobal;
            return View(convocatoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [EnableRateLimiting("postulation")]
        public async Task<IActionResult> Postularse(int convocatoriaId, IFormFile? documento)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var alumno = await _studentAccessService.GetCurrentAlumnoAsync();

            if (alumno == null)
            {
                TempData["Error"] = "No tienes un perfil de alumno asociado";
                return RedirectToAction("Index", "Home");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var convocatoria = await _context.Convocatorias.FindAsync(convocatoriaId);
            if (convocatoria == null)
            {
                TempData["Error"] = "Convocatoria no encontrada";
                return RedirectToAction("Index", "Convocatorias");
            }
            if (!convocatoria.ProyectoId.HasValue)
            {
                TempData["Error"] = "Esta convocatoria no tiene un proyecto asociado y no puede recibir postulaciones.";
                return RedirectToAction("Index", "Convocatorias");
            }

            var elegibilidad = await _eligibilityService.EvaluarAsync(alumno, convocatoria);
            if (!elegibilidad.IsEligible)
            {
                TempData["Error"] = elegibilidad.Message;
                return RedirectToAction("Index", "Convocatorias");
            }

            if (await _context.Postulaciones.AnyAsync(p =>
                p.AlumnoId == alumno.Matricula && p.ProyectoId == convocatoria.ProyectoId.Value))
            {
                TempData["Error"] = "Ya tienes una postulación para este proyecto.";
                return RedirectToAction("Index", "Convocatorias");
            }

            var postulacion = new Postulacion
            {
                AlumnoId = alumno.Matricula,
                ProyectoId = convocatoria.ProyectoId.Value,
                FechaSolicitud = DateTime.Now,
                Estado = "En Revisión"
            };

            if (documento != null && documento.Length > 0)
            {
                try
                {
                    var storedFile = await _fileStorageService.SavePostulacionDocumentAsync(documento);
                    postulacion.DocumentoNombre = storedFile.OriginalName;
                    postulacion.DocumentoRuta = storedFile.Path;
                    postulacion.DocumentoTamano = storedFile.Size;
                }
                catch (InvalidOperationException ex)
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction("Index", "Convocatorias");
                }
            }

            convocatoria.PostulacionesActuales++;
            _context.Postulaciones.Add(postulacion);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _notificationService.Add(
                userId!,
                "Postulacion Enviada",
                $"Tu solicitud para '{convocatoria.Titulo}' ha sido enviada exitosamente.",
                "Exito",
                Url.Action("MisPostulaciones", "Postulaciones"));

            _context.SugerenciasIA.Add(new SugerenciaIA
            {
                AlumnoMatricula = alumno.Matricula,
                ConvocatoriaId = convocatoria.Id,
                ProyectoId = convocatoria.ProyectoId,
                Tipo = "CargaAcademica",
                Titulo = "Resultado de elegibilidad automatica",
                Descripcion = $"Postulación enviada a '{convocatoria.Titulo}'. Promedio {alumno.PromedioGlobal:F2}, carga {alumno.CargaAcademicaActual ?? 0} materias, estado inicial En Revisión.",
                Puntuacion = alumno.PromedioGlobal * 10,
                Razonamiento = "Reglas aplicadas: promedio mínimo, semestre, cupo, carga académica y carrera requerida.",
                Mostrada = true
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Postulacion enviada exitosamente";
            return RedirectToAction("MisPostulaciones");
        }

        [Authorize(Roles = "Administrador,Autoridad")]
        public async Task<IActionResult> Todas(string? estado, string? grupo)
        {
            var query = _context.Postulaciones
                .Include(p => p.Alumno)
                    .ThenInclude(a => a!.Grupo)
                .Include(p => p.Proyecto)
                .AsQueryable();

            if (!string.IsNullOrEmpty(grupo))
                query = query.Where(p => p.Alumno != null && p.Alumno.Grupo != null && p.Alumno.Grupo.Clave == grupo);

            var scopedQuery = query;

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                query = query.Where(p => p.Estado == estado);

            var postulaciones = await query.OrderByDescending(p => p.FechaSolicitud).ToListAsync();
            ViewBag.Estados = new[] { "Todos", "En Revisión", "Aceptado", "Rechazado" };
            ViewBag.EstadoSeleccionado = string.IsNullOrEmpty(estado) ? "Todos" : estado;
            ViewBag.GrupoSeleccionado = grupo;
            ViewBag.Grupos = await _context.Grupos.Select(g => g.Clave).Distinct().OrderBy(g => g).ToListAsync();
            ViewBag.TotalPostulaciones = await scopedQuery.CountAsync();
            ViewBag.PostulacionesPendientes = await scopedQuery.CountAsync(p => p.Estado == "En Revisión");
            ViewBag.PostulacionesAceptadas = await scopedQuery.CountAsync(p => p.Estado == "Aceptado");
            ViewBag.PostulacionesRechazadas = await scopedQuery.CountAsync(p => p.Estado == "Rechazado");
            return View(postulaciones);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Autoridad")]
        [AuditLog(Accion = "Cambiar Estado", Tabla = "Postulaciones")]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            var estadosPermitidos = new[] { "En Revisión", "Aceptado", "Rechazado" };
            if (!estadosPermitidos.Contains(estado))
            {
                return BadRequest("Estado invalido");
            }

            var postulacion = await _context.Postulaciones
                .Include(p => p.Alumno)
                .Include(p => p.Proyecto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (postulacion == null) return NotFound();

            postulacion.Estado = estado;
            var convocatoria = await _context.Convocatorias
                .FirstOrDefaultAsync(c => c.ProyectoId == postulacion.ProyectoId && c.EstaActiva);
            if (convocatoria != null)
            {
                convocatoria.PostulacionesActuales = await _context.Postulaciones
                    .CountAsync(p => p.ProyectoId == postulacion.ProyectoId && p.Id != postulacion.Id && p.Estado != "Rechazado")
                    + (estado == "Rechazado" ? 0 : 1);
            }
            await _context.SaveChangesAsync();

            if (postulacion.Alumno?.UserId != null)
            {
                var titulo = estado == "Aceptado" ? "Postulacion Aceptada" :
                             estado == "Rechazado" ? "Postulacion Rechazada" : "Estado de Postulacion Actualizado";

                var mensaje = estado == "Aceptado" ? $"Felicidades, tu postulacion a '{postulacion.Proyecto?.Titulo}' ha sido aceptada." :
                              estado == "Rechazado" ? $"Tu postulacion a '{postulacion.Proyecto?.Titulo}' no fue aceptada en esta ocasion." :
                              $"El estado de tu postulacion a '{postulacion.Proyecto?.Titulo}' ha cambiado a: {estado}";

                _notificationService.Add(
                    postulacion.Alumno.UserId,
                    titulo,
                    mensaje,
                    estado == "Aceptado" ? "Exito" : estado == "Rechazado" ? "Error" : "Informacion",
                    referenciaId: id);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Estado actualizado a '{estado}'";
            return RedirectToAction("Todas");
        }

        [HttpGet]
        [Authorize(Roles = "Alumno,Administrador,Autoridad")]
        public async Task<IActionResult> DescargarDocumento(int id)
        {
            var postulacion = await _context.Postulaciones.FirstOrDefaultAsync(p => p.Id == id);
            if (postulacion == null || string.IsNullOrWhiteSpace(postulacion.DocumentoRuta)) return NotFound();

            if (User.IsInRole("Alumno"))
            {
                var alumno = await _studentAccessService.GetCurrentAlumnoAsync();
                if (alumno?.Matricula != postulacion.AlumnoId) return Forbid();
            }

            var path = _fileStorageService.GetPostulacionDocumentPath(postulacion.DocumentoRuta);
            if (path == null) return NotFound();

            var contentType = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
            return File(System.IO.File.OpenRead(path), contentType, postulacion.DocumentoNombre ?? Path.GetFileName(path));
        }
    }
}
