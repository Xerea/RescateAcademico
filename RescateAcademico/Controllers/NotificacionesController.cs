using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class NotificacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            var notificaciones = await _context.Notificaciones
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.FechaCreacion)
                .Take(50)
                .ToListAsync();

            var sinLeer = notificaciones.Count(n => !n.Leida);
            ViewBag.SinLeer = sinLeer;

            return View(notificaciones);
        }

        public async Task<IActionResult> MarcarLeida(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var notificacion = await _context.Notificaciones.FindAsync(id);

            if (notificacion != null && notificacion.UserId == userId)
            {
                notificacion.Leida = true;
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(notificacion.Enlace))
                    return Redirect(notificacion.Enlace);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var notificaciones = await _context.Notificaciones
                .Where(n => n.UserId == userId && !n.Leida)
                .ToListAsync();

            foreach (var n in notificaciones)
                n.Leida = true;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<int> GetConteoNoLeidas()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return await _context.Notificaciones.CountAsync(n => n.UserId == userId && !n.Leida);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarAntiguas()
        {
            var fechaLimite = DateTime.Now.AddDays(-30);
            var antiguas = await _context.Notificaciones
                .Where(n => n.FechaCreacion < fechaLimite)
                .ToListAsync();

            _context.Notificaciones.RemoveRange(antiguas);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Se eliminaron {antiguas.Count} notificaciones antiguas";
            return RedirectToAction("Index", "Notificaciones");
        }
    }
}
