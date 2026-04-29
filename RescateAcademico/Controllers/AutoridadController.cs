using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Autoridad")]
    public class AutoridadController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AutoridadController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Profesores()
        {
            var tutores = await _context.Tutores
                .Include(t => t.Usuario)
                .OrderBy(t => t.Apellidos)
                .ToListAsync();

            var grupos = await _context.Grupos.ToListAsync();
            ViewBag.GruposPorProfesor = grupos
                .Where(g => g.ProfesorId.HasValue)
                .GroupBy(g => g.ProfesorId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            return View(tutores);
        }
    }
}