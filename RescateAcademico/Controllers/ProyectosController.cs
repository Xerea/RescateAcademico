using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class ProyectosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProyectosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? tipo, string? busqueda)
        {
            var query = _context.Proyectos.Where(p => p.EstaActivo).AsQueryable();

            if (!string.IsNullOrEmpty(tipo) && tipo != "Todos")
                query = query.Where(p => p.Tipo == tipo);

            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(p => p.Titulo.Contains(busqueda) || p.Descripcion.Contains(busqueda));

            var proyectos = await query.OrderByDescending(p => p.Id).ToListAsync();
            return View(proyectos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.Postulaciones)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto == null) return NotFound();
            return View(proyecto);
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create(Proyecto proyecto)
        {
            if (ModelState.IsValid)
            {
                _context.Proyectos.Add(proyecto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Proyecto creado exitosamente";
                return RedirectToAction("Index");
            }
            return View(proyecto);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            var proyecto = await _context.Proyectos.FindAsync(id);
            if (proyecto == null) return NotFound();
            return View(proyecto);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(Proyecto proyecto)
        {
            if (ModelState.IsValid)
            {
                _context.Update(proyecto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Proyecto actualizado";
                return RedirectToAction("Index");
            }
            return View(proyecto);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var proyecto = await _context.Proyectos.FindAsync(id);
            if (proyecto != null)
            {
                proyecto.EstaActivo = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Proyecto desactivado";
            }
            return RedirectToAction("Index");
        }
    }
}
