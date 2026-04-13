using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class BitacoraController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BitacoraController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? usuario, string? accion, DateTime? desde, DateTime? hasta)
        {
            var query = _context.BitacoraLogs.AsQueryable();

            if (!string.IsNullOrEmpty(usuario))
                query = query.Where(b => b.UsuarioEmail != null && b.UsuarioEmail.Contains(usuario));

            if (!string.IsNullOrEmpty(accion) && accion != "Todos")
                query = query.Where(b => b.Accion == accion);

            if (desde.HasValue)
                query = query.Where(b => b.FechaHora >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(b => b.FechaHora <= hasta.Value.AddDays(1));

            var logs = await query
                .OrderByDescending(b => b.FechaHora)
                .Take(500)
                .ToListAsync();

            ViewBag.Usuarios = await _context.BitacoraLogs
                .Select(b => b.UsuarioEmail)
                .Distinct()
                .ToListAsync();
            
            ViewBag.Acciones = await _context.BitacoraLogs
                .Select(b => b.Accion)
                .Distinct()
                .ToListAsync();

            return View(logs);
        }

        public async Task<IActionResult> Detalles(int id)
        {
            var log = await _context.BitacoraLogs.FindAsync(id);
            if (log == null) return NotFound();
            return View(log);
        }

        public async Task<IActionResult> Exportar(string? usuario, DateTime? desde, DateTime? hasta)
        {
            var query = _context.BitacoraLogs.AsQueryable();

            if (!string.IsNullOrEmpty(usuario))
                query = query.Where(b => b.UsuarioEmail != null && b.UsuarioEmail.Contains(usuario));

            if (desde.HasValue)
                query = query.Where(b => b.FechaHora >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(b => b.FechaHora <= hasta.Value.AddDays(1));

            var logs = await query.OrderByDescending(b => b.FechaHora).ToListAsync();
            return View(logs);
        }
    }
}
