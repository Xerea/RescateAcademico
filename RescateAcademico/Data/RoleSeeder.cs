using Microsoft.AspNetCore.Identity;
using RescateAcademico.Models;

namespace RescateAcademico.Data
{
    public static class RoleSeeder
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, ApplicationDbContext context)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Administrador", "Tutor", "Alumno", "Autoridad" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Admin
            string adminEmail = "admin@ipn.mx";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(adminUser, "Administrador");
            }

            // Tutor
            string tutorEmail = "tutor@ipn.mx";
            var tutorUser = await userManager.FindByEmailAsync(tutorEmail);
            if (tutorUser == null)
            {
                tutorUser = new ApplicationUser { UserName = tutorEmail, Email = tutorEmail, IsActive = true, EmailConfirmed = true };
                var r2 = await userManager.CreateAsync(tutorUser, "Tutor123!");
                if (r2.Succeeded) await userManager.AddToRoleAsync(tutorUser, "Tutor");
            }

            // Alumno
            string alumnoEmail = "alumno@alumno.ipn.mx";
            var alumnoUser = await userManager.FindByEmailAsync(alumnoEmail);
            if (alumnoUser == null)
            {
                alumnoUser = new ApplicationUser { UserName = alumnoEmail, Email = alumnoEmail, IsActive = true, EmailConfirmed = true };
                var r3 = await userManager.CreateAsync(alumnoUser, "Alumno123!");
                if (r3.Succeeded) await userManager.AddToRoleAsync(alumnoUser, "Alumno");
            }

            // Autoridad
            string autoridadEmail = "autoridad@ipn.mx";
            var autoridadUser = await userManager.FindByEmailAsync(autoridadEmail);
            if (autoridadUser == null)
            {
                autoridadUser = new ApplicationUser { UserName = autoridadEmail, Email = autoridadEmail, IsActive = true, EmailConfirmed = true };
                var r4 = await userManager.CreateAsync(autoridadUser, "Autoridad123!");
                if (r4.Succeeded) await userManager.AddToRoleAsync(autoridadUser, "Autoridad");
            }

            // Seed Carreras
            if (!context.Carreras.Any())
            {
                context.Carreras.AddRange(
                    new Carrera { Clave = "INFO", Nombre = "Informática", Semestres = 6 },
                    new Carrera { Clave = "LABQ", Nombre = "Laboratorio Químico", Semestres = 6 },
                    new Carrera { Clave = "ELEC", Nombre = "Electrónica", Semestres = 6 },
                    new Carrera { Clave = "MECA", Nombre = "Mecánica", Semestres = 6 },
                    new Carrera { Clave = "CONT", Nombre = "Contabilidad", Semestres = 6 },
                    new Carrera { Clave = "ADMI", Nombre = "Administración", Semestres = 6 }
                );
                await context.SaveChangesAsync();
            }

            // Seed Ciclos Escolares
            if (!context.CiclosEscolares.Any())
            {
                context.CiclosEscolares.AddRange(
                    new CicloEscolar { Nombre = "2026-A", Periodo = "2026-1", FechaInicio = new DateTime(2026, 1, 6), FechaFin = new DateTime(2026, 6, 30), EsActual = true },
                    new CicloEscolar { Nombre = "2025-B", Periodo = "2025-2", FechaInicio = new DateTime(2025, 8, 1), FechaFin = new DateTime(2025, 12, 15), EsActual = false },
                    new CicloEscolar { Nombre = "2025-A", Periodo = "2025-1", FechaInicio = new DateTime(2025, 1, 6), FechaFin = new DateTime(2025, 6, 30), EsActual = false }
                );
                await context.SaveChangesAsync();
            }

            // Seed Tutor si no existe
            Tutor? tutor = null;
            if (tutorUser != null)
            {
                tutor = context.Tutores.FirstOrDefault(t => t.UserId == tutorUser.Id);
                if (tutor == null)
                {
                    tutor = new Tutor
                    {
                        Nombre = "Profesor",
                        Apellidos = "Tutor Demo",
                        Email = tutorEmail,
                        UserId = tutorUser.Id,
                        Especialidad = "Asesoría Académica",
                        EstaActivo = true
                    };
                    context.Tutores.Add(tutor);
                    await context.SaveChangesAsync();
                }
            }

            // Seed Alumnos de prueba
            if (!context.Alumnos.Any())
            {
                var alumnosDemo = new List<Alumno>
                {
                    new() { Matricula = "2023600001", Nombre = "Juan", Apellidos = "Pérez García", Carrera = "Informática", PromedioGlobal = 9.5m, SemestreActual = 4, RiesgoAcademico = "Verde", CargaAcademicaActual = 5, UserId = alumnoUser?.Id, Correo = "alumno@alumno.ipn.mx", Estatus = "Activo" },
                    new() { Matricula = "2023600002", Nombre = "María", Apellidos = "López Hernández", Carrera = "Informática", PromedioGlobal = 8.2m, SemestreActual = 4, RiesgoAcademico = "Verde", CargaAcademicaActual = 6, Estatus = "Activo" },
                    new() { Matricula = "2023600003", Nombre = "Carlos", Apellidos = "Martínez Ruiz", Carrera = "Electrónica", PromedioGlobal = 6.8m, SemestreActual = 3, RiesgoAcademico = "Amarillo", CargaAcademicaActual = 7, Estatus = "Activo" },
                    new() { Matricula = "2023600004", Nombre = "Ana", Apellidos = "González Díaz", Carrera = "Laboratorio Químico", PromedioGlobal = 5.5m, SemestreActual = 5, RiesgoAcademico = "Rojo", CargaAcademicaActual = 8, Estatus = "Activo" },
                    new() { Matricula = "2023600005", Nombre = "Pedro", Apellidos = "Sánchez Veláquez", Carrera = "Mecánica", PromedioGlobal = 7.5m, SemestreActual = 2, RiesgoAcademico = "Verde", CargaAcademicaActual = 5, Estatus = "Activo" },
                    new() { Matricula = "2023600006", Nombre = "Sofía", Apellidos = "Ramírez Flores", Carrera = "Contabilidad", PromedioGlobal = 8.8m, SemestreActual = 6, RiesgoAcademico = "Verde", CargaAcademicaActual = 4, Estatus = "Activo" },
                    new() { Matricula = "2023600007", Nombre = "Luis", Apellidos = "Hernández Morales", Carrera = "Administración", PromedioGlobal = 6.2m, SemestreActual = 4, RiesgoAcademico = "Amarillo", CargaAcademicaActual = 6, Estatus = "Activo" },
                    new() { Matricula = "2023600008", Nombre = "Laura", Apellidos = "Díaz Castillo", Carrera = "Informática", PromedioGlobal = 9.1m, SemestreActual = 3, RiesgoAcademico = "Verde", CargaAcademicaActual = 5, Estatus = "Activo" }
                };

                context.Alumnos.AddRange(alumnosDemo);
                await context.SaveChangesAsync();

                // Asignar tutor a alumnos
                if (tutor != null)
                {
                    foreach (var alumno in alumnosDemo.Take(5))
                    {
                        context.AsignacionesTutor.Add(new AsignacionTutor
                        {
                            TutorId = tutor.Id,
                            AlumnoMatricula = alumno.Matricula,
                            Periodo = "2026-A",
                            EstaActiva = true
                        });
                    }
                    await context.SaveChangesAsync();
                }

                // Seed Materias
                var materias = new List<Materia>
                {
                    new() { Clave = "INF-101", Nombre = "Fundamentos de Programación", Creditos = 8, Semestre = 1, Carrera = "Informática" },
                    new() { Clave = "INF-102", Nombre = "Matemáticas Discretas", Creditos = 6, Semestre = 1, Carrera = "Informática" },
                    new() { Clave = "INF-201", Nombre = "Programación Orientada a Objetos", Creditos = 8, Semestre = 2, Carrera = "Informática" },
                    new() { Clave = "INF-202", Nombre = "Base de Datos I", Creditos = 6, Semestre = 2, Carrera = "Informática" },
                    new() { Clave = "INF-301", Nombre = "Desarrollo Web", Creditos = 8, Semestre = 3, Carrera = "Informática" },
                    new() { Clave = "INF-302", Nombre = "Redes de Computadoras", Creditos = 6, Semestre = 3, Carrera = "Informática" },
                    new() { Clave = "INF-401", Nombre = "Ingeniería de Software", Creditos = 8, Semestre = 4, Carrera = "Informática" },
                    new() { Clave = "INF-402", Nombre = "Inteligencia Artificial", Creditos = 6, Semestre = 4, Carrera = "Informática" }
                };
                context.Materias.AddRange(materias);
                await context.SaveChangesAsync();

                // Seed Calificaciones
                var califData = new List<CalificacionData>
                {
                    new("2023600001", "2025-A", 1, 9.0m, true, 1),
                    new("2023600001", "2025-A", 2, 10.0m, true, 1),
                    new("2023600001", "2025-B", 3, 9.5m, true, 1),
                    new("2023600001", "2025-B", 4, 9.0m, true, 1),
                    new("2023600001", "2026-A", 5, 9.5m, true, 1),
                    new("2023600001", "2026-A", 6, 10.0m, true, 1),

                    new("2023600002", "2025-A", 1, 8.0m, true, 1),
                    new("2023600002", "2025-A", 2, 8.5m, true, 1),
                    new("2023600002", "2025-B", 3, 8.0m, true, 1),
                    new("2023600002", "2025-B", 4, 8.5m, true, 1),
                    new("2023600002", "2026-A", 5, 7.5m, true, 1),
                    new("2023600002", "2026-A", 6, 8.5m, true, 1),

                    new("2023600003", "2025-B", 1, 6.0m, true, 1),
                    new("2023600003", "2025-B", 2, 7.0m, true, 1),
                    new("2023600003", "2026-A", 3, 6.5m, true, 1),
                    new("2023600003", "2026-A", 4, 7.5m, true, 1),

                    new("2023600004", "2025-A", 1, 5.0m, false, 2),
                    new("2023600004", "2025-B", 1, 6.0m, true, 2),
                    new("2023600004", "2025-A", 2, 5.5m, false, 1),
                    new("2023600004", "2026-A", 3, 5.0m, false, 1)
                };

                foreach (var c in califData)
                {
                    context.Calificaciones.Add(new Calificacion
                    {
                        AlumnoMatricula = c.Matricula,
                        MateriaId = c.MateriaId,
                        Periodo = c.Periodo,
                        CicloEscolar = c.Periodo,
                        Valor = c.Valor,
                        Aprobada = c.Aprobada,
                        VecesCursada = c.Veces,
                        Tipo = "Ordinario"
                    });
                }
                await context.SaveChangesAsync();
            }

            // Seed Proyectos
            if (!context.Proyectos.Any())
            {
                context.Proyectos.AddRange(
                    new Proyecto { Titulo = "Laboratorio de Computación", Descripcion = "Apoyo en laboratorio de cómputo", Tipo = "Laboratorio", CupoMaximo = 10, FechaCierre = DateTime.Now.AddMonths(2), EstaActivo = true },
                    new Proyecto { Titulo = "Biblioteca Digital", Descripcion = "Digitalización de acervo bibliotecario", Tipo = "Servicio Social", CupoMaximo = 5, FechaCierre = DateTime.Now.AddMonths(1), EstaActivo = true },
                    new Proyecto { Titulo = "Centro de Cómputo", Descripcion = "Mantenimiento de equipos", Tipo = "Apoyo Académico", CupoMaximo = 8, FechaCierre = DateTime.Now.AddMonths(3), EstaActivo = true }
                );
                await context.SaveChangesAsync();
            }

            // Seed Convocatorias
            if (!context.Convocatorias.Any())
            {
                var proyectos = context.Proyectos.ToList();
                var proyecto1 = proyectos[0];
                var proyecto2 = proyectos.Count > 1 ? proyectos[1] : proyecto1;
                var proyecto3 = proyectos.Count > 2 ? proyectos[2] : proyecto1;
                context.Convocatorias.AddRange(
                    new Convocatoria { Titulo = "Servicio Social en Laboratorios", Descripcion = "Apoyo en laboratorios de cómputo", Tipo = "ServicioSocial", ProyectoId = proyecto1.Id, CupoMaximo = 10, FechaCierre = DateTime.Now.AddDays(30), PromedioMinimo = 7.0m, SemestreMinimo = 3, ValidadaPorAcademia = true, Area = "Ciencias Básicas", EstaActiva = true },
                    new Convocatoria { Titulo = "Investigación en IA", Descripcion = "Proyecto de investigación en inteligencia artificial", Tipo = "Investigacion", ProyectoId = proyecto2.Id, CupoMaximo = 3, FechaCierre = DateTime.Now.AddDays(45), PromedioMinimo = 8.5m, SemestreMinimo = 4, ValidadaPorAcademia = true, Area = "Tecnología", EstaActiva = true },
                    new Convocatoria { Titulo = "Apoyo en Biblioteca", Descripcion = "Digitalización de documentos", Tipo = "ApoyoAcademico", ProyectoId = proyecto3.Id, CupoMaximo = 5, FechaCierre = DateTime.Now.AddDays(20), PromedioMinimo = 6.0m, SemestreMinimo = 2, ValidadaPorAcademia = true, Area = "Humanidades", EstaActiva = true },
                    new Convocatoria { Titulo = "Apoyo a laboratorio de química", Descripcion = "Seguimiento de material y bitácoras de laboratorio", Tipo = "Laboratorio", ProyectoId = proyecto1.Id, CupoMaximo = 4, FechaCierre = DateTime.Now.AddDays(35), PromedioMinimo = 7.5m, SemestreMinimo = 3, ValidadaPorAcademia = true, Area = "Laboratorio", EstaActiva = true },
                    new Convocatoria { Titulo = "Tutoría entre pares", Descripcion = "Apoyo entre estudiantes para regularización", Tipo = "ApoyoAcademico", ProyectoId = proyecto2.Id, CupoMaximo = 6, FechaCierre = DateTime.Now.AddDays(28), PromedioMinimo = 7.0m, SemestreMinimo = 2, ValidadaPorAcademia = true, Area = "Formación", EstaActiva = true }
                );
                await context.SaveChangesAsync();
            }

            if (!context.Postulaciones.Any())
            {
                var alumnos = context.Alumnos.Take(6).ToList();
                var proyectos = context.Proyectos.ToList();
                if (alumnos.Any() && proyectos.Any())
                {
                    var postulaciones = new List<Postulacion>
                    {
                        new() { AlumnoId = alumnos[0].Matricula, ProyectoId = proyectos[0].Id, Estado = "Aceptado", FechaSolicitud = DateTime.Now.AddDays(-12) },
                        new() { AlumnoId = alumnos[1].Matricula, ProyectoId = proyectos[0].Id, Estado = "En Revisión", FechaSolicitud = DateTime.Now.AddDays(-6) },
                        new() { AlumnoId = alumnos[2].Matricula, ProyectoId = proyectos[1].Id, Estado = "Rechazado", FechaSolicitud = DateTime.Now.AddDays(-9) },
                        new() { AlumnoId = alumnos[3].Matricula, ProyectoId = proyectos[2].Id, Estado = "En Revisión", FechaSolicitud = DateTime.Now.AddDays(-4) },
                        new() { AlumnoId = alumnos[4].Matricula, ProyectoId = proyectos[1].Id, Estado = "Aceptado", FechaSolicitud = DateTime.Now.AddDays(-3) }
                    };
                    context.Postulaciones.AddRange(postulaciones);
                    await context.SaveChangesAsync();
                }
            }

            if (!context.Notificaciones.Any())
            {
                var usuarioIds = context.Users.Select(u => u.Id).Take(4).ToList();
                foreach (var userId in usuarioIds)
                {
                    context.Notificaciones.AddRange(
                        new Notificacion
                        {
                            UserId = userId,
                            Titulo = "Bienvenido a Rescate Academico",
                            Mensaje = "Tu cuenta esta lista para usarse en el sistema.",
                            Tipo = "Informacion",
                            FechaCreacion = DateTime.Now.AddDays(-2),
                            Leida = false
                        },
                        new Notificacion
                        {
                            UserId = userId,
                            Titulo = "Actualizacion de estado",
                            Mensaje = "Se actualizo una solicitud relacionada con tus actividades.",
                            Tipo = "Exito",
                            FechaCreacion = DateTime.Now.AddDays(-1),
                            Leida = false
                        }
                    );
                }
                await context.SaveChangesAsync();
            }

            if (!context.BitacoraLogs.Any())
            {
                var adminId = context.Users.Where(u => u.Email == "admin@ipn.mx").Select(u => u.Id).FirstOrDefault() ?? "system-admin";
                var autoridadId = context.Users.Where(u => u.Email == "autoridad@ipn.mx").Select(u => u.Id).FirstOrDefault() ?? "system-autoridad";
                context.BitacoraLogs.AddRange(
                    new BitacoraLog
                    {
                        UsuarioId = adminId,
                        UsuarioEmail = "admin@ipn.mx",
                        Accion = "Crear",
                        TablaAfectada = "Convocatorias",
                        RegistroNuevo = "Convocatoria Servicio Social en Laboratorios",
                        FechaHora = DateTime.Now.AddDays(-5)
                    },
                    new BitacoraLog
                    {
                        UsuarioId = adminId,
                        UsuarioEmail = "admin@ipn.mx",
                        Accion = "Editar",
                        TablaAfectada = "Alumnos",
                        RegistroNuevo = "Actualizacion de riesgo academico",
                        FechaHora = DateTime.Now.AddDays(-3)
                    },
                    new BitacoraLog
                    {
                        UsuarioId = autoridadId,
                        UsuarioEmail = "autoridad@ipn.mx",
                        Accion = "Consulta",
                        TablaAfectada = "Estadisticas",
                        RegistroNuevo = "Reporte institucional generado",
                        FechaHora = DateTime.Now.AddDays(-1)
                    }
                );
                await context.SaveChangesAsync();
            }
        }

        private record CalificacionData(string Matricula, string Periodo, int MateriaId, decimal Valor, bool Aprobada, int Veces);
    }
}
