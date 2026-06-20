using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Filters;
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

        [Authorize(Roles = "Administrador,Autoridad")]
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

        [Authorize(Roles = "Administrador,Autoridad,Alumno")]
        public async Task<IActionResult> Details(int id)
        {
            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto == null) return NotFound();
            if (User.IsInRole("Alumno"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var puedeVer = !string.IsNullOrWhiteSpace(userId) && await _context.Postulaciones
                    .AnyAsync(p => p.ProyectoId == id && p.Alumno != null && p.Alumno.UserId == userId);
                if (!puedeVer) return Forbid();
            }
            return View(proyecto);
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([Bind("Titulo,Descripcion,Tipo,CupoMaximo,FechaCierre")] Proyecto proyecto)
        {
            ValidarProyecto(proyecto);
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
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit([Bind("Id,Titulo,Descripcion,Tipo,CupoMaximo,FechaCierre,EstaActivo")] Proyecto proyecto)
        {
            ValidarProyecto(proyecto);
            if (ModelState.IsValid)
            {
                _context.Update(proyecto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Proyecto actualizado";
                return RedirectToAction("Index");
            }
            return View(proyecto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        [AuditLog(Accion = "Desactivar", Tabla = "Proyectos")]
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

        private void ValidarProyecto(Proyecto proyecto)
        {
            if (proyecto.CupoMaximo < 1)
                ModelState.AddModelError(nameof(proyecto.CupoMaximo), "El cupo debe ser al menos uno.");
            if (proyecto.FechaCierre.Date < DateTime.Today)
                ModelState.AddModelError(nameof(proyecto.FechaCierre), "La fecha de cierre no puede ser anterior a hoy.");
        }
    }
}
