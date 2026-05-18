# Rescate Academico

Sistema web institucional para monitoreo de riesgo academico, seguimiento tutorial, planes de mejora, convocatorias y reportes operativos para el CECyT No. 13 del Instituto Politecnico Nacional.

Construido como una aplicacion ASP.NET Core MVC con autenticacion por roles, dashboards por perfil de usuario y reglas transparentes para clasificar riesgo academico y probabilidad de desercion.

## Stack Tecnologico

| Capa | Tecnologia |
|------|-----------|
| Backend | ASP.NET Core 8 MVC con Entity Framework Core |
| Base de datos local | SQLite |
| Base de datos produccion | PostgreSQL (Railway) |
| Autenticacion | ASP.NET Core Identity |
| Frontend | Bootstrap 5, DataTables, GSAP 3, CountUp.js, Chart.js |
| IA | Heuristica institucional + OpenAI GPT-4o-mini (opcional) |
| Despliegue | Railway con Docker y GitHub Actions CI |

## Funcionalidad Principal

### Roles y Autenticacion
- Cuatro roles institucionales: `Administrador`, `Autoridad`, `Tutor` (Profesor), `Alumno`.
- Login con bloqueo tras intentos fallidos, recuperacion de contrasena y reCAPTCHA v3.
- Redireccion por rol: el profesor ingresa directamente a su hub de trabajo.

### Hub del Profesor (Mis Grupos)
- Pagina unica de trabajo con todos los recursos del profesor en un solo lugar.
- Tarjetas de grupo con indicadores visuales de riesgo (semaforo por alumno).
- Distribucion de riesgo con barra proporcional (Verde, Amarillo, Rojo).
- Prediccion de desercion por grupo: conteo de probabilidad Alta, Media y Baja.
- Sugerencias inteligentes: muestra los 3 estudiantes en mayor riesgo con botones contextuales ("Crear plan" si no tiene uno, "Ver plan" si ya existe).
- Acciones rapidas: Materias Reprobadas, Intervenciones, Planes de Mejora, Predicciones.

### Plan de Mejora en Un Click
- El sistema genera automaticamente un plan de accion basado en los factores de riesgo del alumno.
- El profesor solo ajusta tipos de intervencion mediante checkboxes (Asesoria academica, Control de ausencias, Regularizacion ETS, Apoyo psicologico, Tutoria personalizada).
- Los checkboxes se pre-seleccionan segun los factores de riesgo detectados.
- Opcion "Personalizar" para editar manualmente el texto del plan.
- Intervenciones vinculadas al plan: cada accion del profesor queda registrada en la linea de tiempo del plan.
- Prevencion de duplicados: no se permiten multiples planes activos para el mismo alumno.

### Prediccion de Desercion
- Sistema de dos capas:
  1. **Heuristica institucional**: reglas deterministicas y auditables que calculan probabilidad en tiempo real.
  2. **OpenAI GPT-4o-mini** (opcional): genera analisis narrativo personalizado con recomendaciones especificas.
- Si la clave de OpenAI no esta configurada, el sistema funciona normalmente con la heuristica base.
- Animacion de tipeo progresivo en la vista de detalle.

### Seguimiento e Intervenciones
- Registro de intervenciones tutoriales vinculadas a planes de mejora.
- Intervenciones independientes para acciones rapidas sin plan.
- Seguimiento con fechas y notas por intervencion.
- Linea de tiempo integrada en el plan de mejora.

### Convocatorias y Postulaciones
- Catalogo de convocatorias con filtros por tipo.
- Validacion de elegibilidad: promedio minimo, semestre, carrera, cupo y carga academica.
- Postulacion con carga de documentos (validacion MIME y tamano maximo 5 MB).
- Gestion de estados: En Revision, Aceptado, Rechazado.

### Reportes y Auditoria
- Tablero estadistico por carrera.
- Exportacion CSV de alumnos, postulaciones y alumnos en riesgo.
- Bitacora de auditoria para mutaciones criticas.
- Reporte institucional imprimible.

### Notificaciones y Diseno
- Sistema de notificaciones con badge en tiempo real.
- Toasts animados con GSAP.
- Componente web `<ra-semaforo>` para indicadores de riesgo.
- Modo oscuro TRUE BLACK para OLED.
- Diseno responsivo (desktop y movil).
- Busqueda global con `Ctrl+K`.

## Reglas de Riesgo y Prediccion

El sistema usa reglas explicables. Los valores exactos estan definidos en `Services/RiskEvaluationService.cs`.

### Riesgo Academico (Verde, Amarillo, Rojo)

Un alumno se clasifica como Rojo si cumple cualquiera de:
- Promedio global menor a 6.0
- Dos o mas materias reprobadas
- Mas de cinco ausencias
- Dos o mas parciales bajos
- Dos o mas ETS presentados
- Dos o mas recursamientos

Amarillo si no es Rojo pero cumple cualquiera de:
- Promedio global menor a 7.5
- Una materia reprobada
- Mas de tres ausencias
- Un parcial bajo
- Un ETS presentado
- Un recursamiento

Verde si no cumple ninguna condicion anterior.

### Probabilidad de Desercion

Inicia en 10% y suma:
- Promedio menor a 6.0: +45%
- Promedio entre 6.0 y 6.99: +25%
- Materias reprobadas: +4% por materia (maximo +20%)
- Recursamientos: +5% por recursamiento (maximo +15%)
- Ausencias: +2% por ausencia (maximo +15%)
- Carga academica de 7 o mas materias: +15%

Resultado redondeado a dos decimales, maximo 95%.

## Cuentas de Demostracion

El sistema incluye datos sembrados con 308 alumnos, 12 profesores y 6 carreras.

| Rol | Correo |
|-----|--------|
| Administrador | `admin@ipn.mx` |
| Autoridad | `autoridad@ipn.mx` |
| Profesor | `profesor1@ipn.mx` |
| Alumno | `demo.alumno@alumno.ipn.mx` |

Las contrasenas se configuran mediante variables de entorno:
- `DEMO_ADMIN_PASSWORD`
- `DEMO_AUTORIDAD_PASSWORD`
- `DEMO_TUTOR_PASSWORD`
- `DEMO_ALUMNO_PASSWORD`

En desarrollo local, los valores por defecto estan en `appsettings.json`. El acordeon de credenciales en la pagina de login se oculta automaticamente en produccion.

## Ejecucion Local

```bash
git clone https://github.com/Xerea/RescateAcademico.git
cd RescateAcademico/RescateAcademico
dotnet run
```

La aplicacion se abre en `https://localhost:5001`. Para regenerar los datos de demostracion, elimina `app.db` y reinicia.

## Despliegue en Railway

El repositorio incluye `Dockerfile` y `railway.toml` para despliegue automatico en Railway.

Variables de entorno requeridas:
- `DATABASE_URL` — conexion PostgreSQL proporcionada por Railway.
- `OPENAI_API_KEY` — clave de API de OpenAI (opcional, el analisis heuristico funciona sin ella).
- `RECAPTCHA_SITE_KEY` y `RECAPTCHA_SECRET_KEY` — claves de reCAPTCHA v3 (opcional, el login funciona sin ellas).

El primer despliegue siembra aproximadamente 300 alumnos, 12 profesores, calificaciones y predicciones de demostracion. El seeding se omite si la base ya contiene datos.

## Arquitectura

```text
.
|-- RescateAcademico/
|   |-- Controllers/        Controladores MVC
|   |-- Data/               DbContext y seeding inicial
|   |-- Filters/            Filtros transversales, auditoria
|   |-- Models/             Entidades de dominio
|   |-- Services/           Reglas de negocio y servicios internos
|   |-- ViewModels/         Contratos de presentacion
|   |-- Views/              Vistas Razor
|   `-- wwwroot/            CSS, JavaScript y activos estaticos
|-- .github/                Workflows CI
|-- docs/                   Documentacion complementaria
|-- Dockerfile              Imagen de produccion
|-- railway.toml            Configuracion de Railway
`-- RescateAcademico.sln    Solucion principal
```

### Servicios

| Servicio | Responsabilidad |
|----------|----------------|
| `RiskEvaluationService` | Calculo unificado de riesgo, probabilidad y sugerencias |
| `StudentAccessService` | Control de acceso centralizado por rol y grupo |
| `ConvocatoriaEligibilityService` | Validacion de requisitos para postulaciones |
| `DesercionPredictionService` | Heuristica de prediccion + integracion OpenAI |
| `AlertasService` | Evaluacion masiva y notificaciones por cambio de riesgo |
| `FileStorageService` | Almacenamiento privado con validacion de archivos |
| `NotificationService` | Creacion de notificaciones en-app |
| `CurrentUserContext` | Acceso a ClaimsPrincipal desde servicios |

## Seguridad

- AntiForgery Token en todos los formularios POST.
- Rate limiting: login (10/min), registro (3/5 min), postulacion (5/min), OpenAI (5/min).
- Cookies HttpOnly, Secure, SameSite=Strict.
- Headers: CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy.
- Proteccion contra Open Redirect con `Url.IsLocalUrl`.
- CSV injection protection en exportaciones.
- Control de acceso IDOR centralizado por rol y grupo.
- Bitacora de auditoria para mutaciones criticas con `[AuditLog]`.

## Verificacion de Calidad

```bash
dotnet build .\RescateAcademico.sln
```

Objetivo: `0 warnings, 0 errors`.

GitHub Actions compila automaticamente en cada push y pull request.

## Equipo

| Integrante | Responsabilidad |
|------------|----------------|
| Sergio | Seguridad, roles, autenticacion y bitacora |
| Sara | Convocatorias, postulaciones y reportes |
| Alejandra | Dashboard, perfil academico y estadisticas |
| Elias | Administracion, seeding y operacion institucional |
| Buenfil | Inteligencia artificial y predicciones |

## Licencia

Consulta [LICENSE.txt](LICENSE.txt).
