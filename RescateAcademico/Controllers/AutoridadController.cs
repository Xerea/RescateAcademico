using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Autoridad,Administrador")]
    public class AutoridadController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AutoridadController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Profesores(string? estado = "activos", string? grupo = null, string? busqueda = null)
        {
            var grupos = await _context.Grupos
                .Include(g => g.Alumnos)
                .OrderBy(g => g.Clave)
                .ToListAsync();

            var query = _context.Tutores
                .Include(t => t.Usuario)
                .AsQueryable();

            if (string.IsNullOrWhiteSpace(estado))
            {
                estado = "activos";
            }

            if (estado == "activos")
            {
                query = query.Where(t => t.EstaActivo);
            }
            else if (estado == "inactivos")
            {
                query = query.Where(t => !t.EstaActivo);
            }
            else if (estado == "sin-grupo")
            {
                var tutoresConGrupo = grupos
                    .Where(g => g.ProfesorId.HasValue)
                    .Select(g => g.ProfesorId!.Value)
                    .Distinct()
                    .ToList();

                query = query.Where(t => t.EstaActivo && !tutoresConGrupo.Contains(t.Id));
            }

            if (!string.IsNullOrWhiteSpace(grupo))
            {
                var profesorIds = grupos
                    .Where(g => g.Clave == grupo && g.ProfesorId.HasValue)
                    .Select(g => g.ProfesorId!.Value)
                    .Distinct()
                    .ToList();

                query = query.Where(t => profesorIds.Contains(t.Id));
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var termino = busqueda.Trim();
                query = query.Where(t =>
                    t.Nombre.Contains(termino) ||
                    t.Apellidos.Contains(termino) ||
                    t.Email.Contains(termino) ||
                    (t.NumeroEmpleado != null && t.NumeroEmpleado.Contains(termino)) ||
                    (t.Especialidad != null && t.Especialidad.Contains(termino)));
            }

            var tutores = await query
                .OrderBy(t => t.Apellidos)
                .ThenBy(t => t.Nombre)
                .ToListAsync();

            var tutoresVisibles = tutores.Select(t => t.Id).ToHashSet();
            var gruposAsignados = grupos
                .Where(g => g.ProfesorId.HasValue && tutoresVisibles.Contains(g.ProfesorId.Value))
                .ToList();

            ViewBag.GruposPorProfesor = grupos
                .Where(g => g.ProfesorId.HasValue)
                .GroupBy(g => g.ProfesorId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
            ViewBag.AlumnosPorProfesor = grupos
                .Where(g => g.ProfesorId.HasValue)
                .GroupBy(g => g.ProfesorId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(grupoAsignado => grupoAsignado.Alumnos.Count));

            ViewBag.TotalProfesores = tutores.Count;
            ViewBag.ProfesoresActivos = tutores.Count(t => t.EstaActivo);
            ViewBag.TotalGrupos = gruposAsignados.Count;
            ViewBag.ProfesoresSinGrupo = tutores.Count(t => t.EstaActivo && !grupos.Any(g => g.ProfesorId == t.Id));
            ViewBag.TotalAlumnosSupervisados = gruposAsignados.Sum(g => g.Alumnos.Count);
            ViewBag.Grupos = grupos.Select(g => g.Clave).Distinct().OrderBy(c => c).ToList();
            ViewBag.FiltroEstado = estado;
            ViewBag.FiltroGrupo = grupo;
            ViewBag.Busqueda = busqueda;

            return View(tutores);
        }
    }
}
