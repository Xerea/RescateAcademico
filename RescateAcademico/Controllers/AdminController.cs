using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new DashboardStats
            {
                TotalAlumnos = await _context.Alumnos.CountAsync(),
                TotalProyectos = await _context.Proyectos.CountAsync(),
                TotalConvocatorias = await _context.Convocatorias.Where(c => c.EstaActiva).CountAsync(),
                TotalPostulaciones = await _context.Postulaciones.CountAsync(),
                PostulacionesPendientes = await _context.Postulaciones.Where(p => p.Estado == "En Revisión").CountAsync(),
                AlumnosEnRiesgo = await _context.Alumnos.Where(a => a.RiesgoAcademico == "Rojo" || a.RiesgoAcademico == "Amarillo").CountAsync(),
                TotalTutores = await _context.Tutores.CountAsync()
            };
            return View(stats);
        }

        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _context.Users.ToListAsync();
            return View(usuarios);
        }

        public async Task<IActionResult> Alumnos()
        {
            var alumnos = await _context.Alumnos.Include(a => a.Usuario).ToListAsync();
            return View(alumnos);
        }

        public IActionResult CrearAlumno()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearAlumno(Alumno alumno)
        {
            if (ModelState.IsValid)
            {
                _context.Alumnos.Add(alumno);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Alumno creado exitosamente";
                return RedirectToAction("Alumnos");
            }
            return View(alumno);
        }

        public async Task<IActionResult> EditarAlumno(string id)
        {
            var alumno = await _context.Alumnos.FindAsync(id);
            if (alumno == null) return NotFound();
            return View(alumno);
        }

        [HttpPost]
        public async Task<IActionResult> EditarAlumno(Alumno alumno)
        {
            if (ModelState.IsValid)
            {
                _context.Update(alumno);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Alumno actualizado exitosamente";
                return RedirectToAction("Alumnos");
            }
            return View(alumno);
        }

        public async Task<IActionResult> EliminarAlumno(string id)
        {
            var alumno = await _context.Alumnos.FindAsync(id);
            if (alumno != null)
            {
                _context.Alumnos.Remove(alumno);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Alumno eliminado";
            }
            return RedirectToAction("Alumnos");
        }

        public async Task<IActionResult> Tutores()
        {
            var tutores = await _context.Tutores.Include(t => t.Usuario).ToListAsync();
            return View(tutores);
        }

        public IActionResult CrearTutor()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearTutor(Tutor tutor)
        {
            if (ModelState.IsValid)
            {
                _context.Tutores.Add(tutor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tutor creado exitosamente";
                return RedirectToAction("Tutores");
            }
            return View(tutor);
        }

        public async Task<IActionResult> CiclosEscolares()
        {
            var ciclos = await _context.CiclosEscolares.OrderByDescending(c => c.EsActual).ToListAsync();
            return View(ciclos);
        }

        public IActionResult CrearCiclo()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearCiclo(CicloEscolar ciclo)
        {
            if (ModelState.IsValid)
            {
                if (ciclo.EsActual)
                {
                    var otrosCiclos = await _context.CiclosEscolares.Where(c => c.EsActual).ToListAsync();
                    foreach (var c in otrosCiclos) c.EsActual = false;
                }
                _context.CiclosEscolares.Add(ciclo);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ciclo escolar creado";
                return RedirectToAction("CiclosEscolares");
            }
            return View(ciclo);
        }

        public async Task<IActionResult> Carreras()
        {
            var carreras = await _context.Carreras.OrderBy(c => c.Nombre).ToListAsync();
            return View(carreras);
        }

        public IActionResult CrearCarrera()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearCarrera(Carrera carrera)
        {
            if (ModelState.IsValid)
            {
                _context.Carreras.Add(carrera);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Carrera creada";
                return RedirectToAction("Carreras");
            }
            return View(carrera);
        }

        public async Task<IActionResult> ToggleActivoCarrera(int id)
        {
            var carrera = await _context.Carreras.FindAsync(id);
            if (carrera != null)
            {
                carrera.EstaActiva = !carrera.EstaActiva;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Carreras");
        }

        public async Task<IActionResult> Bitacora()
        {
            var logs = await _context.BitacoraLogs
                .OrderByDescending(b => b.FechaHora)
                .Take(100)
                .ToListAsync();
            return View(logs);
        }

        public async Task<IActionResult> Autenticaciones()
        {
            var auths = await _context.Autenticaciones
                .OrderByDescending(a => a.FechaIntento)
                .Take(100)
                .ToListAsync();
            return View(auths);
        }

        public async Task<IActionResult> BloquearUsuario(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddMinutes(20));
                TempData["Success"] = "Usuario bloqueado por 20 minutos";
            }
            return RedirectToAction("Usuarios");
        }

        public async Task<IActionResult> DesbloquearUsuario(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["Success"] = "Usuario desbloqueado";
            }
            return RedirectToAction("Usuarios");
        }
    }

    public class DashboardStats
    {
        public int TotalAlumnos { get; set; }
        public int TotalProyectos { get; set; }
        public int TotalConvocatorias { get; set; }
        public int TotalPostulaciones { get; set; }
        public int PostulacionesPendientes { get; set; }
        public int AlumnosEnRiesgo { get; set; }
        public int TotalTutores { get; set; }
    }
}
