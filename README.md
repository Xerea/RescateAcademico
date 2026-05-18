# Rescate Académico

Sistema web institucional para monitoreo de riesgo académico, seguimiento tutorial, planes de mejora, convocatorias y reportes operativos para CECyT No. 13 del Instituto Politécnico Nacional.

Rescate Académico está construido como una aplicación ASP.NET Core MVC con autenticación por roles, dashboards por perfil de usuario y reglas transparentes para clasificar riesgo académico y probabilidad de deserción.

## Estado del Proyecto

Este repositorio contiene la versión MVC/Razor actualmente preparada para despliegue en Railway.

- Plataforma principal: ASP.NET Core 8 MVC.
- Base local: SQLite.
- Base en producción: PostgreSQL vía `DATABASE_URL`.
- Despliegue: Dockerfile + Railway.
- Datos iniciales: seeding institucional simulado para demostración y validación funcional.
- Predicción de deserción: heurística institucional determinística.
- IA opcional: OpenAI para análisis narrativo, no para decidir el riesgo final.

## Funcionalidad Principal

- Autenticación con ASP.NET Identity, bloqueo por intentos fallidos y roles institucionales.
- Roles: `Administrador`, `Autoridad`, `Tutor`, `Alumno`.
- Paneles diferenciados por rol.
- Semáforo de riesgo académico: `Verde`, `Amarillo`, `Rojo`.
- Probabilidad de deserción calculada con reglas auditables.
- Seguimiento de estudiantes por grupo asignado al profesor.
- Intervenciones tutoriales y planes de mejora.
- Convocatorias, postulaciones y validación de elegibilidad.
- Notificaciones in-app con panel ligero y soporte opcional para notificaciones del navegador.
- Reportes, estadísticas, exportaciones CSV y bitácora de auditoría.
- Dashboard de integridad de datos para detectar inconsistencias operativas.

## Arquitectura

```text
.
|-- RescateAcademico/       Aplicación ASP.NET Core MVC
|   |-- Controllers/        Controladores MVC
|   |-- Data/               DbContext y seeding inicial
|   |-- Filters/            Filtros transversales, auditoría
|   |-- Models/             Entidades de dominio
|   |-- Services/           Reglas de negocio y servicios internos
|   |-- ViewModels/         Contratos de presentación
|   |-- Views/              Vistas Razor
|   `-- wwwroot/            CSS, JavaScript y activos estáticos
|-- .github/                Workflows y plantillas del repositorio
|-- docs/                   Documentación complementaria
|-- Dockerfile              Imagen de producción
|-- railway.toml            Configuración de Railway
|-- global.json             Versión del SDK .NET
`-- RescateAcademico.sln    Solución principal
```

## Reglas de Riesgo y Predicción

El sistema usa reglas explicables, no un modelo opaco.

### Riesgo Académico

Un alumno se clasifica como `Rojo` si cumple cualquiera de estas condiciones:

- Promedio global menor a `6.0`.
- Dos o más materias reprobadas.
- Más de cinco ausencias.
- Dos o más parciales bajos.
- Dos o más ETS presentados.
- Dos o más recursamientos.

Si no es `Rojo`, se clasifica como `Amarillo` si cumple cualquiera de estas condiciones:

- Promedio global menor a `7.5`.
- Una materia reprobada.
- Más de tres ausencias.
- Un parcial bajo.
- Un ETS presentado.
- Un recursamiento.

Si no cumple ninguna condición anterior, se clasifica como `Verde`.

### Probabilidad de Deserción

La probabilidad inicia en `10%` y suma pesos por indicadores académicos:

- Promedio menor a `6.0`: `+45%`.
- Promedio entre `6.0` y `6.99`: `+25%`.
- Materias reprobadas: `+4%` por materia, hasta `20%`.
- Recursamientos: `+5%` por recursamiento, hasta `15%`.
- Ausencias: `+2%` por ausencia, hasta `15%`.
- Carga académica de 7 o más materias: `+15%`.

El resultado se redondea a dos decimales y se limita a un máximo de `95%`.

La IA, cuando está configurada, genera una explicación narrativa basada en estos datos. No reemplaza las reglas institucionales.

## Seguridad y Control de Acceso

- Antiforgery global para formularios y endpoints POST.
- Rate limiting para login, registro, postulaciones y acciones de IA.
- Cookies `HttpOnly`, `Secure` y `SameSite=Strict`.
- CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy y Permissions-Policy.
- Validación de archivos por extensión, MIME type y tamaño máximo.
- Prevención de open redirects con `Url.IsLocalUrl`.
- Escapado en exportaciones CSV contra formula injection.
- Bitácora de auditoría para mutaciones críticas.
- Acceso a estudiantes centralizado por rol:
  - Administrador y Autoridad: visibilidad institucional.
  - Tutor: solo alumnos de sus grupos asignados.
  - Alumno: solo su propio expediente.

## Despliegue

El despliegue esperado es Railway mediante el `Dockerfile` del repositorio.

Railway debe proveer una base PostgreSQL y la variable `DATABASE_URL`. La aplicación detecta esa variable y usa Npgsql automáticamente. Si `DATABASE_URL` no existe, usa SQLite local configurado en `appsettings.json`.

El primer arranque puede tardar más de lo normal si la base está vacía, porque se crean tablas y se siembran datos de demostración.

## Variables de Entorno

| Variable | Uso |
| --- | --- |
| `DATABASE_URL` | Conexión PostgreSQL en Railway. |
| `OPENAI_API_KEY` | Habilita análisis narrativo con IA. |
| `RECAPTCHA_SITE_KEY` | Llave pública de reCAPTCHA v3. |
| `RECAPTCHA_SECRET_KEY` | Llave privada de reCAPTCHA v3. |
| `ENFORCE_RECAPTCHA` | Si es `true`, reCAPTCHA bloquea login cuando falla. |
| `SHOW_DEMO_CREDENTIALS` | Si es `true`, muestra credenciales demo en ambientes no locales. |
| `DEMO_ADMIN_PASSWORD` | Contraseña semilla para administrador demo. |
| `DEMO_AUTORIDAD_PASSWORD` | Contraseña semilla para autoridad demo. |
| `DEMO_TUTOR_PASSWORD` | Contraseña semilla para profesores demo. |
| `DEMO_ALUMNO_PASSWORD` | Contraseña semilla para alumnos demo. |
| `UPLOADS_PATH` | Ruta alternativa para almacenamiento privado de archivos. |
| `RAILWAY_VOLUME_MOUNT_PATH` | Volumen persistente en Railway para archivos subidos. |

## Credenciales y Datos de Demostración

Este README no publica credenciales operativas.

Las credenciales de demostración son configurables por variables de entorno y solo deben mostrarse desde la aplicación en ambientes controlados. En producción, el acordeón de credenciales demo debe permanecer oculto salvo que se active explícitamente con `SHOW_DEMO_CREDENTIALS=true`.

Los datos sembrados son simulados y sirven para validar flujos, roles, reportes y comportamiento del sistema. No deben presentarse como datos reales de SAES ni como información académica oficial.

## Verificación de Calidad

Comando principal de validación:

```bash
dotnet build .\RescateAcademico.sln
```

El objetivo para cada cambio listo para despliegue es:

```text
0 warnings
0 errors
```

El repositorio incluye GitHub Actions para compilar la solución en `master` y en pull requests contra `master`.

## Consideraciones Operativas

- No se usan migraciones EF en el flujo actual; el esquema se crea con `EnsureCreated`.
- El seeding se omite si la base ya contiene suficientes alumnos.
- Los archivos subidos deben almacenarse en ruta privada o volumen persistente, no como activos públicos.
- Las notificaciones del navegador dependen del permiso otorgado por el usuario y de soporte del navegador.
- El análisis con OpenAI debe tratarse como apoyo narrativo, no como decisión automática final.

## Licencia

Consulta [LICENSE.txt](LICENSE.txt).
