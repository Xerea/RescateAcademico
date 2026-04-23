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
| Tutor #1 | tutor1@ipn.mx | Demo123! |
| Tutor #2 | tutor2@ipn.mx | Demo123! |

#### Alumnos de demostración (50 cuentas)
Cada alumno tiene su propia cuenta de usuario vinculada. El patrón de email es: `{matricula}@alumno.ipn.mx`

| Matrícula | Email | Contraseña | Perfil académico |
|-----------|-------|------------|------------------|
| 2023600001 | 2023600001@alumno.ipn.mx | Demo123! | Verde (buen desempeño) |
| 2023600002 | 2023600002@alumno.ipn.mx | Demo123! | Verde |
| 2023600015 | 2023600015@alumno.ipn.mx | Demo123! | Amarillo (riesgo moderado) |
| 2023600025 | 2023600025@alumno.ipn.mx | Demo123! | Amarillo |
| 2023600035 | 2023600035@alumno.ipn.mx | Demo123! | Rojo (alto riesgo) |
| 2023600045 | 2023600045@alumno.ipn.mx | Demo123! | Rojo |

> **Tip**: Puedes usar cualquier matrícula del rango `2023600001` a `2023600050`. Todas las contraseñas son `Demo123!`.

#### Distribución de datos demo
- **50 alumnos** con perfiles académicos variados (40% Verde, 35% Amarillo, 25% Rojo)
- **10 tutores** con especialidades reales del IPN
- **8 carreras** del CECyT 13: Programación, Computación, Contabilidad, Administración, Diseño y Comunicación Visual, Logística y Transporte, Máquinas y Herramientas, Sistemas Automotrices
- **8 proyectos** con convocatorias activas
- **~45 postulaciones** en varios estados (Aceptado, Rechazado, En Revisión)
- **~350 calificaciones** distribuidas por materia y ciclo
- **20 intervenciones tutoriales** registradas
- **12 planes de mejora** académica
- **Predicciones ML.NET** generadas para todos los alumnos

## Configuración de Base de Datos

### Desarrollo local (SQLite)
Por defecto usa SQLite (`app.db` en la carpeta del proyecto). Los datos persisten entre ejecuciones.

### Producción (Railway - PostgreSQL)
Para desplegar en Railway, actualizar `appsettings.json`:
```json
"ConnectionStrings": {
    "DefaultConnection": "Host=your-railway-host;Database=railway;Username=postgres;Password=your-password"
}
```

Y en `Program.cs`, cambiar `UseSqlite` por `UseNpgsql`.

## Estructura del Proyecto
```
Controllers/    -> Controladores MVC
Models/         -> Modelos de datos y ViewModels
Views/          -> Vistas Razor (.cshtml)
Data/           -> ApplicationDbContext y RoleSeeder
wwwroot/        -> Archivos estáticos (CSS, JS, imágenes, uploads)
```

## Roles del Sistema
1. **Administrador** - Gestión completa: usuarios, alumnos, tutores, proyectos, convocatorias, reportes
2. **Tutor** - Monitoreo de tutorados, registro de intervenciones, radiografía académica
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
- Registro de intervenciones tutorales
- Dashboard de integridad de datos

## Funcionalidades Pendientes
- Exportación a PDF/Excel con logo institucional
- ML.NET con datos reales de SAES
- Planes de mejora académica personalizados

## Equipo
- Sergio: Autenticación, Roles, Ciclos Escolares, Bitácora
- Sara: Recuperación de contraseña, Convocatorias, Postulaciones, Estadísticas
- Alejandra: Dashboard, Perfil Académico, Notificaciones, Reportes
- Elias: Tutorados, Carga Masiva, Intervenciones, Integridad
- Alexis: Seguridad, IA/ML, Alertas

## Notas para Desarrolladores
- `ResetOnStartup` está en `false` en producción. No modificar.
- El seeding de datos ocurre automáticamente al iniciar si las tablas están vacías.
- Las contraseñas deben cumplir: mínimo 6 caracteres, al menos una mayúscula y un número.

---
*Proyecto académico - IPN CECyT No. 13 - 2026*
