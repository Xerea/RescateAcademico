using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Controllers
{
    [Authorize]
    public class ConvocatoriasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConvocatoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? tipo, string? area, string? busqueda)
        {
            var query = _context.Convocatorias
                .Where(c => c.EstaActiva && c.ValidadaPorAcademia)
                .AsQueryable();

            if (!string.IsNullOrEmpty(tipo) && tipo != "Todos")
                query = query.Where(c => c.Tipo == tipo);

            if (!string.IsNullOrEmpty(area))
                query = query.Where(c => c.Area != null && c.Area.Contains(area));

            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(c => c.Titulo.Contains(busqueda) || (c.Descripcion != null && c.Descripcion.Contains(busqueda)));

            var convocatorias = await query.OrderByDescending(c => c.FechaPublicacion).ToListAsync();
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
        public IActionResult Create()
        {
            ViewBag.Proyectos = _context.Proyectos.Where(p => p.EstaActivo).ToList();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create(Convocatoria convocatoria)
        {
            if (ModelState.IsValid)
            {
                convocatoria.FechaPublicacion = DateTime.Now;
                _context.Convocatorias.Add(convocatoria);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Convocatoria creada exitosamente";
                return RedirectToAction("Todas");
            }
            ViewBag.Proyectos = _context.Proyectos.Where(p => p.EstaActivo).ToList();
            return View(convocatoria);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            var convocatoria = await _context.Convocatorias.FindAsync(id);
            if (convocatoria == null) return NotFound();
            ViewBag.Proyectos = _context.Proyectos.ToList();
            return View(convocatoria);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(Convocatoria convocatoria)
        {
            if (ModelState.IsValid)
            {
                _context.Update(convocatoria);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Convocatoria actualizada";
                return RedirectToAction("Todas");
            }
            ViewBag.Proyectos = _context.Proyectos.ToList();
            return View(convocatoria);
        }

        [Authorize(Roles = "Administrador")]
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

        [Authorize(Roles = "Administrador")]
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
