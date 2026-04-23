# Rescate Académico - README

## Descripción
Plataforma web institucional para el monitoreo y seguimiento del desempeño estudiantil del IPN. Facilita la asignación de alumnos a proyectos académicos mediante una "radiografía" visual del desempeño.

## Tecnologías
- ASP.NET Core MVC (.NET 8.0)
- Entity Framework Core (SQLite local / PostgreSQL para producción)
- ASP.NET Identity (autenticación y roles)
- Bootstrap 5 + Bootstrap Icons
- ML.NET (predicción de deserción)

## Cómo ejecutar localmente

### Prerrequisitos
- .NET 8.0 SDK instalado
- Git

### Pasos
```bash
# Clonar repositorio
git clone https://github.com/Xerea/RescateAcademico.git
cd RescateAcademico/RescateAcademico

# Restaurar paquetes
dotnet restore

# Ejecutar
dotnet run
```

La aplicación estará disponible en `https://localhost:5001` (o el puerto que indique la consola).

### Credenciales de prueba (Demo)

> **Nota importante**: Todos los datos en esta demo son **simulados** (mock data) generados para demostrar la funcionalidad del sistema. No provienen de SAES real, pero representan fielmente la estructura y tipos de datos que el sistema procesaría en producción.

#### Roles institucionales
| Rol | Email | Contraseña |
|-----|-------|------------|
| Administrador | admin@ipn.mx | Admin123! |
| Autoridad | autoridad@ipn.mx | Autoridad123! |
| Profesor #1 | profesor1@ipn.mx | Demo123! |
| Profesor #2 | profesor2@ipn.mx | Demo123! |

> **Tip**: Puedes usar cualquier profesor del rango `profesor1@ipn.mx` a `profesor12@ipn.mx`. Todas las contraseñas son `Demo123!`.

#### Alumnos de demostración (~308 cuentas)
Cada alumno tiene su propia cuenta de usuario vinculada. El patrón de email sigue el formato IPN: `{inicial_nombre}{apellido_paterno}{inicial_materna}{año}00@alumno.ipn.mx` (ej. `salanisp2300@alumno.ipn.mx`). La boleta sigue el formato: `2023{secuencia:000000}`.

| Boleta | Email | Contraseña | Perfil académico |
|--------|-------|------------|------------------|
| 2023000001 | sgarcia2300@alumno.ipn.mx | Demo123! | Verde (buen desempeño) |
| 2023000002 | llopez2300@alumno.ipn.mx | Demo123! | Verde |
| 2023000100 | pmartinez2300@alumno.ipn.mx | Demo123! | Amarillo (riesgo moderado) |
| 2023000200 | jhernandez2300@alumno.ipn.mx | Demo123! | Amarillo |
| 2023000250 | rgonzalez2300@alumno.ipn.mx | Demo123! | Rojo (alto riesgo) |
| 2023000300 | mperez2300@alumno.ipn.mx | Demo123! | Rojo |

> **Tip**: Puedes usar cualquier boleta del rango `2023000001` a `2023000308`. Todas las contraseñas son `Demo123!`.

#### Distribución de datos demo
- **~308 alumnos** en **10 grupos** del CECyT 13 (3ro a 6to semestre)
- **12 profesores** con especialidades reales del IPN
- **6 carreras** del CECyT 13: Administración, Contabilidad, Gastronomía, Informática, Seguridad Informática, Turismo
- **8 proyectos** con convocatorias activas
- **~100 postulaciones** en varios estados (Aceptado, Rechazado, En Revisión)
- **~4,200 calificaciones** distribuidas por materia y ciclo
- **80 intervenciones** registradas
- **40 planes de mejora** académica
- **25 dictámenes académicos**
- **35 reportes COSECOVI**
- **Predicciones ML.NET** generadas para todos los alumnos

## Configuración de Base de Datos

### Desarrollo local (SQLite)
Por defecto usa SQLite (`app.db` en la carpeta del proyecto). Los datos persisten entre ejecuciones.

### Producción (Railway - PostgreSQL)
El proyecto está configurado para desplegarse automáticamente en Railway usando PostgreSQL. Solo necesitas:
1. Crear un proyecto en Railway y vincular este repositorio de GitHub.
2. Agregar un servicio **PostgreSQL** desde el dashboard de Railway.
3. Railway inyectará automáticamente la variable de entorno `DATABASE_URL`.
4. `Program.cs` detecta `DATABASE_URL` y usa `Npgsql` automáticamente. **No necesitas modificar `appsettings.json` ni `Program.cs`.**

> **Nota**: El primer despliegue creará las tablas y sembrará los 300+ registros demo automáticamente. Este proceso puede tardar 2-3 minutos.

## Estructura del Proyecto
```
Controllers/    -> Controladores MVC
Models/         -> Modelos de datos y ViewModels
Views/          -> Vistas Razor (.cshtml)
Data/           -> ApplicationDbContext, Seeders y RoleSeeder
wwwroot/        -> Archivos estáticos (CSS, JS, imágenes, uploads)
```

## Roles del Sistema
1. **Administrador** - Gestión completa: usuarios, alumnos, profesores, proyectos, convocatorias, reportes
2. **Profesor** - Monitoreo de estudiantes, registro de intervenciones, radiografía académica
3. **Alumno** - Perfil académico, postulación a convocatorias, notificaciones
4. **Autoridad** - Estadísticas globales, reportes institucionales, revisión de postulaciones

## Funcionalidades Implementadas
- Autenticación segura con bloqueo por intentos fallidos
- Gestión de roles y permisos
- Radiografía académica con semáforo de riesgo (Verde/Amarillo/Rojo)
- Catálogo de convocatorias con filtros
- Sistema de postulación con validación de elegibilidad
- Notificaciones automáticas
- Tablero estadístico
- Bitácora de auditoría
- Carga masiva de alumnos (CSV)
- Registro de intervenciones
- Dashboard de integridad de datos
- Exportación a CSV e impresión de reportes
- Predicción de deserción con ML.NET
- Planes de mejora académica personalizados
- Dictámenes académicos y reportes COSECOVI
- Panel de profesor con semáforo, grupos y historial académico tipo SAES

## Funcionalidades Pendientes
- Exportación a PDF/Excel con logo institucional
- Integración directa con SAES para datos reales

## Equipo
- Sergio: Autenticación, Roles, Ciclos Escolares, Bitácora
- Sara: Recuperación de contraseña, Convocatorias, Postulaciones, Estadísticas
- Alejandra: Dashboard, Perfil Académico, Notificaciones, Reportes
- Elias: Estudiantes, Carga Masiva, Intervenciones, Integridad
- Alexis: Seguridad, IA/ML, Alertas

## Notas para Desarrolladores
- `ResetOnStartup` está en `false` en producción. No modificar.
- El seeding de datos ocurre automáticamente al iniciar si las tablas están vacías.
- Las contraseñas deben cumplir: mínimo 6 caracteres, al menos una mayúscula y un número.

---
*Proyecto académico - IPN CECyT No. 13 - 2026*
