using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Filters;
using RescateAcademico.Models;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class PostulacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PostulacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> MisPostulaciones()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.UserId == userId);

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
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.UserId == userId);

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

            if (convocatoria.FechaCierre < DateTime.Now)
            {
                TempData["Error"] = "Esta convocatoria ya está cerrada";
                return RedirectToAction("Index", "Convocatorias");
            }

            if (convocatoria.CupoMaximo > 0 && convocatoria.PostulacionesActuales >= convocatoria.CupoMaximo)
            {
                TempData["Error"] = "Esta convocatoria ya no tiene cupo disponible";
                return RedirectToAction("Index", "Convocatorias");
            }

            if (convocatoria.PromedioMinimo.HasValue && alumno.PromedioGlobal < convocatoria.PromedioMinimo.Value)
            {
                TempData["Error"] = $"Tu promedio ({alumno.PromedioGlobal:F2}) no cumple con el mínimo requerido ({convocatoria.PromedioMinimo:F2})";
                return RedirectToAction("Index", "Convocatorias");
            }

            if (convocatoria.SemestreMinimo.HasValue && alumno.SemestreActual < convocatoria.SemestreMinimo.Value)
            {
                TempData["Error"] = $"No cumples el semestre mínimo requerido ({convocatoria.SemestreMinimo}).";
                return RedirectToAction("Index", "Convocatorias");
            }

            if (!string.IsNullOrWhiteSpace(convocatoria.CarreraRequerida) &&
                !string.Equals(convocatoria.CarreraRequerida, alumno.Carrera, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = $"La convocatoria requiere carrera '{convocatoria.CarreraRequerida}' y tu carrera actual es '{alumno.Carrera}'.";
                return RedirectToAction("Index", "Convocatorias");
            }

            if ((alumno.CargaAcademicaActual ?? 0) >= 8 && alumno.PromedioGlobal < 8.0m)
            {
                TempData["Error"] = "Tu carga académica actual es alta. Se recomienda regularizar materias antes de postularte a más actividades.";
                return RedirectToAction("Index", "Convocatorias");
            }

            var yaPostulado = await _context.Postulaciones
                .AnyAsync(p => p.AlumnoId == alumno.Matricula && p.ProyectoId == convocatoria.ProyectoId);

            if (yaPostulado)
            {
                TempData["Error"] = "Ya te has postulado a este proyecto";
                return RedirectToAction("Index", "Convocatorias");
            }

            return View(convocatoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB total request
        public async Task<IActionResult> Postularse(int convocatoriaId, IFormFile? documento)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.UserId == userId);

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

            if (convocatoria.FechaCierre < DateTime.Now)
            {
                TempData["Error"] = "Esta convocatoria ya está cerrada";
                return RedirectToAction("Index", "Convocatorias");
            }

            if (convocatoria.CupoMaximo > 0 && convocatoria.PostulacionesActuales >= convocatoria.CupoMaximo)
            {
                TempData["Error"] = "Esta convocatoria ya no tiene cupo disponible";
                return RedirectToAction("Index", "Convocatorias");
            }

            var yaPostulado = await _context.Postulaciones
                .AnyAsync(p => p.AlumnoId == alumno.Matricula && p.ProyectoId == convocatoria.ProyectoId);

            if (yaPostulado)
            {
                TempData["Error"] = "Ya te has postulado a este proyecto";
                return RedirectToAction("Index", "Convocatorias");
            }

            var postulacion = new Postulacion
            {
                AlumnoId = alumno.Matricula,
                ProyectoId = convocatoria.ProyectoId ?? 0,
                FechaSolicitud = DateTime.Now,
                Estado = "En Revisión"
            };

            if (documento != null && documento.Length > 0)
            {
                const long maxFileSize = 5 * 1024 * 1024; // 5 MB
                if (documento.Length > maxFileSize)
                {
                    TempData["Error"] = "El archivo excede el tamaño máximo permitido de 5 MB.";
                    return RedirectToAction("Index", "Convocatorias");
                }

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(documento.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Tipo de archivo no permitido. Solo se aceptan PDF, DOC, DOCX, JPG, JPEG y PNG.";
                    return RedirectToAction("Index", "Convocatorias");
                }

                var allowedMimeTypes = new Dictionary<string, string[]>
                {
                    [".pdf"] = new[] { "application/pdf" },
                    [".doc"] = new[] { "application/msword" },
                    [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                    [".jpg"] = new[] { "image/jpeg" },
                    [".jpeg"] = new[] { "image/jpeg" },
                    [".png"] = new[] { "image/png" }
                };
                if (!allowedMimeTypes[ext].Contains(documento.ContentType.ToLowerInvariant()))
                {
                    TempData["Error"] = "El contenido del archivo no coincide con su extensión.";
                    return RedirectToAction("Index", "Convocatorias");
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "postulaciones");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var safeFileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, safeFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await documento.CopyToAsync(stream);
                }

                postulacion.DocumentoNombre = documento.FileName;
                postulacion.DocumentoRuta = $"/uploads/postulaciones/{safeFileName}";
                postulacion.DocumentoTamano = documento.Length;
            }

            convocatoria.PostulacionesActuales++;
            _context.Postulaciones.Add(postulacion);
            await _context.SaveChangesAsync();

            var notificacion = new Notificacion
            {
                UserId = userId!,
                Titulo = "Postulación Enviada",
                Mensaje = $"Tu solicitud para '{convocatoria.Titulo}' ha sido enviada exitosamente.",
                Tipo = "Exito",
                Enlace = Url.Action("MisPostulaciones", "Postulaciones")
            };
            _context.Notificaciones.Add(notificacion);

            _context.SugerenciasIA.Add(new SugerenciaIA
            {
                AlumnoMatricula = alumno.Matricula,
                ConvocatoriaId = convocatoria.Id,
                ProyectoId = convocatoria.ProyectoId,
                Tipo = "CargaAcademica",
                Titulo = "Resultado de elegibilidad automática",
                Descripcion = $"Postulación enviada a '{convocatoria.Titulo}'. Promedio {alumno.PromedioGlobal:F2}, carga {alumno.CargaAcademicaActual ?? 0} materias, estado inicial En Revisión.",
                Puntuacion = alumno.PromedioGlobal * 10,
                Razonamiento = "Reglas aplicadas: promedio mínimo, semestre, cupo, carga académica y carrera requerida.",
                Mostrada = true
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Postulación enviada exitosamente";
            return RedirectToAction("MisPostulaciones");
        }

        [Authorize(Roles = "Administrador,Autoridad")]
        public async Task<IActionResult> Todas(string? estado)
        {
            var query = _context.Postulaciones
                .Include(p => p.Alumno)
                .Include(p => p.Proyecto)
                .AsQueryable();

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                query = query.Where(p => p.Estado == estado);

            var postulaciones = await query.OrderByDescending(p => p.FechaSolicitud).ToListAsync();
            ViewBag.Estados = new[] { "Todos", "En Revisión", "Aceptado", "Rechazado" };
            return View(postulaciones);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Autoridad")]
        [AuditLog(Accion = "Cambiar Estado", Tabla = "Postulaciones")]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            var postulacion = await _context.Postulaciones
                .Include(p => p.Alumno)
                .Include(p => p.Proyecto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (postulacion == null) return NotFound();

            postulacion.Estado = estado;
            await _context.SaveChangesAsync();

            if (postulacion.Alumno?.UserId != null)
            {
                var titulo = estado == "Aceptado" ? "¡Postulación Aceptada!" : 
                             estado == "Rechazado" ? "Postulación Rechazada" : "Estado de Postulación Actualizado";
                
                var mensaje = estado == "Aceptado" ? $"Felicidades, tu postulación a '{postulacion.Proyecto?.Titulo}' ha sido aceptada." :
                              estado == "Rechazado" ? $"Tu postulación a '{postulacion.Proyecto?.Titulo}' no fue aceptada en esta ocasión." :
                              $"El estado de tu postulación a '{postulacion.Proyecto?.Titulo}' ha cambiado a: {estado}";

                var notificacion = new Notificacion
                {
                    UserId = postulacion.Alumno.UserId,
                    Titulo = titulo,
                    Mensaje = mensaje,
                    Tipo = estado == "Aceptado" ? "Exito" : estado == "Rechazado" ? "Error" : "Informacion",
                    ReferenciaId = id
                };
                _context.Notificaciones.Add(notificacion);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Estado actualizado a '{estado}'";
            return RedirectToAction("Todas");
        }
    }
}
