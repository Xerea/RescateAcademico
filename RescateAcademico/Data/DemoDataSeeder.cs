using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;
using RescateAcademico.Services;
using System.Globalization;
using System.Text;

namespace RescateAcademico.Seeders
{
    public static class DemoDataSeeder
    {
        private static readonly Random _rng = new(42);
        private static readonly HashSet<string> _emailsUsados = new();
        private static int _emailCounter = 1;

        // ===== DATOS CECyT 13 =====
        private static readonly string[] NombresHombre = new[] {
            "Juan","Carlos","Luis","Pedro","Jorge","Miguel","Fernando","Andrés","Daniel","Alejandro",
            "Roberto","Ricardo","Sergio","Eduardo","Manuel","Francisco","Javier","Antonio","Raúl","Hugo",
            "Diego","Gabriel","Martín","Emiliano","Mateo","Leonardo","Sebastián","Tomás","Julio","Armando",
            "Ignacio","Oscar","Arturo","Gerardo","Ernesto","Mario","Alberto","Guillermo","Víctor","Enrique"
        };
        private static readonly string[] NombresMujer = new[] {
            "María","Ana","Laura","Sofía","Isabella","Valentina","Camila","Daniela","Victoria","Natalia",
            "Alejandra","Fernanda","Gabriela","Andrea","Paula","Diana","Carmen","Patricia","Rosa","Elena",
            "Lucía","Mariana","Regina","Ximena","Renata","Julia","Antonella","Emily","Melanie","Vanessa",
            "Guadalupe","Araceli","Leticia","Norma","Silvia","Cecilia","Rocío","Teresa","Irma","Lorena"
        };
        private static readonly string[] Apellidos = new[] {
            "García","López","Martínez","Hernández","González","Pérez","Sánchez","Ramírez","Flores","Torres",
            "Rivera","Ruiz","Díaz","Moreno","Jiménez","Muñoz","Castillo","Ortega","Vargas","Cruz",
            "Reyes","Morales","Mendoza","Herrera","Aguirre","Vega","Rojas","Silva","Delgado","Guerrero",
            "Soto","Estrada","Medina","Espinoza","Contreras","Cortés","Navarro","Sandoval","Campos","Ibarra",
            "Álvarez","Domínguez","Peña","Valdez","Santos","Miranda","Cabrera","Fuentes","Mejía","Arias",
            "Padilla","Esparza","Zúñiga","Castro","Franco","Villanueva","Ayala","Ramos","Mora","Romero"
        };

        private static readonly string[] Carreras = new[] {
            "Administración","Contabilidad","Gastronomía","Informática","Seguridad Informática","Turismo"
        };
        private static readonly string[] CarreraClaves = new[] { "A","C","G","I","S","T" };
        private static readonly string[] Turnos = new[] { "Matutino","Vespertino" };
        private static readonly string[] TurnoAbrevs = new[] { "IM","IV" };

        // Materias comunes (Semestres 1 y 2)
        private static readonly List<MateriaSeed> MateriasComunes = new()
        {
            new("0101","ÁLGEBRA",1,5.62m), new("0102","FILOSOFÍA I",1,3.37m), new("0103","COMPUTACIÓN BÁSICA I",1,4.5m),
            new("0104","INGLÉS I",1,5.62m), new("0105","EXPRESIÓN ORAL Y ESCRITA I",1,4.5m), new("0106","DESARROLLO DE HABILIDADES DEL PENSAMIENTO",1,3.37m),
            new("0107","HISTORIA DE MÉXICO CONTEMPORÁNEO I",1,3.37m), new("0108","DESARROLLO PERSONAL",1,4.5m), new("0109","ORIENTACIÓN JUVENIL Y PROFESIONAL I",1,0m),
            new("0201","GEOMETRÍA Y TRIGONOMETRÍA",2,5.62m), new("0202","FILOSOFÍA II",2,3.37m), new("0203","COMPUTACIÓN BÁSICA II",2,4.5m),
            new("0204","INGLÉS II",2,5.62m), new("0205","EXPRESIÓN ORAL Y ESCRITA II",2,4.5m), new("0206","BIOLOGÍA BÁSICA",2,5.62m),
            new("0207","HISTORIA DE MÉXICO CONTEMPORÁNEO II",2,3.37m), new("0208","ORIENTACIÓN JUVENIL Y PROFESIONAL II",2,0m), new("0209","COMUNICACIÓN Y LIDERAZGO",2,3.37m),
        };

        // Materias por carrera (semestres 3-6) - representativas obligatorias
        private static readonly Dictionary<string, List<MateriaSeed>> MateriasPorCarrera = new()
        {
            ["A"] = new() {
                new("A301","GEOMETRÍA ANALÍTICA",3,5.62m), new("A302","FÍSICA I",3,5.62m), new("A303","QUÍMICA I",3,4.5m), new("A304","INGLÉS III",3,6.75m), new("A305","COMUNICACIÓN CIENTÍFICA",3,3.37m),
                new("A306","CONTABILIDAD I",3,5.62m), new("A307","ENTORNO SOCIOECONÓMICO DE MÉXICO",3,3.37m), new("A308","CÁLCULOS FINANCIEROS I",3,4.5m), new("A309","ADMINISTRACIÓN",3,4.5m),
                new("A401","CÁLCULO DIFERENCIAL",4,5.62m), new("A402","FÍSICA II",4,5.62m), new("A403","QUÍMICA II",4,4.5m), new("A404","INGLÉS IV",4,6.75m), new("A405","DERECHO",4,3.37m),
                new("A406","CONTABILIDAD II",4,5.62m), new("A407","CÁLCULOS FINANCIEROS II",4,3.37m), new("A408","ADMINISTRACIÓN DE CAPITAL HUMANO",4,3.37m),
                new("A501","CÁLCULO INTEGRAL",5,5.62m), new("A502","MICROECONOMÍA",5,4.5m), new("A503","INGLÉS V",5,6.75m), new("A504","DERECHO MERCANTIL",5,3.37m), new("A505","CONTABILIDAD III",5,5.62m),
                new("A506","ORIENTACIÓN JUVENIL Y PROFESIONAL III",5,0m), new("A507","ADMINISTRACIÓN DE SUELDOS Y SALARIOS",5,4.5m), new("A508","DISEÑO ORGANIZACIONAL",5,4.5m),
                new("A601","PROBABILIDAD Y ESTADÍSTICA",6,5.62m), new("A602","MACROECONOMÍA",6,4.5m), new("A603","INGLÉS VI",6,6.75m), new("A604","ORIENTACIÓN JUVENIL Y PROFESIONAL IV",6,0m),
                new("A605","PLAN DE NEGOCIOS",6,5.62m), new("A606","SISTEMAS DE CALIDAD",6,4.5m), new("A607","NOCIONES DE FINANZAS",6,4.5m),
            },
            ["C"] = new() {
                new("C301","GEOMETRÍA ANALÍTICA",3,5.62m), new("C302","FÍSICA I",3,5.62m), new("C303","QUÍMICA I",3,4.5m), new("C304","INGLÉS III",3,6.75m), new("C305","COMUNICACIÓN CIENTÍFICA",3,3.37m),
                new("C306","CONTABILIDAD I",3,5.62m), new("C307","ENTORNO SOCIOECONÓMICO DE MÉXICO",3,3.37m), new("C308","CÁLCULOS FINANCIEROS I",3,4.5m), new("C309","LEGISLACIÓN FISCAL",3,4.5m),
                new("C401","CÁLCULO DIFERENCIAL",4,5.62m), new("C402","FÍSICA II",4,5.62m), new("C403","QUÍMICA II",4,4.5m), new("C404","INGLÉS IV",4,6.75m), new("C405","DERECHO",4,3.37m),
                new("C406","CONTABILIDAD II",4,5.62m), new("C407","CÁLCULOS FINANCIEROS II",4,3.37m), new("C408","LEGISLACIÓN FISCAL PERSONAS FÍSICAS",4,4.5m),
                new("C501","CÁLCULO INTEGRAL",5,5.62m), new("C502","MICROECONOMÍA",5,4.5m), new("C503","INGLÉS V",5,6.75m), new("C504","DERECHO MERCANTIL",5,3.37m), new("C505","CONTABILIDAD III",5,5.62m),
                new("C506","ORIENTACIÓN JUVENIL Y PROFESIONAL III",5,0m), new("C507","LEGISLACIÓN FISCAL PERSONAS MORALES",5,4.5m), new("C508","NOCIONES DE FINANZAS",5,4.5m),
                new("C601","PROBABILIDAD Y ESTADÍSTICA",6,5.62m), new("C602","MACROECONOMÍA",6,4.5m), new("C603","INGLÉS VI",6,6.75m), new("C604","ORIENTACIÓN JUVENIL Y PROFESIONAL IV",6,0m),
                new("C605","SEGURIDAD SOCIAL",6,4.5m), new("C606","NÓMINAS",6,4.5m), new("C607","NOCIONES DE AUDITORÍA",6,4.5m),
            },
            ["G"] = new() {
                new("G301","GEOMETRÍA ANALÍTICA",3,5.62m), new("G302","FÍSICA I",3,5.62m), new("G303","QUÍMICA I",3,4.5m), new("G304","INGLÉS III",3,6.75m), new("G305","COMUNICACIÓN CIENTÍFICA",3,3.37m),
                new("G306","CONTABILIDAD I",3,5.62m), new("G307","ENTORNO SOCIOECONÓMICO DE MÉXICO",3,3.37m), new("G308","CÁLCULOS FINANCIEROS I",3,4.5m), new("G309","TÉCNICAS CULINARIAS",3,4.5m),
                new("G401","CÁLCULO DIFERENCIAL",4,5.62m), new("G402","FÍSICA II",4,5.62m), new("G403","QUÍMICA II",4,4.5m), new("G404","INGLÉS IV",4,6.75m), new("G405","DERECHO",4,3.37m),
                new("G406","CONTABILIDAD II",4,5.62m), new("G407","CÁLCULOS FINANCIEROS II",4,3.37m), new("G408","CERTIFICACIONES NORMATIVAS, SEGURIDAD E HIGIENE",4,3.37m),
                new("G501","CÁLCULO INTEGRAL",5,5.62m), new("G502","MICROECONOMÍA",5,4.5m), new("G503","INGLÉS V",5,6.75m), new("G504","DERECHO MERCANTIL",5,3.37m), new("G505","CONTABILIDAD III",5,5.62m),
                new("G506","ORIENTACIÓN JUVENIL Y PROFESIONAL III",5,0m), new("G507","ADMINISTRACIÓN GASTRONÓMICA FINANCIERA",5,4.5m), new("G508","GESTIÓN DE EVENTOS Y BANQUETES",5,3.37m),
                new("G601","PROBABILIDAD Y ESTADÍSTICA",6,5.62m), new("G602","MACROECONOMÍA",6,4m), new("G603","INGLÉS VI",6,6m), new("G604","ORIENTACIÓN JUVENIL Y PROFESIONAL IV",6,0m),
                new("G605","SERVICIOS EN RESTAURANTE Y BAR",6,4.5m), new("G606","GASTRONOMÍA INTERNACIONAL II",6,4.5m), new("G607","PLAN DE NEGOCIOS DE ALIMENTOS Y BEBIDAS",6,4.5m),
            },
            ["I"] = new() {
                new("I301","GEOMETRÍA ANALÍTICA",3,5.62m), new("I302","FÍSICA I",3,5.62m), new("I303","QUÍMICA I",3,4.5m), new("I304","INGLÉS III",3,6.75m), new("I305","COMUNICACIÓN CIENTÍFICA",3,3.37m),
                new("I306","CONTABILIDAD I",3,5.62m), new("I307","ENTORNO SOCIOECONÓMICO DE MÉXICO",3,3.37m), new("I308","CÁLCULOS FINANCIEROS I",3,4.5m), new("I309","HERRAMIENTAS DE PROGRAMACIÓN",3,4.5m), new("I310","ENSAMBLADO Y MANTENIMIENTO DE PCS",3,3.37m),
                new("I401","CÁLCULO DIFERENCIAL",4,5.62m), new("I402","FÍSICA II",4,5.62m), new("I403","QUÍMICA II",4,4.5m), new("I404","INGLÉS IV",4,6.75m), new("I405","DERECHO",4,3.37m),
                new("I406","CONTABILIDAD II",4,5.62m), new("I407","CÁLCULOS FINANCIEROS II",4,3.37m), new("I408","PROGRAMACIÓN ORIENTADA A OBJETOS",4,6.75m), new("I409","TELEINFORMÁTICA",4,4.5m),
                new("I501","CÁLCULO INTEGRAL",5,5.62m), new("I502","MICROECONOMÍA",5,4.5m), new("I503","INGLÉS V",5,6.75m), new("I504","DERECHO MERCANTIL",5,3.37m), new("I505","CONTABILIDAD III",5,5.62m),
                new("I506","ORIENTACIÓN JUVENIL Y PROFESIONAL III",5,0m), new("I507","PROGRAMACIÓN DE DISPOSITIVOS MÓVILES",5,6.75m), new("I508","BASE DE DATOS",5,6.75m), new("I509","MODELADO DE SISTEMAS",5,3.37m),
                new("I601","PROBABILIDAD Y ESTADÍSTICA",6,5.62m), new("I602","MACROECONOMÍA",6,4.5m), new("I603","INGLÉS VI",6,6.75m), new("I604","ORIENTACIÓN JUVENIL Y PROFESIONAL IV",6,0m),
                new("I605","PRODUCCIÓN MULTIMEDIA Y AMBIENTES VIRTUALES",6,5.62m), new("I606","DESARROLLO WEB",6,5.62m), new("I607","PROGRAMACIÓN AVANZADA",6,6.75m),
            },
            ["S"] = new() {
                new("S301","GEOMETRÍA ANALÍTICA",3,5.62m), new("S302","FÍSICA I",3,5.62m), new("S303","QUÍMICA I",3,4.5m), new("S304","INGLÉS III",3,6.75m), new("S305","COMUNICACIÓN CIENTÍFICA",3,3.37m),
                new("S306","CONTABILIDAD I",3,5.62m), new("S307","ENTORNO SOCIOECONÓMICO DE MÉXICO",3,3.37m), new("S308","CÁLCULOS FINANCIEROS I",3,4.5m), new("S309","SISTEMAS OPERATIVOS",3,3.37m), new("S310","CIBERSEGURIDAD Y SOCIEDAD",3,2.25m),
                new("S401","CÁLCULO DIFERENCIAL",4,5.62m), new("S402","FÍSICA II",4,5.62m), new("S403","QUÍMICA II",4,4.5m), new("S404","INGLÉS IV",4,6.75m), new("S405","DERECHO",4,3.37m),
                new("S406","CONTABILIDAD II",4,5.62m), new("S407","CÁLCULOS FINANCIEROS II",4,3.37m), new("S408","REDES DE COMPUTADORAS",4,4.5m), new("S409","GOBERNANZA DE TECNOLOGÍAS DE LA INFORMACIÓN",4,4.5m),
                new("S501","CÁLCULO INTEGRAL",5,5.62m), new("S502","MICROECONOMÍA",5,4.5m), new("S503","INGLÉS V",5,6.75m), new("S504","DERECHO MERCANTIL",5,3.37m), new("S505","CONTABILIDAD III",5,5.62m),
                new("S506","ORIENTACIÓN JUVENIL Y PROFESIONAL III",5,0m), new("S507","MODELOS DE SEGURIDAD DE LA INFORMACIÓN",5,4.5m), new("S508","SEGURIDAD EN REDES",5,4.5m), new("S509","SISTEMAS DE BASES DE DATOS",5,4.5m),
                new("S601","PROBABILIDAD Y ESTADÍSTICA",6,5.62m), new("S602","MACROECONOMÍA",6,4m), new("S603","INGLÉS VI",6,6m), new("S604","ORIENTACIÓN JUVENIL Y PROFESIONAL IV",6,0m),
                new("S605","SOFTWARE MALICIOSO",6,5.62m), new("S606","AUDITORÍA INFORMÁTICA",6,6.75m), new("S607","SISTEMAS DE INFORMACIÓN SEGUROS",6,3.37m),
            },
            ["T"] = new() {
                new("T301","GEOMETRÍA ANALÍTICA",3,5.62m), new("T302","FÍSICA I",3,5.62m), new("T303","QUÍMICA I",3,4.5m), new("T304","INGLÉS III",3,6.75m), new("T305","COMUNICACIÓN CIENTÍFICA",3,3.37m),
                new("T306","CONTABILIDAD I",3,5.62m), new("T307","ENTORNO SOCIOECONÓMICO DE MÉXICO",3,3.37m), new("T308","CÁLCULOS FINANCIEROS I",3,4.5m), new("T309","PRINCIPIOS CULINARIOS",3,4.5m), new("T310","ADMINISTRACIÓN Y TURISMO",3,3.37m),
                new("T401","CÁLCULO DIFERENCIAL",4,5.62m), new("T402","FÍSICA II",4,5.62m), new("T403","QUÍMICA II",4,4.5m), new("T404","INGLÉS IV",4,6.75m), new("T405","DERECHO",4,3.37m),
                new("T406","CÁLCULOS FINANCIEROS II",4,3.37m), new("T407","COCINAS DEL MUNDO",4,4.5m), new("T408","OPERACIÓN HOTELERA",4,3.37m), new("T409","INGLÉS TURÍSTICO I",4,3.37m),
                new("T501","CÁLCULO INTEGRAL",5,5.62m), new("T502","MICROECONOMÍA",5,4.5m), new("T503","INGLÉS V",5,6.75m), new("T504","DERECHO MERCANTIL",5,3.37m), new("T505","CONTABILIDAD III",5,5.62m),
                new("T506","ORIENTACIÓN JUVENIL Y PROFESIONAL III",5,0m), new("T507","ADMINISTRACIÓN DE AGENCIAS DE VIAJES",5,3.37m), new("T508","PATRIMONIO TURÍSTICO NACIONAL",5,3.37m), new("T509","ADMINISTRACIÓN HOTELERA",5,3.37m),
                new("T601","PROBABILIDAD Y ESTADÍSTICA",6,5.62m), new("T602","MACROECONOMÍA",6,4.5m), new("T603","INGLÉS VI",6,6.75m), new("T604","ORIENTACIÓN JUVENIL Y PROFESIONAL IV",6,0m),
                new("T605","PROYECTOS TURÍSTICOS",6,5.62m), new("T606","GLOBALIZADORES DE AGENCIAS DE VIAJES",6,4.5m), new("T607","PATRIMONIO TURÍSTICO INTERNACIONAL",6,4.5m),
            },
        };

        private record MateriaSeed(string Clave, string Nombre, int Semestre, decimal Creditos);

        public static async Task SeedAsync(IServiceProvider serviceProvider, ApplicationDbContext context)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Guard: skip only if a substantial amount of student data already exists.
            // This prevents re-seeding on a healthy database but allows recovery
            // from a partial/broken seed (e.g., old RoleSeeder left only 8 students).
            var existingStudentCount = await context.Alumnos.CountAsync();
            if (existingStudentCount >= 50)
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Database already contains {Count} students. Skipping DemoDataSeeder.", existingStudentCount);
                return;
            }

            var alertas = new AlertasService(context);

            await SeedRolesAsync(roleManager);
            var adminUser = await EnsureUserAsync(userManager, "admin@ipn.mx", "Admin123!", "Administrador");
            var autoridadUser = await EnsureUserAsync(userManager, "autoridad@ipn.mx", "Autoridad123!", "Autoridad");

            await SeedCarrerasAsync(context);
            await SeedCiclosAsync(context);
            var profesores = await SeedProfesoresAsync(userManager, context);
            var grupos = await SeedGruposAsync(context, profesores);
            var alumnos = await SeedAlumnosAsync(userManager, context, grupos);
            await SeedMateriasAsync(context);
            await SeedCalificacionesAsync(context, alumnos);
            await SeedAsignacionesTutorAsync(context, profesores, alumnos);
            await SeedProyectosAsync(context);
            await SeedConvocatoriasAsync(context);
            await SeedPostulacionesAsync(context, alumnos);
            await SeedIntervencionesAsync(context, profesores, alumnos);
            await SeedPlanesMejoraAsync(context, profesores, alumnos);
            await SeedNotificacionesAsync(context, alumnos, profesores, adminUser.Id, autoridadUser.Id);
            await SeedBitacoraAsync(context, adminUser.Id, autoridadUser.Id);
            await SeedSugerenciasIAAsync(context, alumnos);
            await SeedDictamenesAsync(context, alumnos);
            await SeedCosecoviAsync(context, alumnos);

            foreach (var alumno in alumnos)
            {
                await alertas.EvaluarYAlertarAsync(alumno.Matricula);
            }

            await SeedPrediccionesDesercionAsync(context, alumnos);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in new[] { "Administrador", "Tutor", "Alumno", "Autoridad" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> um, string email, string password, string role)
        {
            var user = await um.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await um.CreateAsync(user, password);
                if (result.Succeeded) await um.AddToRoleAsync(user, role);
            }
            return user;
        }

        private static async Task SeedCarrerasAsync(ApplicationDbContext context)
        {
            if (await context.Carreras.AnyAsync()) return;
            var carreras = Carreras.Select((c, i) => new Carrera
            {
                Clave = CarreraClaves[i],
                Nombre = c,
                Semestres = 6,
                EstaActiva = true
            }).ToList();
            context.Carreras.AddRange(carreras);
            await context.SaveChangesAsync();
        }

        private static async Task SeedCiclosAsync(ApplicationDbContext context)
        {
            if (await context.CiclosEscolares.AnyAsync()) return;
            context.CiclosEscolares.AddRange(
                new CicloEscolar { Nombre = "2025-A", Periodo = "2025-1", FechaInicio = new DateTime(2025, 1, 6), FechaFin = new DateTime(2025, 6, 30), EsActual = false, EstaActivo = true },
                new CicloEscolar { Nombre = "2025-B", Periodo = "2025-2", FechaInicio = new DateTime(2025, 8, 1), FechaFin = new DateTime(2025, 12, 15), EsActual = false, EstaActivo = true },
                new CicloEscolar { Nombre = "2026-A", Periodo = "2026-1", FechaInicio = new DateTime(2026, 1, 6), FechaFin = new DateTime(2026, 6, 30), EsActual = true, EstaActivo = true }
            );
            await context.SaveChangesAsync();
        }

        private static async Task<List<Tutor>> SeedProfesoresAsync(UserManager<ApplicationUser> um, ApplicationDbContext context)
        {
            if (await context.Tutores.AnyAsync()) return await context.Tutores.ToListAsync();
            var profesores = new List<Tutor>();
            var especialidades = new[] { "Matemáticas", "Programación", "Física", "Química", "Orientación Educativa", "Psicología", "Administración", "Ingeniería", "Idiomas", "Ciencias Sociales", "Contabilidad", "Gastronomía", "Turismo", "Seguridad Informática", "Desarrollo Web", "Bases de Datos", "Redes", "Derecho", "Economía", "Estadística" };

            for (int i = 0; i < 12; i++)
            {
                var email = $"profesor{i + 1}@ipn.mx";
                var user = await EnsureUserAsync(um, email, "Demo123!", "Tutor");
                var esHombre = _rng.NextDouble() > 0.4;
                var nombre = esHombre ? NombresHombre[_rng.Next(NombresHombre.Length)] : NombresMujer[_rng.Next(NombresMujer.Length)];
                var ap1 = Apellidos[_rng.Next(Apellidos.Length)];
                var ap2 = Apellidos[_rng.Next(Apellidos.Length)];
                profesores.Add(new Tutor
                {
                    Nombre = nombre,
                    Apellidos = $"{ap1} {ap2}",
                    Email = email,
                    UserId = user.Id,
                    Especialidad = especialidades[i],
                    EstaActivo = true,
                    Telefono = $"55{_rng.Next(1000, 9999):D4}{_rng.Next(1000, 9999):D4}"
                });
            }
            context.Tutores.AddRange(profesores);
            await context.SaveChangesAsync();
            return profesores;
        }

        private static async Task<List<Grupo>> SeedGruposAsync(ApplicationDbContext context, List<Tutor> profesores)
        {
            if (await context.Grupos.AnyAsync()) return await context.Grupos.ToListAsync();
            var grupos = new List<Grupo>();
            var config = new[]
            {
                (3, "Informática", "Matutino", 1),
                (3, "Contabilidad", "Matutino", 2),
                (3, "Administración", "Vespertino", 1),
                (3, "Informática", "Vespertino", 2),
                (4, "Seguridad Informática", "Matutino", 1),
                (4, "Turismo", "Vespertino", 1),
                (4, "Gastronomía", "Vespertino", 2),
                (5, "Informática", "Vespertino", 1),
                (5, "Administración", "Vespertino", 2),
                (6, "Informática", "Vespertino", 1),
            };

            int profIdx = 0;
            foreach (var (sem, carrera, turno, num) in config)
            {
                var turnoAbrev = turno == "Matutino" ? "IM" : "IV";
                var clave = $"{sem}{turnoAbrev}{num}";
                var carreraClave = CarreraClaves[Array.IndexOf(Carreras, carrera)];
                grupos.Add(new Grupo
                {
                    Clave = clave,
                    Carrera = carrera,
                    Semestre = sem,
                    Turno = turno,
                    NumeroGrupo = num,
                    ProfesorId = profesores[profIdx % profesores.Count].Id
                });
                profIdx++;
            }
            context.Grupos.AddRange(grupos);
            await context.SaveChangesAsync();
            return grupos;
        }

        private static string GenerarEmailIPN(string nombre, string paterno, string materno, int anio)
        {
            var sb = new StringBuilder();
            sb.Append(char.ToLowerInvariant(nombre[0]));
            sb.Append(RemoveDiacritics(paterno.ToLowerInvariant()));
            sb.Append(char.ToLowerInvariant(materno[0]));
            sb.Append((anio % 100).ToString("D2"));
            sb.Append("00");
            var baseEmail = sb.ToString();
            var email = $"{baseEmail}@alumno.ipn.mx";
            if (!_emailsUsados.Add(email))
            {
                email = $"{baseEmail}{_emailCounter++}@alumno.ipn.mx";
                _emailsUsados.Add(email);
            }
            return email;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static async Task<List<Alumno>> SeedAlumnosAsync(UserManager<ApplicationUser> um, ApplicationDbContext context, List<Grupo> grupos)
        {
            if (await context.Alumnos.AnyAsync()) return await context.Alumnos.ToListAsync();
            var alumnos = new List<Alumno>();
            int seq = 1;

            foreach (var grupo in grupos)
            {
                int cantidad = grupo.Semestre <= 3 ? 36 : grupo.Semestre == 4 ? 30 : grupo.Semestre == 5 ? 25 : 24;
                for (int i = 0; i < cantidad; i++)
                {
                    var esHombre = _rng.NextDouble() > 0.45;
                    var nombre = esHombre ? NombresHombre[_rng.Next(NombresHombre.Length)] : NombresMujer[_rng.Next(NombresMujer.Length)];
                    var ap1 = Apellidos[_rng.Next(Apellidos.Length)];
                    var ap2 = Apellidos[_rng.Next(Apellidos.Length)];
                    var anioIngreso = 2026 - grupo.Semestre + 1;
                    var email = GenerarEmailIPN(nombre, ap1, ap2, anioIngreso);
                    var boleta = $"{anioIngreso}{seq:D6}";
                    seq++;

                    var perfil = GenerarPerfilAcademico();
                    var user = await EnsureUserAsync(um, email, "Demo123!", "Alumno");

                    alumnos.Add(new Alumno
                    {
                        Matricula = boleta,
                        Nombre = nombre,
                        Apellidos = $"{ap1} {ap2}",
                        Carrera = grupo.Carrera,
                        SemestreActual = grupo.Semestre,
                        PromedioGlobal = perfil.Promedio,
                        MateriasReprobadas = perfil.Reprobadas,
                        Ausencias = perfil.Ausencias,
                        ParcialesBajos = perfil.ParcialesBajos,
                        EtsPresentados = perfil.ETSpresentados,
                        Recursamientos = perfil.Recursamientos,
                        CargaAcademicaActual = _rng.Next(5, 10),
                        Estatus = "Activo",
                        Correo = email,
                        UserId = user.Id,
                        GrupoId = grupo.Id,
                        FechaUltimaActualizacion = DateTime.Now.AddDays(-_rng.Next(1, 30))
                    });
                }
            }
            context.Alumnos.AddRange(alumnos);
            await context.SaveChangesAsync();
            return alumnos;
        }

        private static (decimal Promedio, int Reprobadas, int Ausencias, int ParcialesBajos, int ETSpresentados, int Recursamientos) GenerarPerfilAcademico()
        {
            var roll = _rng.NextDouble();
            if (roll < 0.40)
            {
                return (Math.Round((decimal)(_rng.NextDouble() * 2.0 + 8.0), 2), 0, _rng.Next(0, 3), 0, 0, 0);
            }
            else if (roll < 0.75)
            {
                return (Math.Round((decimal)(_rng.NextDouble() * 1.4 + 6.0), 2), _rng.Next(0, 2), _rng.Next(2, 5), _rng.Next(0, 2), _rng.Next(0, 2), _rng.Next(0, 2));
            }
            else
            {
                return (Math.Round((decimal)(_rng.NextDouble() * 2.5 + 4.0), 2), _rng.Next(1, 4), _rng.Next(4, 10), _rng.Next(1, 4), _rng.Next(0, 3), _rng.Next(0, 3));
            }
        }

        private static async Task SeedMateriasAsync(ApplicationDbContext context)
        {
            if (await context.Materias.AnyAsync()) return;
            var materias = new List<Materia>();
            foreach (var m in MateriasComunes)
            {
                materias.Add(new Materia { Clave = m.Clave, Nombre = m.Nombre, Creditos = (int)m.Creditos, Semestre = m.Semestre, Carrera = "Común" });
            }
            foreach (var carrera in MateriasPorCarrera)
            {
                foreach (var m in carrera.Value)
                {
                    materias.Add(new Materia { Clave = m.Clave, Nombre = m.Nombre, Creditos = (int)m.Creditos, Semestre = m.Semestre, Carrera = Carreras[Array.IndexOf(CarreraClaves, carrera.Key)] });
                }
            }
            context.Materias.AddRange(materias);
            await context.SaveChangesAsync();
        }

        private static async Task SeedCalificacionesAsync(ApplicationDbContext context, List<Alumno> alumnos)
        {
            if (await context.Calificaciones.AnyAsync()) return;
            var materias = await context.Materias.ToListAsync();
            var calificaciones = new List<Calificacion>();

            foreach (var alumno in alumnos)
            {
                // Common subjects for semesters 1-2 (all students have completed these)
                var comunes = MateriasComunes;
                foreach (var mat in comunes)
                {
                    var baseGrade = (double)alumno.PromedioGlobal;
                    var variance = _rng.NextDouble() * 3.0 - 1.5;
                    var grade = Math.Clamp(baseGrade + variance, 4.0, 10.0);
                    var aprobada = grade >= 6.0;
                    calificaciones.Add(new Calificacion
                    {
                        AlumnoMatricula = alumno.Matricula,
                        MateriaId = materias.First(m => m.Clave == mat.Clave).Id,
                        Periodo = mat.Semestre == 1 ? "2025-A" : "2025-B",
                        CicloEscolar = mat.Semestre == 1 ? "2025-A" : "2025-B",
                        Valor = (decimal)Math.Round(grade, 1),
                        Aprobada = aprobada,
                        VecesCursada = aprobada ? 1 : _rng.Next(1, 3),
                        Tipo = aprobada ? "Ordinario" : (_rng.NextDouble() > 0.5 ? "Extraordinario" : "Ordinario")
                    });
                }

                // Career-specific subjects for semesters 3+
                var carreraClave = CarreraClaves[Array.IndexOf(Carreras, alumno.Carrera)];
                if (MateriasPorCarrera.TryGetValue(carreraClave, out var matsCarrera))
                {
                    foreach (var mat in matsCarrera.Where(m => m.Semestre <= alumno.SemestreActual))
                    {
                        var dbMat = materias.FirstOrDefault(m => m.Clave == mat.Clave);
                        if (dbMat == null) continue;
                        var baseGrade = (double)alumno.PromedioGlobal;
                        var variance = _rng.NextDouble() * 3.0 - 1.5;
                        var grade = Math.Clamp(baseGrade + variance, 4.0, 10.0);
                        var aprobada = grade >= 6.0;
                        var periodo = mat.Semestre <= 3 ? "2025-A" : mat.Semestre <= 4 ? "2025-B" : mat.Semestre <= 5 ? "2026-A" : "2026-A";
                        calificaciones.Add(new Calificacion
                        {
                            AlumnoMatricula = alumno.Matricula,
                            MateriaId = dbMat.Id,
                            Periodo = periodo,
                            CicloEscolar = periodo,
                            Valor = (decimal)Math.Round(grade, 1),
                            Aprobada = aprobada,
                            VecesCursada = aprobada ? 1 : _rng.Next(1, 3),
                            Tipo = aprobada ? "Ordinario" : (_rng.NextDouble() > 0.5 ? "Extraordinario" : "Ordinario")
                        });
                    }
                }
            }
            context.Calificaciones.AddRange(calificaciones);
            await context.SaveChangesAsync();
        }

        private static async Task SeedAsignacionesTutorAsync(ApplicationDbContext context, List<Tutor> profesores, List<Alumno> alumnos)
        {
            if (await context.AsignacionesTutor.AnyAsync()) return;
            var asignaciones = new List<AsignacionTutor>();
            var grupos = alumnos.GroupBy(a => a.GrupoId).ToList();
            foreach (var grupo in grupos)
            {
                var prof = profesores[_rng.Next(profesores.Count)];
                foreach (var alumno in grupo)
                {
                    asignaciones.Add(new AsignacionTutor
                    {
                        TutorId = prof.Id,
                        AlumnoMatricula = alumno.Matricula,
                        Periodo = "2026-A",
                        EstaActiva = true,
                        FechaAsignacion = DateTime.Now.AddMonths(-_rng.Next(1, 4))
                    });
                }
            }
            context.AsignacionesTutor.AddRange(asignaciones);
            await context.SaveChangesAsync();
        }

        private static async Task SeedProyectosAsync(ApplicationDbContext context)
        {
            if (await context.Proyectos.AnyAsync()) return;
            context.Proyectos.AddRange(
                new Proyecto { Titulo = "Laboratorio de Cómputo Avanzado", Descripcion = "Apoyo en mantenimiento y soporte de laboratorios de cómputo del plantel.", Tipo = "Laboratorio", CupoMaximo = 15, FechaCierre = DateTime.Now.AddDays(45), EstaActivo = true },
                new Proyecto { Titulo = "Biblioteca Digital IPN", Descripcion = "Digitalización de acervo bibliográfico y modernización del catálogo.", Tipo = "Servicio Social", CupoMaximo = 10, FechaCierre = DateTime.Now.AddDays(30), EstaActivo = true },
                new Proyecto { Titulo = "Centro de Cómputo", Descripcion = "Soporte técnico y mantenimiento preventivo de equipos institucionales.", Tipo = "Apoyo Académico", CupoMaximo = 12, FechaCierre = DateTime.Now.AddDays(60), EstaActivo = true },
                new Proyecto { Titulo = "Investigación en IA Educativa", Descripcion = "Desarrollo de herramientas de IA para apoyo al estudiante.", Tipo = "Investigacion", CupoMaximo = 5, FechaCierre = DateTime.Now.AddDays(90), EstaActivo = true },
                new Proyecto { Titulo = "Programa de Tutorías entre Pares", Descripcion = "Apoyo académico entre estudiantes para regularización.", Tipo = "Apoyo Académico", CupoMaximo = 20, FechaCierre = DateTime.Now.AddDays(25), EstaActivo = true },
                new Proyecto { Titulo = "Laboratorio de Química", Descripcion = "Organización de material y bitácoras de experimentos.", Tipo = "Laboratorio", CupoMaximo = 8, FechaCierre = DateTime.Now.AddDays(40), EstaActivo = true },
                new Proyecto { Titulo = "Diseño de Material Didáctico", Descripcion = "Creación de infografías y videos educativos.", Tipo = "Servicio Social", CupoMaximo = 6, FechaCierre = DateTime.Now.AddDays(35), EstaActivo = true },
                new Proyecto { Titulo = "Análisis de Datos Institucionales", Descripcion = "Procesamiento estadístico de indicadores académicos.", Tipo = "Investigacion", CupoMaximo = 4, FechaCierre = DateTime.Now.AddDays(55), EstaActivo = true }
            );
            await context.SaveChangesAsync();
        }

        private static async Task SeedConvocatoriasAsync(ApplicationDbContext context)
        {
            if (await context.Convocatorias.AnyAsync()) return;
            var proyectos = await context.Proyectos.ToListAsync();
            context.Convocatorias.AddRange(
                new Convocatoria { Titulo = "Servicio Social en Laboratorios de Cómputo", Descripcion = "Apoyo en laboratorios de cómputo. Requiere promedio mínimo y disponibilidad de horario.", Tipo = "ServicioSocial", ProyectoId = proyectos[0].Id, CupoMaximo = 15, FechaCierre = DateTime.Now.AddDays(45), PromedioMinimo = 7.0m, SemestreMinimo = 3, ValidadaPorAcademia = true, Area = "Ciencias Básicas", EstaActiva = true, FechaPublicacion = DateTime.Now.AddDays(-10), PostulacionesActuales = 0 },
                new Convocatoria { Titulo = "Investigación en IA Educativa", Descripcion = "Proyecto de investigación en inteligencia artificial aplicada a la educación.", Tipo = "Investigacion", ProyectoId = proyectos[3].Id, CupoMaximo = 5, FechaCierre = DateTime.Now.AddDays(90), PromedioMinimo = 8.5m, SemestreMinimo = 5, ValidadaPorAcademia = true, Area = "Tecnología", EstaActiva = true, FechaPublicacion = DateTime.Now.AddDays(-5), PostulacionesActuales = 0 },
                new Convocatoria { Titulo = "Apoyo en Biblioteca Digital", Descripcion = "Digitalización de documentos y soporte en biblioteca.", Tipo = "ApoyoAcademico", ProyectoId = proyectos[1].Id, CupoMaximo = 10, FechaCierre = DateTime.Now.AddDays(30), PromedioMinimo = 6.0m, SemestreMinimo = 2, ValidadaPorAcademia = true, Area = "Humanidades", EstaActiva = true, FechaPublicacion = DateTime.Now.AddDays(-15), PostulacionesActuales = 0 },
                new Convocatoria { Titulo = "Laboratorio de Química", Descripcion = "Seguimiento de material y bitácoras de laboratorio.", Tipo = "Laboratorio", ProyectoId = proyectos[5].Id, CupoMaximo = 8, FechaCierre = DateTime.Now.AddDays(40), PromedioMinimo = 7.5m, SemestreMinimo = 3, ValidadaPorAcademia = true, Area = "Laboratorio", EstaActiva = true, FechaPublicacion = DateTime.Now.AddDays(-8), PostulacionesActuales = 0 },
                new Convocatoria { Titulo = "Tutorías entre Pares", Descripcion = "Apoyo entre estudiantes para regularización académica.", Tipo = "ApoyoAcademico", ProyectoId = proyectos[4].Id, CupoMaximo = 20, FechaCierre = DateTime.Now.AddDays(25), PromedioMinimo = 7.0m, SemestreMinimo = 2, ValidadaPorAcademia = true, Area = "Formación", EstaActiva = true, FechaPublicacion = DateTime.Now.AddDays(-12), PostulacionesActuales = 0 },
                new Convocatoria { Titulo = "Diseño de Material Didáctico", Descripcion = "Creación de recursos visuales y multimedia para docentes.", Tipo = "ServicioSocial", ProyectoId = proyectos[6].Id, CupoMaximo = 6, FechaCierre = DateTime.Now.AddDays(35), PromedioMinimo = 7.0m, SemestreMinimo = 3, CarreraRequerida = "Diseño y Comunicación Visual", ValidadaPorAcademia = true, Area = "Diseño", EstaActiva = true, FechaPublicacion = DateTime.Now.AddDays(-7), PostulacionesActuales = 0 },
                new Convocatoria { Titulo = "Centro de Cómputo", Descripcion = "Mantenimiento y soporte técnico.", Tipo = "ApoyoAcademico", ProyectoId = proyectos[2].Id, CupoMaximo = 12, FechaCierre = DateTime.Now.AddDays(60), PromedioMinimo = 6.5m, SemestreMinimo = 2, ValidadaPorAcademia = true, Area = "Tecnología", EstaActiva = true, FechaPublicacion = DateTime.Now.AddDays(-20), PostulacionesActuales = 0 },
                new Convocatoria { Titulo = "Análisis de Datos Institucionales", Descripcion = "Procesamiento de estadísticas académicas.", Tipo = "Investigacion", ProyectoId = proyectos[7].Id, CupoMaximo = 4, FechaCierre = DateTime.Now.AddDays(55), PromedioMinimo = 8.0m, SemestreMinimo = 4, ValidadaPorAcademia = true, Area = "Estadística", EstaActiva = true, FechaPublicacion = DateTime.Now.AddDays(-3), PostulacionesActuales = 0 }
            );
            await context.SaveChangesAsync();
        }

        private static async Task SeedPostulacionesAsync(ApplicationDbContext context, List<Alumno> alumnos)
        {
            if (await context.Postulaciones.AnyAsync()) return;
            var convocatorias = await context.Convocatorias.ToListAsync();
            var postulaciones = new List<Postulacion>();
            var estados = new[] { "Aceptado", "En Revisión", "Rechazado" };
            var pesos = new[] { 0.35, 0.45, 0.20 };
            int postIdx = 0;

            foreach (var conv in convocatorias)
            {
                int count = _rng.Next(8, 20);
                var candidatos = alumnos.Where(a =>
                    a.SemestreActual >= (conv.SemestreMinimo ?? 1) &&
                    a.PromedioGlobal >= (conv.PromedioMinimo ?? 0m) &&
                    (string.IsNullOrEmpty(conv.CarreraRequerida) || a.Carrera == conv.CarreraRequerida)
                ).OrderBy(_ => _rng.Next()).Take(count).ToList();

                foreach (var alumno in candidatos)
                {
                    var estado = ElegirConPesos(estados, pesos);
                    postulaciones.Add(new Postulacion
                    {
                        AlumnoId = alumno.Matricula,
                        ProyectoId = conv.ProyectoId ?? 0,
                        Estado = estado,
                        FechaSolicitud = DateTime.Now.AddDays(-_rng.Next(1, 20)),
                        DocumentoNombre = _rng.NextDouble() > 0.6 ? $"CV_{alumno.Matricula}.pdf" : null,
                        DocumentoRuta = _rng.NextDouble() > 0.6 ? $"/uploads/postulaciones/demo_{postIdx}.pdf" : null
                    });
                    postIdx++;
                }
            }
            context.Postulaciones.AddRange(postulaciones);
            await context.SaveChangesAsync();

            foreach (var conv in convocatorias)
            {
                conv.PostulacionesActuales = postulaciones.Count(p => p.ProyectoId == conv.ProyectoId);
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedIntervencionesAsync(ApplicationDbContext context, List<Tutor> profesores, List<Alumno> alumnos)
        {
            if (await context.IntervencionesTutoria.AnyAsync()) return;
            var intervenciones = new List<IntervencionTutoria>();
            var tipos = new[] { "Académica", "Psicológica", "Vocacional", "Disciplinaria", "Seguimiento" };
            var resultados = new[] { "Positivo", "En Proceso", "Negativo", "Requiere Seguimiento" };
            var alumnosRiesgo = alumnos.Where(a => a.PromedioGlobal < 7.5m || (a.MateriasReprobadas ?? 0) > 0).ToList();

            foreach (var alumno in alumnosRiesgo.Take(80))
            {
                var tutor = profesores[_rng.Next(profesores.Count)];
                intervenciones.Add(new IntervencionTutoria
                {
                    TutorId = tutor.Id,
                    AlumnoMatricula = alumno.Matricula,
                    Fecha = DateTime.Now.AddDays(-_rng.Next(1, 60)),
                    Tipo = tipos[_rng.Next(tipos.Length)],
                    Descripcion = $"Intervención {tipos[_rng.Next(tipos.Length)].ToLower()} realizada. Se evaluó situación académica del estudiante {alumno.Nombre} {alumno.Apellidos}.",
                    Resultado = resultados[_rng.Next(resultados.Length)],
                    RequiereSeguimiento = _rng.NextDouble() > 0.6,
                    NotasSeguimiento = "Asistir a asesorías, organizar horario de estudio, revisar material de apoyo."
                });
            }
            context.IntervencionesTutoria.AddRange(intervenciones);
            await context.SaveChangesAsync();
        }

        private static async Task SeedPlanesMejoraAsync(ApplicationDbContext context, List<Tutor> profesores, List<Alumno> alumnos)
        {
            if (await context.PlanesMejora.AnyAsync()) return;
            var planes = new List<PlanMejora>();
            var estados = new[] { "Activo", "Cumplido", "Vencido", "Cancelado" };
            var alumnosRiesgo = alumnos.Where(a => a.PromedioGlobal < 7.0m).ToList();

            foreach (var alumno in alumnosRiesgo.Take(40))
            {
                var tutor = profesores[_rng.Next(profesores.Count)];
                var estado = estados[_rng.Next(estados.Length)];
                planes.Add(new PlanMejora
                {
                    AlumnoMatricula = alumno.Matricula,
                    TutorId = tutor.Id,
                    FechaCreacion = DateTime.Now.AddDays(-_rng.Next(7, 45)),
                    FechaCierre = estado == "Activo" ? DateTime.Now.AddDays(30) : DateTime.Now.AddDays(-_rng.Next(1, 10)),
                    Estado = estado,
                    Recomendaciones = "1. Asistir puntualmente a clases. 2. Entregar tareas en tiempo y forma. 3. Solicitar asesorías en materias con bajo rendimiento.",
                    Metas = "Subir promedio general en 0.5 puntos. No reprobar materias en el ciclo actual. Reducir inasistencias a máximo 2.",
                    AccionesTomadas = "Primera reunión de seguimiento realizada. Se acordó horario de asesorías."
                });
            }
            context.PlanesMejora.AddRange(planes);
            await context.SaveChangesAsync();
        }

        private static async Task SeedNotificacionesAsync(ApplicationDbContext context, List<Alumno> alumnos, List<Tutor> profesores, string adminId, string autoridadId)
        {
            if (await context.Notificaciones.AnyAsync()) return;
            var notificaciones = new List<Notificacion>();
            var allUserIds = alumnos.Select(a => a.UserId).Concat(profesores.Select(t => t.UserId)).Concat(new[] { adminId, autoridadId }).Where(id => !string.IsNullOrEmpty(id)).ToList();

            foreach (var uid in allUserIds)
            {
                notificaciones.Add(new Notificacion
                {
                    UserId = uid!,
                    Titulo = "Bienvenido a Rescate Académico",
                    Mensaje = "El sistema de monitoreo académico del IPN está listo para su uso.",
                    Tipo = "Informacion",
                    FechaCreacion = DateTime.Now.AddDays(-5),
                    Leida = _rng.NextDouble() > 0.3
                });
            }

            foreach (var alumno in alumnos.Where(a => a.PromedioGlobal < 7.0m).Take(60))
            {
                if (!string.IsNullOrEmpty(alumno.UserId))
                {
                    notificaciones.Add(new Notificacion
                    {
                        UserId = alumno.UserId,
                        Titulo = "Alerta de Riesgo Académico",
                        Mensaje = $"Se detectó que tu promedio actual es {alumno.PromedioGlobal:F2}. Se recomienda contactar a tu profesor.",
                        Tipo = "Advertencia",
                        FechaCreacion = DateTime.Now.AddDays(-_rng.Next(1, 5)),
                        Leida = false,
                        Enlace = "/PerfilAcademico"
                    });
                }
            }

            var postulaciones = await context.Postulaciones.Include(p => p.Alumno).ToListAsync();
            foreach (var post in postulaciones.Where(p => p.Estado != "En Revisión" && p.Alumno?.UserId != null).Take(80))
            {
                notificaciones.Add(new Notificacion
                {
                    UserId = post.Alumno!.UserId!,
                    Titulo = $"Postulación {post.Estado}",
                    Mensaje = $"Tu postulación ha sido {post.Estado.ToLower()}.",
                    Tipo = post.Estado == "Aceptado" ? "Exito" : "Error",
                    FechaCreacion = DateTime.Now.AddDays(-_rng.Next(1, 3)),
                    Leida = _rng.NextDouble() > 0.5
                });
            }
            context.Notificaciones.AddRange(notificaciones);
            await context.SaveChangesAsync();
        }

        private static async Task SeedBitacoraAsync(ApplicationDbContext context, string adminId, string autoridadId)
        {
            if (await context.BitacoraLogs.AnyAsync()) return;
            context.BitacoraLogs.AddRange(
                new BitacoraLog { UsuarioId = adminId, UsuarioEmail = "admin@ipn.mx", Accion = "Crear", TablaAfectada = "Convocatorias", RegistroNuevo = "Servicio Social en Laboratorios", FechaHora = DateTime.Now.AddDays(-20) },
                new BitacoraLog { UsuarioId = adminId, UsuarioEmail = "admin@ipn.mx", Accion = "Crear", TablaAfectada = "Convocatorias", RegistroNuevo = "Investigación en IA", FechaHora = DateTime.Now.AddDays(-18) },
                new BitacoraLog { UsuarioId = adminId, UsuarioEmail = "admin@ipn.mx", Accion = "Editar", TablaAfectada = "Alumnos", RegistroNuevo = "Actualización masiva de datos académicos", FechaHora = DateTime.Now.AddDays(-10) },
                new BitacoraLog { UsuarioId = adminId, UsuarioEmail = "admin@ipn.mx", Accion = "Crear", TablaAfectada = "Usuarios", RegistroNuevo = "Creación de cuentas institucionales", FechaHora = DateTime.Now.AddDays(-8) },
                new BitacoraLog { UsuarioId = autoridadId, UsuarioEmail = "autoridad@ipn.mx", Accion = "Consulta", TablaAfectada = "Estadisticas", RegistroNuevo = "Reporte institucional Q1 2026", FechaHora = DateTime.Now.AddDays(-5) },
                new BitacoraLog { UsuarioId = autoridadId, UsuarioEmail = "autoridad@ipn.mx", Accion = "Exportar", TablaAfectada = "Alumnos", RegistroNuevo = "Exportación CSV de alumnos en riesgo", FechaHora = DateTime.Now.AddDays(-2) },
                new BitacoraLog { UsuarioId = adminId, UsuarioEmail = "admin@ipn.mx", Accion = "Evaluar", TablaAfectada = "RiesgoAcademico", RegistroNuevo = "Evaluación automática de riesgos", FechaHora = DateTime.Now.AddDays(-1) }
            );
            await context.SaveChangesAsync();
        }

        private static async Task SeedSugerenciasIAAsync(ApplicationDbContext context, List<Alumno> alumnos)
        {
            if (await context.SugerenciasIA.AnyAsync()) return;
            var sugerencias = new List<SugerenciaIA>();
            var proyectos = await context.Proyectos.ToListAsync();
            foreach (var alumno in alumnos.Take(50))
            {
                var proyecto = proyectos[_rng.Next(proyectos.Count)];
                sugerencias.Add(new SugerenciaIA
                {
                    AlumnoMatricula = alumno.Matricula,
                    ProyectoId = proyecto.Id,
                    Tipo = "ProyectoSugerido",
                    Titulo = $"Proyecto recomendado: {proyecto.Titulo}",
                    Descripcion = $"Basado en tu perfil académico (promedio {alumno.PromedioGlobal}), este proyecto tiene alta compatibilidad.",
                    Puntuacion = Math.Min(100, alumno.PromedioGlobal * 10 + _rng.Next(-10, 10)),
                    Razonamiento = "Análisis de promedio, carrera y semestre.",
                    Mostrada = _rng.NextDouble() > 0.3,
                    FechaGeneracion = DateTime.Now.AddDays(-_rng.Next(1, 10))
                });
            }
            context.SugerenciasIA.AddRange(sugerencias);
            await context.SaveChangesAsync();
        }

        private static async Task SeedDictamenesAsync(ApplicationDbContext context, List<Alumno> alumnos)
        {
            if (await context.DictamenesAcademicos.AnyAsync()) return;
            var dictamenes = new List<DictamenAcademico>();
            var tipos = new[] { "Baja Temporal", "Baja Definitiva", "Cambio de Carrera", "Dictamen Técnico", "Reincorporación", "Regularización" };
            var estados = new[] { "Activo", "Resuelto", "Cancelado" };
            var candidatos = alumnos.Where(a => a.PromedioGlobal < 6.5m || (a.MateriasReprobadas ?? 0) >= 2).OrderBy(_ => _rng.Next()).Take(25).ToList();

            foreach (var alumno in candidatos)
            {
                var tipo = tipos[_rng.Next(tipos.Length)];
                dictamenes.Add(new DictamenAcademico
                {
                    AlumnoMatricula = alumno.Matricula,
                    Tipo = tipo,
                    Descripcion = $"Dictamen de {tipo.ToLower()} emitido para el estudiante {alumno.Nombre} {alumno.Apellidos} debido a situación académica.",
                    FechaEmision = DateTime.Now.AddDays(-_rng.Next(10, 90)),
                    Estado = estados[_rng.Next(estados.Length)],
                    EmitidoPor = "Comité Académico CECyT 13",
                    Observaciones = tipo == "Baja Temporal" ? "Se recomienda reincorporación en siguiente ciclo con plan de mejora." :
                                    tipo == "Regularización" ? "El estudiante debe presentar extraordinarios en las materias reprobadas." :
                                    "Seguimiento por orientación educativa."
                });
            }
            context.DictamenesAcademicos.AddRange(dictamenes);
            await context.SaveChangesAsync();
        }

        private static async Task SeedCosecoviAsync(ApplicationDbContext context, List<Alumno> alumnos)
        {
            if (await context.ReportesCosecovi.AnyAsync()) return;
            var reportes = new List<ReporteCosecovi>();
            var canalizaciones = new[] { "Psicología", "Trabajo Social", "Orientación Educativa", "Médico", "Tutoría Académica" };
            var situaciones = new[] {
                "Bajo rendimiento académico detectado en el semestre actual.",
                "Inasistencias recurrentes sin justificación médica.",
                "Cambio de comportamiento y desmotivación observada.",
                "Situación familiar que afecta el desempeño escolar.",
                "Dificultades de adaptación al ambiente escolar."
            };
            var candidatos = alumnos.Where(a => a.PromedioGlobal < 7.0m || (a.Ausencias ?? 0) > 4).OrderBy(_ => _rng.Next()).Take(35).ToList();

            foreach (var alumno in candidatos)
            {
                reportes.Add(new ReporteCosecovi
                {
                    AlumnoMatricula = alumno.Matricula,
                    Periodo = "2026-A",
                    FechaReporte = DateTime.Now.AddDays(-_rng.Next(5, 60)),
                    SituacionObservada = situaciones[_rng.Next(situaciones.Length)],
                    Recomendaciones = "Seguimiento semanal por parte del profesor tutor. Entrevista con padres de familia.",
                    AccionesPropuestas = "Canalización al área correspondiente. Plan de acción personalizado.",
                    Canalizacion = canalizaciones[_rng.Next(canalizaciones.Length)],
                    Estado = _rng.NextDouble() > 0.4 ? "Atendido" : "Seguimiento",
                    ElaboradoPor = "COSECOVI CECyT 13"
                });
            }
            context.ReportesCosecovi.AddRange(reportes);
            await context.SaveChangesAsync();
        }

        private static async Task SeedPrediccionesDesercionAsync(ApplicationDbContext context, List<Alumno> alumnos)
        {
            if (await context.PrediccionesDesercion.AnyAsync()) return;
            var predicciones = new List<PrediccionDesercion>();
            foreach (var alumno in alumnos)
            {
                var prob = CalcularProbabilidadDesercion(alumno);
                var nivel = prob > 0.7m ? "Critico" : prob > 0.5m ? "Alto" : prob > 0.3m ? "Medio" : "Bajo";
                var factores = new List<string>();
                if (alumno.PromedioGlobal < 6) factores.Add("Promedio bajo");
                if ((alumno.MateriasReprobadas ?? 0) >= 2) factores.Add("Materias reprobadas");
                if ((alumno.Ausencias ?? 0) > 5) factores.Add("Alto ausentismo");
                if ((alumno.ParcialesBajos ?? 0) >= 2) factores.Add("Parciales bajos");
                if (factores.Count == 0) factores.Add("Sin factores críticos detectados");

                predicciones.Add(new PrediccionDesercion
                {
                    AlumnoMatricula = alumno.Matricula,
                    ProbabilidadDesercion = prob,
                    NivelRiesgo = nivel,
                    FactoresDetectados = string.Join(", ", factores),
                    Recomendaciones = nivel == "Bajo" ? "Mantener seguimiento periódico." : "Intervención inmediata recomendada: tutoría académica y apoyo psicológico.",
                    AusenciasTotales = alumno.Ausencias ?? 0,
                    PromedioParcial = alumno.PromedioGlobal,
                    MateriasReprobadas = alumno.MateriasReprobadas ?? 0,
                    IntervencionRealizada = false,
                    FechaPrediccion = DateTime.Now.AddDays(-_rng.Next(1, 7)),
                    PeriodoEvaluado = "2026-A"
                });
            }
            context.PrediccionesDesercion.AddRange(predicciones);
            await context.SaveChangesAsync();
        }

        private static decimal CalcularProbabilidadDesercion(Alumno a)
        {
            double score = 0;
            score += Math.Max(0, (7.0 - (double)a.PromedioGlobal) * 0.15);
            score += (a.MateriasReprobadas ?? 0) * 0.12;
            score += (a.Ausencias ?? 0) * 0.04;
            score += (a.ParcialesBajos ?? 0) * 0.08;
            score += (a.EtsPresentados ?? 0) * 0.06;
            score += (a.Recursamientos ?? 0) * 0.07;
            return Math.Min(0.99m, Math.Round((decimal)score, 2));
        }

        private static string ElegirConPesos(string[] opciones, double[] pesos)
        {
            var r = _rng.NextDouble();
            double acum = 0;
            for (int i = 0; i < opciones.Length; i++)
            {
                acum += pesos[i];
                if (r <= acum) return opciones[i];
            }
            return opciones[^1];
        }
    }
}
