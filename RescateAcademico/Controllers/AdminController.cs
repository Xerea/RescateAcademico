using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Filters;
using RescateAcademico.Models;
using RescateAcademico.Services;
using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AlertasService _alertasService;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AlertasService alertasService)
        {
            _context = context;
            _userManager = userManager;
            _alertasService = alertasService;
        }

        public IActionResult Index()
        {
            // Admin landing now routes to the global Dashboard,
            // where role-specific stats are rendered.
            return RedirectToAction("Index", "Dashboard");
        }

        public IActionResult Gestion()
        {
            return View();
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearAlumno([Bind("Matricula,Nombre,Apellidos,Carrera,PromedioGlobal,SemestreActual,RiesgoAcademico,CargaAcademicaActual,MateriasReprobadas,EtsPresentados,Recursamientos,Ausencias,ParcialesBajos,Estatus,Correo,GrupoId")] Alumno alumno)
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarAlumno([Bind("Matricula,Nombre,Apellidos,Carrera,PromedioGlobal,SemestreActual,RiesgoAcademico,CargaAcademicaActual,MateriasReprobadas,EtsPresentados,Recursamientos,Ausencias,ParcialesBajos,Estatus,Correo,GrupoId")] Alumno alumno)
        {
            if (ModelState.IsValid)
            {
                _context.Update(alumno);
                await _context.SaveChangesAsync();

                // Re-evaluar riesgo automáticamente
                var resultado = await _alertasService.EvaluarYAlertarAsync(alumno.Matricula);
                if (resultado.Contains("actualizado"))
                {
                    TempData["Success"] = $"Alumno actualizado. {resultado}";
                }
                else
                {
                    TempData["Success"] = "Alumno actualizado exitosamente";
                }

                return RedirectToAction("Alumnos");
            }
            return View(alumno);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog(Accion = "Eliminar", Tabla = "Alumnos")]
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCiclo([Bind("Nombre,Periodo,FechaInicio,FechaFin,EsActual,EstaActivo")] CicloEscolar ciclo)
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCarrera([Bind("Clave,Nombre,Descripcion,Semestres,EstaActiva")] Carrera carrera)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog(Accion = "Cambiar Estado", Tabla = "Carreras")]
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

        public IActionResult Bitacora()
        {
            return RedirectToAction("Index", "Bitacora");
        }

        public async Task<IActionResult> Autenticaciones()
        {
            var auths = await _context.Autenticaciones
                .OrderByDescending(a => a.FechaIntento)
                .Take(100)
                .ToListAsync();
            return View(auths);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog(Accion = "Bloquear", Tabla = "Usuarios")]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog(Accion = "Desbloquear", Tabla = "Usuarios")]
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

        // HU-RA-19: Crear Cuenta Institucional (Admin only)
        public IActionResult CrearCuenta()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog(Accion = "Crear Cuenta", Tabla = "Usuarios")]
        public async Task<IActionResult> CrearCuenta(CrearCuentaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Rol);

                if (model.Rol == "Alumno")
                {
                    var alumno = new Alumno
                    {
                        Matricula = model.Matricula ?? "",
                        Nombre = model.Nombre ?? "",
                        Apellidos = model.Apellidos ?? "",
                        Correo = model.Email,
                        UserId = user.Id,
                        Carrera = model.Carrera ?? "",
                        SemestreActual = model.SemestreActual > 0 ? model.SemestreActual : 1,
                        PromedioGlobal = 0,
                        Estatus = "Activo"
                    };
                    _context.Alumnos.Add(alumno);
                    await _context.SaveChangesAsync();
                }
                else if (model.Rol == "Tutor")
                {
                    var tutor = new Tutor
                    {
                        Nombre = model.Nombre ?? "",
                        Apellidos = model.Apellidos ?? "",
                        Email = model.Email,
                        UserId = user.Id,
                        Especialidad = model.Especialidad ?? "",
                        EstaActivo = true
                    };
                    _context.Tutores.Add(tutor);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Cuenta creada exitosamente para {model.Email} con rol {model.Rol}.";
                return RedirectToAction("Usuarios");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // HU-RA-26/27: Dashboard de Integridad de Datos
        public async Task<IActionResult> IntegridadDatos()
        {
            var problemas = new List<(string Mensaje, string Severidad)>();

            // Usuarios
            var alumnosSinUsuario = await _context.Alumnos.CountAsync(a => string.IsNullOrEmpty(a.UserId));
            if (alumnosSinUsuario > 0)
                problemas.Add(($"{alumnosSinUsuario} alumnos no tienen cuenta de usuario vinculada", "warning"));

            var tutoresSinUsuario = await _context.Tutores.CountAsync(t => string.IsNullOrEmpty(t.UserId));
            if (tutoresSinUsuario > 0)
                problemas.Add(($"{tutoresSinUsuario} tutores no tienen cuenta de usuario vinculada", "warning"));

            // Tutorías
            var asignacionesInactivas = await _context.AsignacionesTutor.CountAsync(a => !a.EstaActiva);
            if (asignacionesInactivas > 0)
                problemas.Add(($"{asignacionesInactivas} asignaciones tutor-alumno están inactivas", "info"));

            var alumnosSinTutor = await _context.Alumnos
                .CountAsync(a => !_context.AsignacionesTutor.Any(at => at.AlumnoMatricula == a.Matricula && at.EstaActiva));
            if (alumnosSinTutor > 0)
                problemas.Add(($"{alumnosSinTutor} alumnos activos no tienen tutor asignado", "warning"));

            // Convocatorias
            var convocatoriasVencidas = await _context.Convocatorias
                .CountAsync(c => c.EstaActiva && c.FechaCierre < DateTime.Now);
            if (convocatoriasVencidas > 0)
                problemas.Add(($"{convocatoriasVencidas} convocatorias activas ya vencieron", "danger"));

            var convocatoriasSinCupo = await _context.Convocatorias
                .CountAsync(c => c.EstaActiva && c.PostulacionesActuales >= c.CupoMaximo);
            if (convocatoriasSinCupo > 0)
                problemas.Add(($"{convocatoriasSinCupo} convocatorias activas sin cupo disponible", "info"));

            // Postulaciones
            var postulacionesSinAlumno = await _context.Postulaciones
                .CountAsync(p => !_context.Alumnos.Any(a => a.Matricula == p.AlumnoId));
            if (postulacionesSinAlumno > 0)
                problemas.Add(($"{postulacionesSinAlumno} postulaciones huérfanas (sin alumno)", "danger"));

            var postulacionesSinProyecto = await _context.Postulaciones
                .CountAsync(p => !_context.Proyectos.Any(pr => pr.Id == p.ProyectoId));
            if (postulacionesSinProyecto > 0)
                problemas.Add(($"{postulacionesSinProyecto} postulaciones huérfanas (sin proyecto)", "danger"));

            // Riesgo académico
            var alumnosSinRiesgo = await _context.Alumnos.CountAsync(a => string.IsNullOrEmpty(a.RiesgoAcademico));
            if (alumnosSinRiesgo > 0)
                problemas.Add(($"{alumnosSinRiesgo} alumnos no tienen evaluación de riesgo", "warning"));

            var alumnosRojoSinPlan = await _context.Alumnos
                .CountAsync(a => a.RiesgoAcademico == "Rojo" && !_context.PlanesMejora.Any(p => p.AlumnoMatricula == a.Matricula && p.Estado == "Activo"));
            if (alumnosRojoSinPlan > 0)
                problemas.Add(($"{alumnosRojoSinPlan} alumnos en riesgo crítico sin plan de mejora activo", "danger"));

            // Calificaciones
            var califSinMateria = await _context.Calificaciones
                .CountAsync(c => !_context.Materias.Any(m => m.Id == c.MateriaId));
            if (califSinMateria > 0)
                problemas.Add(($"{califSinMateria} calificaciones sin materia asociada", "danger"));

            // Duplicados
            var matriculasDuplicadas = await _context.Alumnos
                .GroupBy(a => a.Matricula)
                .CountAsync(g => g.Count() > 1);
            if (matriculasDuplicadas > 0)
                problemas.Add(($"{matriculasDuplicadas} matrículas duplicadas detectadas", "danger"));

            // Nuevos checks IA de integridad
            var alumnosSinCalif = await _context.Alumnos
                .CountAsync(a => !_context.Calificaciones.Any(c => c.AlumnoMatricula == a.Matricula));
            if (alumnosSinCalif > 0)
                problemas.Add(($"{alumnosSinCalif} alumnos sin calificaciones registradas", "warning"));

            var profesoresSinGrupo = await _context.Tutores
                .CountAsync(t => t.EstaActivo && !_context.Grupos.Any(g => g.ProfesorId == t.Id));
            if (profesoresSinGrupo > 0)
                problemas.Add(($"{profesoresSinGrupo} profesores activos sin grupo asignado", "warning"));

            var gruposVacios = await _context.Grupos
                .CountAsync(g => !_context.Alumnos.Any(a => a.GrupoId == g.Id));
            if (gruposVacios > 0)
                problemas.Add(($"{gruposVacios} grupos sin estudiantes", "info"));

            var inconsistenciaRiesgo = await _context.Alumnos
                .CountAsync(a =>
                    (a.PromedioGlobal >= 8 && a.RiesgoAcademico == "Rojo") ||
                    (a.PromedioGlobal < 6 && a.RiesgoAcademico == "Verde"));
            if (inconsistenciaRiesgo > 0)
                problemas.Add(($"{inconsistenciaRiesgo} alumnos con inconsistencia entre promedio y nivel de riesgo", "danger"));

            ViewBag.Problemas = problemas;
            ViewBag.TotalAlumnos = await _context.Alumnos.CountAsync();
            ViewBag.TotalTutores = await _context.Tutores.CountAsync();
            ViewBag.TotalProyectos = await _context.Proyectos.CountAsync();
            ViewBag.TotalConvocatorias = await _context.Convocatorias.CountAsync();
            ViewBag.TotalPostulaciones = await _context.Postulaciones.CountAsync();
            ViewBag.TotalCalificaciones = await _context.Calificaciones.CountAsync();
            ViewBag.TotalIntervenciones = await _context.IntervencionesTutoria.CountAsync();
            ViewBag.TotalPlanes = await _context.PlanesMejora.CountAsync();
            ViewBag.AlumnosSinTutor = alumnosSinTutor;
            ViewBag.AlumnosRojoSinPlan = alumnosRojoSinPlan;
            ViewBag.AlumnosSinRiesgo = alumnosSinRiesgo;

            return View();
        }

        // HU-RA-23: Evaluación automática de riesgo académico
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog(Accion = "Evaluar Riesgos", Tabla = "Alumnos")]
        public async Task<IActionResult> EvaluarRiesgos()
        {
            var (evaluados, cambios, detalles) = await _alertasService.EvaluarTodosAsync();
            TempData["Success"] = $"Evaluación completada: {evaluados} alumnos evaluados, {cambios} cambios de riesgo detectados.";
            if (detalles.Any())
            {
                TempData["Detalles"] = string.Join("; ", detalles.Take(5));
            }
            return RedirectToAction("Index");
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

        // Tutor-specific stats
        public int TutorAssignedStudents { get; set; }
        public int TutorStudentsAtRisk { get; set; }
        public int TutorRecentInterventions { get; set; }

        // Student-specific stats
        public decimal AlumnoPromedio { get; set; }
        public string AlumnoRiesgo { get; set; } = "Verde";
        public int AlumnoSemestre { get; set; }
        public int AlumnoPostulaciones { get; set; }
        public string AlumnoNombre { get; set; } = string.Empty;
        public string AlumnoMatricula { get; set; } = string.Empty;
        public string? AlumnoCarrera { get; set; }
        public string? AlumnoGrupo { get; set; }
        public int AlumnoMateriasReprobadas { get; set; }
        public int AlumnoAusencias { get; set; }
        public int AlumnoConvocatoriasDisponibles { get; set; }
        public int AlumnoPostulacionesPendientes { get; set; }
        public int AlumnoPostulacionesAceptadas { get; set; }

        // Chart data for dashboard
        public int RiesgoVerde { get; set; }
        public int RiesgoAmarillo { get; set; }
        public int RiesgoRojo { get; set; }
        public List<(string Carrera, int Count)> AlumnosPorCarrera { get; set; } = new();
        public List<(int Semestre, int Count)> AlumnosPorSemestre { get; set; } = new();

        // Autoridad-specific stats
        public decimal PromedioGeneral { get; set; }
        public int AlumnosPromedioCritico { get; set; }
        public int AlumnosPromedioObservacion { get; set; }
        public int AlumnosPromedioSolido { get; set; }
        public int AlumnosConMateriasReprobadas { get; set; }
        public int AlumnosConAusencias { get; set; }
        public int ConvocatoriasProximasACerrar { get; set; }
        public int IntervencionesRecientes { get; set; }
        public int TotalGrupos { get; set; }
    }

    public class CrearCuentaViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Rol { get; set; } = "Alumno";

        public string? Matricula { get; set; }
        public string? Nombre { get; set; }
        public string? Apellidos { get; set; }
        public string? Carrera { get; set; }
        public int SemestreActual { get; set; } = 1;
        public string? Especialidad { get; set; }
    }
}
