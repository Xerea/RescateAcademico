# Rescate Académico - Documentación Técnica Exhaustiva

## 1. Estructura General del Proyecto

El proyecto **Rescate Académico** es una aplicación web ASP.NET Core MVC (.NET 8.0) que sigue el patrón de arquitectura MVC (Modelo-Vista-Controlador). El proyecto está ubicado en `C:\Users\Xerea\RiderProjects\RescateAcademico`.

```
RescateAcademico/
├── RescateAcademico.slnx          # Archivo de solución
├── RescateAcademico/
│   ├── RescateAcademico.csproj   # Archivo de proyecto (.NET 8.0)
│   ├── Program.cs                 # Punto de entrada
│   ├── appsettings.json          # Configuración
│   ├── Controllers/               # Controladores
│   ├── Models/                  # Modelos de datos
│   ├── Views/                    # Vistas Razor
│   ├── Data/                     # Contexto de BD y seeding
│   ├── wwwroot/                  # Archivos estáticos
│   └── Migrations/               # Migraciones de EF
```

---

## 2. Carpeta DATA - Explicación Detallada

La carpeta `Data` contiene dos clases fundamentales que manejan la base de datos:

### 2.1 ApplicationDbContext.cs

**¿Qué hace?**
Es el contexto de Entity Framework que representa la base de datos. Define todas las tablas (DbSets) y las relaciones entre ellas.

**¿Por qué es importante?**
Es el puente entre la aplicación y SQLite. Cuando necesitamos guardar o retrieve datos, usamos este contexto.

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    // Tablas del sistema
    public DbSet<Alumno> Alumnos { get; set; }
    public DbSet<Proyecto> Proyectos { get; set; }
    public DbSet<Convocatoria> Convocatorias { get; set; }
    public DbSet<Postulacion> Postulaciones { get; set; }
    public DbSet<Calificacion> Calificaciones { get; set; }
    public DbSet<Materia> Materias { get; set; }
    public DbSet<Tutor> Tutores { get; set; }
    public DbSet<AsignacionTutor> AsignacionesTutor { get; set; }
    public DbSet<Notificacion> Notificaciones { get; set; }
    public DbSet<CicloEscolar> CiclosEscolares { get; set; }
    public DbSet<Carrera> Carreras { get; set; }
    public DbSet<BitacoraLog> BitacoraLogs { get; set; }
    // ... más tablas
}
```

**Relaciones configuradas:**
- Alumno tiene muchas Calificaciones (1:N)
- Alumno tiene muchas Postulaciones (1:N)
- Tutor tiene muchas AsignacionesTutor (1:N)
- Convocatoria tiene un Proyecto (N:1)
- Alumno tiene un User de Identity (1:1 opcional)

### 2.2 RoleSeeder.cs

**¿Qué hace?**
Es una clase estática que **siembra** (seed) la base de datos con datos iniciales cuando la aplicación inicia. Se ejecuta automáticamente en `Program.cs`.

**Datos que crea:**
1. **Roles:** Administrador, Tutor, Alumno, Autoridad
2. **Usuarios de prueba:**
   - admin@ipn.mx / Admin123! (Administrador)
   - tutor@ipn.mx / Tutor123! (Tutor)
   - alumno@alumno.ipn.mx / Alumno123! (Alumno)
   - autoridad@ipn.mx / Autoridad123! (Autoridad)
3. **Carreras:** Informática, Laboratorio Químico, Electrónica, etc.
4. **Ciclos Escolares:** 2026-A (actual), 2025-B, 2025-A
5. **Alumnos de prueba:** 8 alumnos con diferentes promedios y riesgos
6. **Materias:** 8 materias de Ingeniería en Informática
7. **Calificaciones:** Calificaciones de ejemplo para los alumnos
8. **Proyectos:** 3 proyectos activos
9. **Convocatorias:** 5 convocatorias con diferentes requisitos
10. **Postulaciones:** Ejemplos de postulaciones aceptadas/rechazadas

**¿Cómo funciona?**
```csharp
// En Program.cs se llama así:
await RoleSeeder.InitializeAsync(services, context);
```

---

## 3. Carpeta MODELS - Modelos de Datos

Cada clase en Models representa una tabla en la base de datos:

### Alumno
```csharp
- Matricula (PK, string) - Número de boleta del alumno
- Nombre, Apellidos
- Carrera, SemestreActual
- PromedioGlobal (decimal)
- RiesgoAcademico ("Verde", "Amarillo", "Rojo")
- CargaAcademicaActual, MateriasReprobadas, EtsPresentados, Recursamientos
- Estatus ("Activo", "Inactivo")
- UserId ( FK a Identity)
```

### Calificacion
```csharp
- Id (int, PK)
- AlumnoMatricula (FK -> Alumno.Matricula)
- MateriaId (FK -> Materia.Id)
- Periodo (string, ej: "2026-A")
- Valor (decimal, calificación de 0-10)
- Aprobada (bool)
- VecesCursada (int)
- Tipo ("Ordinario", "ETS", "Recursamiento")
```

### Tutor
```csharp
- Id, Nombre, Apellidos, Email
- Especialidad
- UserId (FK a Identity)
```

### Convocatoria
```csharp
- Id, Titulo, Descripcion
- Tipo ("ServicioSocial", "Investigacion", "Laboratorio", "ApoyoAcademico")
- ProyectoId (FK)
- CupoMaximo, PostulacionesActuales
- FechaPublicacion, FechaCierre
- PromedioMinimo, SemestreMinimo
- ValidadaPorAcademia, EstaActiva
```

### Postulacion
```csharp
- Id
- AlumnoId (FK -> Alumno.Matricula)
- ProyectoId (FK)
- FechaSolicitud
- Estado ("En Revisión", "Aceptado", "Rechazado")
```

---

## 4. Carpeta CONTROLLERS - Controladores

### AccountController
Gestiona autenticación: login, logout, registro, recuperación de contraseña.

### AdminController (Requiere rol: Administrador)
- **Index**: Dashboard con estadísticas
- **Alumnos**: CRUD de alumnos
- **Tutores**: CRUD de tutores
- **CiclosEscolares**: Gestión de períodos escolares
- **Carreras**: Gestión de carreras
- **Usuarios**: Gestión de usuarios de Identity
- **Bitacora**: Ver logs de auditoría

### AlumnosController
- **Index**: Lista de alumnos (Admin/Tutor)
- **MisTutorados**: Lista de tutorados del tutor actual
- **Detalles**: Ver perfil completo de un alumno

### PerfilAcademicoController
- **MiPerfil**: Radiografía académica del alumno logueado
- **VerTutorado**: Ver perfil de un tutorado (solo tutores)

### ConvocatoriasController
- **Index**: Catálogo público de convocatorias
- **Todas**: Gestión de convocatorias (Admin)
- **Create/Edit/Delete**: CRUD (Admin)

### PostulacionesController
- **MisPostulaciones**: Postulaciones del alumno
- **Postularse**: Crear nueva postulación
- **Todas**: Revisar todas las postulaciones (Admin/Autoridad)
- **CambiarEstado**: Aceptar/Rechazar postulación

### EstadisticasController (Admin/Autoridad)
- **Index**: Tablero estadístico
- **PorCarrera**: Estadísticas por carrera
- **Reporte**: Generar reporte general

---

## 5. Base de Datos - SQLite

### Conexión
```json
// appsettings.json
"ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
}
```

### ¿Cómo se crean las tablas?
1. **ApplicationDbContext** define los DbSets
2. **RoleSeeder** llama a `context.Database.EnsureCreated()` en startup
3. Esto crea todas las tablas automáticamente si no existen
4. **ResetOnStartup: true** en appsettings.json hace que se recree la BD cada vez

### Tablas principales:
- AspNetUsers, AspNetRoles, AspNetUserRoles (Identity)
- Alumnos, Materias, Calificaciones
- Tutores, AsignacionesTutor
- Proyectos, Convocatorias, Postulaciones
- Notificaciones, BitacoraLogs
- CiclosEscolares, Carreras

---

## 6. Autenticación y Autorización

### ¿Cómo funciona el login?
1. Usuario ingresa email y contraseña en `/Account/Login`
2. `SignInManager.PasswordSignInAsync` verifica credenciales
3. Si es correcto, crea una cookie de sesión
4. El sistema sabe quién es el usuario por los **Claims** en la cookie

### Roles del sistema:
| Rol | Descripción |
|-----|-------------|
| Administrador | Acceso completo: gestión de usuarios, alumnos, tutores, proyectos |
| Tutor | Ve solo sus tutorados, semáforo de riesgo |
| Alumno | Ve su perfil, postula a convocatorias |
| Autoridad | Estadísticas globales, reportes |

### Atributos de autorización:
```csharp
[Authorize]                           // Requiere sesión
[Authorize(Roles = "Administrador")] // Requiere rol específico
```

---

## 7. Métodos para Guardar Datos

### Entity Framework Core
Cuando queremos guardar datos en la base de datos:

```csharp
// 1. Obtener el contexto
private readonly ApplicationDbContext _context;

// 2. Agregar datos
_context.Alumnos.Add(nuevoAlumno);

// 3. Guardar cambios
await _context.SaveChangesAsync();

// Para actualizar
_context.Alumnos.Update(alumno);
await _context.SaveChangesAsync();

// Para eliminar
_context.Alumnos.Remove(alumno);
await _context.SaveChangesAsync();
```

### Consultas típicas:
```csharp
// Obtener todos
var alumnos = await _context.Alumnos.ToListAsync();

// Filtrar
var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.Matricula == "2023600001");

// Incluir relacionados
var alumno = await _context.Alumnos
    .Include(a => a.Calificaciones)
    .ThenInclude(c => c.Materia)
    .FirstOrDefaultAsync(a => a.Matricula == "2023600001");
```

---

## 8. Cómo Ejecutar el Proyecto

```bash
# En la carpeta del proyecto
cd RescateAcademico

# Restaurar paquetes
dotnet restore

# Compilar
dotnet build

# Ejecutar
dotnet run
```

El proyecto corre en: **https://localhost:5001** (o el puerto que indique la consola)

---

## 9. Credenciales de Prueba

| Rol | Email | Contraseña |
|-----|-------|------------|
| Administrador | admin@ipn.mx | Admin123! |
| Tutor | tutor@ipn.mx | Tutor123! |
| Alumno | alumno@alumno.ipn.mx | Alumno123! |
| Autoridad | autoridad@ipn.mx | Autoridad123! |

---

## 10. Funcionalidades Implementadas

### HU-RA-01: Inicio de Sesión Seguro ✓
- Validación de credenciales
- Solo usuarios activos
- Correos institucionales (@ipn.mx, @alumno.ipn.mx)

### HU-RA-02: Recuperación de Contraseña ✓
- Solicitud de correo
- Token de reseteo (expira en 10 min)

### HU-RA-03: Menú Principal (Dashboard) ✓
- Panel dinámico según rol
- Tarjetas con acceso rápido

### HU-RA-04: Gestión de Roles y Privilegios ✓
- 4 roles diferenciados
- Permisos por controlador

### HU-RA-05: Prevención de Intrusiones ✓
- Bloqueo tras 3 intentos (20 min)
- Sesión expira en 15 min

### HU-RA-06: Consulta de Situación Académica ✓
- Perfil académico completo
- Promedio global y por período
- Materias aprobadas/reprobadas

### HU-RA-07: Catálogo de Convocatorias ✓
- Filtros por tipo y área
- Proyectos cerrados sin postulación

### HU-RA-08: Postulación a Proyectos ✓
- Validación de requisitos
- Verificación de cupo

### HU-RA-09: Notificaciones de Acción ✓
- Notificaciones automáticas
- Indicador visual

### HU-RA-10: Monitoreo de Tutorados ✓
- Tutor ve solo sus asignados
- Radiografía académica

### HU-RA-11: Semáforo de Rendimiento ✓
- Verde: promedio >= 7.0
- Amarillo: promedio 6.0-6.9
- Rojo: promedio < 6.0

### HU-RA-12: Carga Masiva de Alumnado (Pendiente)
- Importación Excel/CSV

### HU-RA-13: Ajustes de Ciclos Escolares ✓
- CRUD de ciclos y carreras

### HU-RA-14: Bitácora de Auditoría (Parcial)
- Registro de acciones
- Necesita middleware automático

### HU-RA-15: Tablero Estadístico ✓
- Estadísticas globales y por carrera

### HU-RA-16: Emisión de Reportes Oficiales (Básico)
- Vista de reporte
- Necesita exportación PDF/Excel

### HU-RA-17: Sugerencia Optimizada (IA) - Parcial
- Sugerencias basadas en reglas

### HU-RA-18: Prevención de Deserción (IA) - Parcial
- Cálculo de riesgo académico
- Modelo de predicción definido

---

## 11. Diferencias con MiniTareas

| Aspecto | MiniTareas | Rescate Académico |
|---------|-------------|-------------------|
| Base de datos | En memoria (lista) | SQLite real |
| Persistencia | Se pierde al reiniciar | Permanente |
| ORM | No tiene | Entity Framework Core |
| Modelo de datos | 5 modelos | 18+ modelos |
| Roles | Estudiante/Profesor | Admin/Tutor/Alumno/Autoridad |

---

## 12. Recomendaciones para la Presentación

1. **Nombres de clases importantes:**
   - ApplicationDbContext (Data/)
   - RoleSeeder (Data/)
   - Alumno, Calificacion, Convocatoria, Postulacion (Models/)

2. **Métodos clave:**
   - `context.SaveChangesAsync()` - Guarda todo
   - `Include().ThenInclude()` - Carga datos relacionados

3. **Explicar el flujo:**
   - Usuario → Controlador → Modelo → Base de datos
   - Vista ← Controlador ← Datos

---

*Documento generado para la exposición técnica del proyecto Rescate Académico*
*IPN - CECyT No. 13*
*2026*
