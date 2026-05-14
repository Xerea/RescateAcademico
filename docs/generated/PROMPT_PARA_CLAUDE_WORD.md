Genera un documento Word profesional completo para el proyecto "Rescate Académico" del IPN CECyT No. 13 "Ricardo Flores Magón". Usa español formal, formato institucional (colores IPN: guinda #6c1d45, blanco), portada elegante, índice automático, encabezados numerados.

Equipo de desarrollo (5 integrantes):
- Sergio — Seguridad, Roles, Bitácora
- Sara — Convocatorias, Postulaciones, Reportes
- Alejandra — Dashboard, Perfil Académico, Estadísticas
- Elías — Administración, Operación Institucional
- Buenfil — Inteligencia Artificial, Predicciones

El proyecto es una plataforma web ASP.NET Core 8 MVC para monitoreo de riesgo académico de estudiantes del IPN, con 308 estudiantes demo, 12 profesores, 6 carreras, desplegado en Railway con PostgreSQL.

---

ESTRUCTURA DEL DOCUMENTO (en este orden):

## 1. PORTADA DISEÑO FORMAL ELEGANTE
- Título: "Rescate Académico — Plataforma de Monitoreo de Riesgo Académico"
- Subtítulo: "Sistema Web para la Prevención de Deserción Estudiantil"
- Institución: Instituto Politécnico Nacional — CECyT No. 13 "Ricardo Flores Magón"
- Fecha actual
- Nombres completos de los 5 integrantes con sus roles

## 2. ÍNDICE (TABLA DE CONTENIDO)
- Generar automáticamente desde los encabezados del documento

## 3. INTRODUCCIÓN
La deserción escolar es uno de los principales problemas del sistema educativo medio superior en México. En el IPN, los índices de abandono académico representan una preocupación institucional constante. Rescate Académico surge como una solución tecnológica integral para la identificación temprana de estudiantes en riesgo, facilitando intervenciones oportunas por parte del cuerpo docente y las autoridades académicas.

El sistema permite a profesores monitorear el desempeño de sus grupos, crear intervenciones y planes de mejora personalizados, y consultar predicciones de riesgo basadas en inteligencia artificial. Los alumnos pueden consultar su perfil académico completo, simular escenarios de calificaciones, y postularse a convocatorias de proyectos académicos. Las autoridades y administradores cuentan con herramientas de análisis estadístico, reportes exportables y gestión integral del padrón estudiantil.

Desarrollado con ASP.NET Core 8 MVC y Entity Framework Core, el sistema se despliega en Railway con PostgreSQL en producción y SQLite en desarrollo local. La arquitectura incorpora una capa de servicios extraídos siguiendo principios de Clean Architecture, con seguridad reforzada mediante CSRF, rate limiting, CSP, y control de acceso centralizado.

## 4. HOJA DE ALCANCE
El sistema Rescate Académico abarca:
- Módulo de autenticación segura con 4 roles (Administrador, Tutor/Profesor, Alumno, Autoridad)
- Dashboard dinámico por rol con estadísticas en tiempo real
- Perfil académico integral del alumno con semáforo de riesgo
- Sistema de predicción de deserción con heurística institucional + OpenAI GPT-4o-mini
- Registro de intervenciones de tutoría y planes de mejora
- Catálogo de convocatorias y sistema de postulaciones con validación de elegibilidad
- Subida de documentos con validación MIME y almacenamiento privado
- Sistema de notificaciones con badge en tiempo real
- Exportación de datos en CSV con protección contra inyección
- Reporte institucional imprimible
- Bitácora de auditoría para mutaciones críticas
- Panel de administración con CRUD completo
- Despliegue automatizado en Railway con CI/CD desde GitHub

Queda fuera del alcance:
- Aplicación móvil nativa
- Integración con sistemas externos del IPN (TAXCOM, SAES)
- Módulo de reconocimiento facial
- Pasarela de pagos
- Chat en tiempo real

## 5. ANÁLISIS

### Planteamiento del problema
En las instituciones de educación media superior del IPN, los profesores carecen de herramientas centralizadas para identificar tempranamente a estudiantes en riesgo académico. Actualmente, el seguimiento se realiza de forma manual, con expedientes en papel y comunicación informal, lo que provoca:
- Detección tardía de estudiantes con múltiples materias reprobadas
- Falta de trazabilidad en intervenciones de apoyo
- Inexistencia de indicadores predictivos de deserción
- Dificultad para que los alumnos conozcan su situación académica integral
- Procesos de postulación a proyectos académicos desconectados del perfil del estudiante

### Contexto de información
El proyecto se desarrolla para el CECyT No. 13 "Ricardo Flores Magón" del IPN, una institución de nivel medio superior con carreras técnicas. El sistema está diseñado para operar con datos académicos típicos: promedios, calificaciones por materia, inasistencias, evaluaciones parciales, ETS (Exámenes a Título de Suficiencia) y recursamientos. La asignación de estudiantes a profesores se realiza mediante grupos académicos.

### Objetivos y Oportunidades
Objetivo general: Desarrollar una plataforma web integral para el monitoreo, predicción y atención del riesgo académico estudiantil en el IPN.

Objetivos específicos:
1. Implementar un sistema de autenticación segura con roles diferenciados
2. Crear un perfil académico integral con indicadores visuales de riesgo
3. Desarrollar un motor de predicción de deserción con heurística e IA
4. Facilitar la creación de intervenciones y planes de mejora por parte de profesores
5. Digitalizar el proceso de postulación a proyectos y convocatorias académicas
6. Proveer herramientas de análisis estadístico y exportación de datos para autoridades

Oportunidades:
- Reducción del índice de deserción mediante identificación temprana
- Mejora en la comunicación profesor-alumno-autoridad
- Toma de decisiones basada en datos
- Digitalización de procesos administrativos académicos

### Estudio de Factibilidad

#### Factibilidad Técnica
- Stack tecnológico maduro y gratuito (.NET 8, PostgreSQL, SQLite)
- Railway proporciona infraestructura administrada con auto-deploy desde GitHub
- OpenAI API disponible para análisis narrativo (~$0.00030 por estudiante)
- Bibliotecas frontend gratuitas (Bootstrap 5, GSAP, DataTables, Chart.js, CountUp.js)
- El equipo posee conocimientos en C#, ASP.NET, EF Core y desarrollo web
- Conclusión: TOTALMENTE FACTIBLE

#### Factibilidad Económica
- Herramientas de desarrollo gratuitas (Rider Community, VS Code, .NET SDK)
- Railway plan hobby: ~$5/mes para base de datos PostgreSQL
- OpenAI API: ~$0.09 por análisis completo de 300 estudiantes
- Sin costos de licenciamiento
- Dominio y hosting cubiertos por la infraestructura de Railway
- Conclusión: FACTIBLE, costo operativo menor a $10/mes

#### Factibilidad Operativa
- El sistema se integra al flujo existente de profesores y autoridades
- Interfaz intuitiva con diseño responsivo y modo oscuro
- Capacitación mínima requerida (flujo guiado por breadcrumbs)
- Notificaciones automáticas reducen la carga administrativa
- Conclusión: FACTIBLE, curva de aprendizaje baja

#### Factibilidad Legal y Ética
- Datos académicos protegidos conforme a la Ley Federal de Protección de Datos Personales
- Autenticación segura con contraseñas robustas y cookies HttpOnly/Secure
- Auditoría de todas las mutaciones críticas (Bitácora)
- Consentimiento informado para análisis de datos con IA
- No se comparten datos con terceros
- Cumplimiento con políticas institucionales del IPN
- Conclusión: FACTIBLE

#### Factibilidad de Tiempo
- Desarrollo completado: ~9 semanas de trabajo iterativo
- Fase 1 (3 semanas): Autenticación, roles, modelos base, seeding
- Fase 2 (2 semanas): Dashboard, perfil académico, convocatorias
- Fase 3 (2 semanas): Predicción IA, intervenciones, planes de mejora
- Fase 4 (2 semanas): Seguridad, refactorización, pruebas, despliegue
- Conclusión: FACTIBLE, proyecto entregado dentro del semestre

#### Informe del Resultado del Estudio
El proyecto es viable en todas las dimensiones evaluadas. Se recomienda su implementación y despliegue inmediato, con seguimiento post-implementación para incorporar retroalimentación de usuarios reales.

## 6. PRODUCT BACKLOG — LISTADO GENERAL DE HISTORIAS DE USUARIO

| ID | Título | Prioridad | Estado |
|----|--------|-----------|--------|
| HU-RA-01 | Inicio de Sesión Seguro | Alta | ✓ Completado |
| HU-RA-02 | Recuperación de Contraseña | Alta | ✓ Completado |
| HU-RA-03 | Dashboard por Rol | Alta | ✓ Completado |
| HU-RA-04 | Gestión de Roles y Privilegios | Alta | ✓ Completado |
| HU-RA-05 | Prevención de Intrusiones (Bloqueo) | Alta | ✓ Completado |
| HU-RA-06 | Consulta de Situación Académica | Alta | ✓ Completado |
| HU-RA-07 | Catálogo de Convocatorias | Media | ✓ Completado |
| HU-RA-08 | Postulación a Proyectos | Alta | ✓ Completado |
| HU-RA-09 | Notificaciones de Acción | Media | ✓ Completado |
| HU-RA-10 | Monitoreo de Tutorados | Alta | ✓ Completado |
| HU-RA-11 | Semáforo de Rendimiento | Alta | ✓ Completado |
| HU-RA-12 | Carga Masiva de Alumnado | Baja | Pendiente |
| HU-RA-13 | Ajustes de Ciclos Escolares | Media | ✓ Completado |
| HU-RA-14 | Bitácora de Auditoría | Media | ✓ Completado |
| HU-RA-15 | Tablero Estadístico | Media | ✓ Completado |
| HU-RA-16 | Emisión de Reportes Oficiales | Media | ✓ Parcial |
| HU-RA-17 | Sugerencia Optimizada (IA) | Alta | ✓ Completado |
| HU-RA-18 | Prevención de Deserción (IA) | Alta | ✓ Completado |
| HU-RA-19 | Simulador de Elegibilidad (What-If) | Media | ✓ Completado |
| HU-RA-20 | Intervención Temprana | Alta | ✓ Completado |
| HU-RA-21 | Seguimiento de Alertas | Alta | ✓ Completado |
| HU-RA-22 | Evidencia de Postulación (Documentos) | Media | ✓ Completado |
| HU-RA-23 | Evaluación Masiva de Riesgo | Media | ✓ Completado |

## 7. DETALLES DE HISTORIAS DE USUARIO (incluir para las 8 HU más representativas)

[NOTA: Incluye al menos estas 8 HU con el siguiente formato para cada una]

### HU-RA-01: Inicio de Sesión Seguro
- Descripción: Como usuario del sistema, quiero iniciar sesión con mi correo institucional para acceder a las funciones según mi rol.
- Análisis: El sistema debe validar credenciales contra Identity, verificar que la cuenta esté activa y no pendiente de verificación, y bloquear tras 3 intentos fallidos por 20 minutos. Tras autenticación exitosa, el profesor es redirigido directamente a Mis Grupos, los demás roles al Dashboard.
- Diagrama de solución: [Flujo: Login → Validar credenciales → Verificar estado → Bloqueo/Acceso → Redirigir por rol]
- Código de solución: AccountController.Login POST, validación de correo institucional (@ipn.mx, @alumno.ipn.mx), PasswordSignInAsync con lockoutOnFailure: true.
- Parámetros de entrada: email (string), password (string), rememberMe (bool)
- Parámetros de salida: Redirección a Dashboard/Profesor o mensaje de error con conteo de intentos fallidos
- Componentes de BD: AspNetUsers, AspNetRoles, AspNetUserRoles
- Criterios de aceptación:
  1. Usuario con credenciales correctas y cuenta activa accede exitosamente
  2. Usuario con credenciales incorrectas recibe mensaje de error
  3. Tras 3 intentos fallidos, la cuenta se bloquea por 20 minutos
  4. Usuario con cuenta inactiva o pendiente de verificación recibe mensaje apropiado
  5. Profesor es redirigido a Profesor/Index, otros roles a Dashboard/Index
- Matriz de prueba:
  | Caso | Entrada | Resultado esperado | Estado |
  |------|---------|-------------------|--------|
  | Login válido admin | admin@ipn.mx / Admin123! | Redirige a Dashboard | ✓ |
  | Login válido profesor | tutor@ipn.mx / Tutor123! | Redirige a Mis Grupos | ✓ |
  | Login válido alumno | alumno@alumno.ipn.mx / Alumno123! | Redirige a Dashboard | ✓ |
  | Contraseña incorrecta | admin@ipn.mx / wrong | Error + conteo intentos | ✓ |
  | 3 intentos fallidos | ×3 contraseña errónea | Bloqueo 20 min | ✓ |
  | Cuenta inactiva | usuario inactivo | "Cuenta no activa" | ✓ |
- Evidencias de prueba: Captura de pantalla del login exitoso, captura de pantalla del mensaje de bloqueo.

[Repetir este formato para HU-RA-06, HU-RA-08, HU-RA-10, HU-RA-11, HU-RA-18, HU-RA-20, HU-RA-23]

## 8. DISEÑO

### Diagrama Entidad-Relación
Tablas principales y sus relaciones:
- Alumno (Matricula PK, Nombre, Apellidos, Carrera, SemestreActual, PromedioGlobal, RiesgoAcademico, GrupoId FK)
- Calificacion (Id PK, AlumnoMatricula FK, MateriaId FK, Periodo, Valor, Aprobada, Tipo, VecesCursada)
- Materia (Id PK, Clave, Nombre)
- Grupo (Id PK, Clave, Carrera, Semestre, Turno, ProfesorId FK)
- Tutor (Id PK, Nombre, Apellidos, Especialidad, UserId FK, EstaActivo)
- Proyecto (Id PK, Titulo, Tipo, Descripcion)
- Convocatoria (Id PK, Titulo, ProyectoId FK, CupoMaximo, PostulacionesActuales, FechaCierre, PromedioMinimo, SemestreMinimo, CarreraRequerida, EstaActiva, ValidadaPorAcademia)
- Postulacion (Id PK, AlumnoId FK, ProyectoId FK, FechaSolicitud, Estado, DocumentoNombre, DocumentoRuta)
- IntervencionTutoria (Id PK, AlumnoMatricula FK, TutorId FK, Tipo, Descripcion, Fecha, RequiereSeguimiento)
- PlanMejora (Id PK, AlumnoMatricula FK, TutorId FK, Recomendaciones, Metas, Estado, FechaCreacion, FechaCierre)
- PrediccionDesercion (Id PK, AlumnoMatricula FK, ProbabilidadDesercion, NivelRiesgo, FactoresDetectados, FechaPrediccion)
- Notificacion (Id PK, UserId FK, Titulo, Mensaje, Tipo, Leida, FechaCreacion)
- BitacoraLog (Id PK, UsuarioId, Accion, Tabla, Fecha, Detalles)
- AspNetUsers, AspNetRoles, AspNetUserRoles (Identity)

Relaciones principales:
- Alumno 1:N Calificacion
- Alumno N:1 Grupo
- Grupo N:1 Tutor
- Materia 1:N Calificacion
- Proyecto 1:N Convocatoria
- Convocatoria 1:N Postulacion
- Alumno 1:N Postulacion
- Tutor 1:N IntervencionTutoria
- Alumno 1:N IntervencionTutoria
- Alumno 1:N PlanMejora
- Tutor 1:N PlanMejora
- Alumno 1:N PrediccionDesercion

### Diagrama de Clases
[Incluir aquí el diagrama de clases UML mostrando los modelos principales, servicios, y controladores con sus dependencias]

## 9. DESARROLLO

### Stack Tecnológico
- Backend: ASP.NET Core 8 MVC (.NET 8.0)
- ORM: Entity Framework Core 8.0
- Base de datos local: SQLite
- Base de datos producción: PostgreSQL (Railway)
- Autenticación: ASP.NET Core Identity
- Frontend: Bootstrap 5 + DataTables 1.13 + GSAP 3.12 + CountUp.js + Chart.js
- IA: OpenAI GPT-4o-mini (análisis narrativo) + heurística institucional
- Despliegue: Railway con Docker y GitHub auto-deploy
- Control de versiones: Git + GitHub
- IDE: JetBrains Rider

### Servicios Extraídos (Clean Architecture)
- CurrentUserContext: encapsula acceso a ClaimsPrincipal desde HttpContext
- StudentAccessService: control de acceso centralizado — ApplyVisibleStudents(), CanAccessAlumnoAsync(), GetVisibleMatriculasAsync()
- RiskEvaluationService: lógica unificada de riesgo — CalcularRiesgo(), CalcularProbabilidadDesercion(), CalcularNivelPredictivo(), ObtenerFactoresRiesgo(), GenerarSugerencias()
- ConvocatoriaEligibilityService: validación de elegibilidad para postulaciones con patrón EligibilityResult
- NotificationService: creación de notificaciones con patrón unit-of-work
- FileStorageService: almacenamiento privado con validación MIME y extensión
- AlertasService: evaluación masiva y notificación de cambios de riesgo
- DesercionPredictionService: heurística de predicción + integración OpenAI GPT-4o-mini

### Seguridad Implementada
- AntiForgery Token global (AutoValidateAntiforgeryTokenAttribute)
- Rate Limiting: 4 políticas (login 10/min, registro 3/5min, postulación 5/min, OpenAI 5/min)
- CSP con orígenes permitidos (Google Fonts, CDNJS, jsDelivr, DataTables CDN)
- Headers: HSTS, X-Frame-Options: DENY, X-Content-Type-Options: nosniff, Referrer-Policy, Permissions-Policy
- Cookies: HttpOnly, Secure, SameSite=Strict
- Protección Open Redirect (Url.IsLocalUrl)
- Protección IDOR centralizada (StudentAccessService)
- Auditoría: [AuditLog] en mutaciones críticas
- XSS: textContent en modales AJAX, JsonSerializer.Serialize en Chart.js
- CSV injection protection

### Queries de BD Relevantes
```sql
-- Estudiantes en riesgo por grupo
SELECT g.Clave, COUNT(*) as Total, 
  SUM(CASE WHEN a.RiesgoAcademico = 'Rojo' THEN 1 ELSE 0 END) as EnRojo,
  SUM(CASE WHEN a.RiesgoAcademico = 'Amarillo' THEN 1 ELSE 0 END) as EnAmarillo
FROM Alumnos a JOIN Grupos g ON a.GrupoId = g.Id
WHERE g.ProfesorId = @profesorId
GROUP BY g.Clave;

-- Materias más reprobadas por grupo
SELECT m.Nombre, COUNT(*) as Reprobadas
FROM Calificaciones c 
JOIN Materias m ON c.MateriaId = m.Id
JOIN Alumnos a ON c.AlumnoMatricula = a.Matricula
WHERE a.GrupoId = @grupoId AND c.Aprobada = 0
GROUP BY m.Nombre ORDER BY Reprobadas DESC;

-- Tendencia de riesgo por período
SELECT c.Periodo, AVG(a.PromedioGlobal) as PromedioGeneral,
  SUM(CASE WHEN a.RiesgoAcademico = 'Rojo' THEN 1 ELSE 0 END) as Rojos
FROM Alumnos a
JOIN Calificaciones c ON a.Matricula = c.AlumnoMatricula
GROUP BY c.Periodo ORDER BY c.Periodo;
```

### Código Completo
El código fuente completo (~16,000 líneas) está disponible en:
https://github.com/Xerea/RescateAcademico
El sistema compila con 0 warnings y 0 errores.

## 10. COMERCIAL / LINK DE YOUTUBE
[Incluir enlace al video demostrativo del sistema — grabar un recorrido de 5-8 minutos mostrando el flujo completo: login de los 4 roles, dashboard, perfil académico, predicción IA, creación de plan de mejora, postulación a convocatoria]

## 11. MATRIZ DE PRUEBAS GENERAL

| ID | Módulo | Prueba | Resultado Esperado | Estado |
|----|--------|--------|-------------------|--------|
| P01 | Login | Iniciar sesión con credenciales válidas | Acceso al sistema | ✓ |
| P02 | Login | Contraseña incorrecta | Error + conteo de intentos | ✓ |
| P03 | Login | 3 intentos fallidos | Bloqueo 20 min | ✓ |
| P04 | Roles | Login como Admin | Acceso a Gestión, Reportes | ✓ |
| P05 | Roles | Login como Tutor | Redirige a Mis Grupos, sin navbar extra | ✓ |
| P06 | Roles | Login como Alumno | Perfil académico, Convocatorias | ✓ |
| P07 | Roles | Login como Autoridad | Alumnos, Reportes, Estadísticas | ✓ |
| P08 | Dashboard | Carga de estadísticas | Tarjetas con conteos reales | ✓ |
| P09 | Perfil | Ver perfil académico | Promedio, semáforo, materias | ✓ |
| P10 | IA | Predicción de deserción | Porcentaje y nivel de riesgo | ✓ |
| P11 | IA | Análisis OpenAI | Respuesta estructurada con tipeo | ✓* |
| P12 | IA | Sin API key configurada | Mensaje informativo, sin error | ✓ |
| P13 | Profesor | Ver Mis Grupos | Tarjetas de grupo con riesgo | ✓ |
| P14 | Profesor | Ver estudiantes de grupo | Tabla filtrada por grupo | ✓ |
| P15 | Profesor | Crear plan de mejora | Plan creado, notificación al alumno | ✓ |
| P16 | Profesor | Tutor bloqueado en plan | No puede asignar a otro tutor | ✓ |
| P17 | Alumno | Simulador What-If | Cambia promedio y riesgo en tiempo real | ✓ |
| P18 | Postulación | Postularse a convocatoria | Validación de elegibilidad | ✓ |
| P19 | Postulación | Cupo excedido | Mensaje de error | ✓ |
| P20 | Postulación | Subir documento | Archivo guardado, nombre seguro | ✓ |
| P21 | Postulación | Archivo no permitido | Error de validación MIME | ✓ |
| P22 | Export | CSV de alumnos | Descarga con datos correctos | ✓ |
| P23 | Seguridad | CSRF sin token | Solicitud rechazada | ✓ |
| P24 | Seguridad | Acceso a alumno de otro grupo (Tutor) | Acceso denegado (403) | ✓ |
| P25 | UI | Modo oscuro | Cambio correcto en todas las vistas | ✓ |
| P26 | UI | DataTables | Ordenamiento, búsqueda, paginación | ✓ |
| P27 | UI | Quick View modal | Carga datos sin congelar pantalla | ✓ |

*Requiere OPENAI_API_KEY configurada

## 12. EVIDENCIAS DE PRUEBAS GENERAL
[Incluir capturas de pantalla para cada escenario de prueba:
1. Login exitoso Admin
2. Login bloqueado tras 3 intentos
3. Dashboard Admin con estadísticas
4. Mis Grupos con tarjetas y predicciones
5. Perfil académico con semáforo y probabilidad
6. Predicción IA con análisis narrativo
7. Plan de mejora creado
8. Postulación exitosa
9. CSV exportado
10. Modo oscuro activado]

## 13. MANUAL DE USUARIO

### Acceso al Sistema
1. Abrir el navegador y acceder a la URL del sistema
2. En la página de login, ingresar correo institucional y contraseña
3. Si es la primera vez, hacer clic en "Registrarse" y completar el formulario

### Para Profesores (Tutores)
1. Al iniciar sesión, verá la página "Mis Grupos"
2. Revise la distribución de riesgo y las predicciones IA de sus estudiantes
3. Haga clic en un grupo para ver la lista de estudiantes
4. Use el ícono de ojo (👁) para vista rápida de un estudiante
5. Use el ícono de gráfica (📊) para ver el historial académico completo
6. Desde el historial, puede crear un plan de mejora o ver el análisis IA
7. Use las Acciones Rápidas para ver materias reprobadas, intervenciones y planes

### Para Alumnos
1. Al iniciar sesión, verá el Panel con resumen de su situación
2. En "Mi Perfil Académico" puede consultar su promedio, riesgo y materias
3. Use el simulador "¿Cómo me va?" para proyectar escenarios
4. En "Convocatorias" explore proyectos disponibles y postúlese
5. En "Mis Postulaciones" revise el estado de sus solicitudes

### Para Autoridades
1. Al iniciar sesión, acceda al Panel con estadísticas generales
2. En "Alumnos" puede filtrar por carrera, semestre y nivel de riesgo
3. En "Reportes" encontrará Estadísticas, Predicciones IA y Exportes
4. Use los exportes CSV para análisis externos

### Para Administradores
1. Al iniciar sesión, acceda al Panel y a "Gestión"
2. Gestión tiene pestañas para: Usuarios, Alumnos, Profesores, Académico, Operaciones, Sistema
3. Desde Operaciones puede ejecutar evaluación masiva de riesgos
4. Use "Integridad de Datos" para verificar consistencia

## 14. MANUAL TÉCNICO

### Requisitos del Sistema
- .NET 8 SDK
- SQLite (desarrollo) o PostgreSQL (producción)
- Docker (para despliegue en Railway)
- Git

### Instalación Local
```bash
git clone https://github.com/Xerea/RescateAcademico.git
cd RescateAcademico/RescateAcademico
dotnet restore
dotnet build
dotnet run
```
El sistema estará disponible en https://localhost:5001

### Estructura del Proyecto
```
RescateAcademico/
├── Controllers/     (15 controladores)
├── Models/          (18+ modelos)
├── Views/           (60+ vistas Razor)
├── Services/        (8 servicios extraídos)
├── Data/            (DbContext + Seeder)
├── Filters/         (AuditLog)
├── wwwroot/         (CSS, JS, librerías)
├── Program.cs       (punto de entrada, configuración)
└── appsettings.json (configuración local)
```

### Configuración de Variables de Entorno
| Variable | Propósito | Requerida |
|----------|-----------|-----------|
| DATABASE_URL | Conexión PostgreSQL (Railway) | Producción |
| OPENAI_API_KEY | API key de OpenAI para análisis IA | Opcional |
| DEMO_ADMIN_PASSWORD | Contraseña del admin demo | Desarrollo |
| RAILWAY_ENVIRONMENT | Detecta entorno Railway | Producción |
| UPLOADS_PATH | Ruta de almacenamiento de archivos | Opcional |

### Despliegue en Railway
1. Conectar repositorio GitHub a Railway
2. Railway detecta automáticamente el Dockerfile y railway.toml
3. Agregar variables de entorno en el dashboard de Railway
4. El deployment se ejecuta automáticamente en cada push a main
5. El primer deploy tarda 8-10 minutos por el seeding de datos

### Base de Datos
- EnsureCreated() en startup crea las tablas automáticamente
- DemoDataSeeder siembra 308 estudiantes, 12 profesores, 6 carreras
- Guardia: si Alumnos.Count >= 50, omite el seeding
- Sin migraciones EF — cambios de esquema requieren recrear la BD

## 15. PLAN DE IMPLEMENTACIÓN

### Fase 1: Preparación (Semana 1)
- Configurar repositorio Git y CI/CD en Railway
- Configurar variables de entorno
- Verificar despliegue inicial y seeding

### Fase 2: Capacitación (Semana 2)
- Sesión de capacitación para 12 profesores (1 hora)
- Entrega de credenciales individuales
- Guía rápida impresa con flujos principales

### Fase 3: Implementación Piloto (Semanas 3-4)
- Operación con datos reales de 1 grupo piloto
- Registro de feedback de profesores
- Ajustes menores basados en retroalimentación

### Fase 4: Despliegue General (Semana 5)
- Alta de todos los grupos y estudiantes
- Comunicación oficial a la comunidad académica
- Soporte técnico durante el primer mes

## 16. CARTA DE TERMINACIÓN
[Incluir carta formal firmada por el equipo de desarrollo y el profesor asesor, certificando que el proyecto ha sido completado satisfactoriamente con todas las funcionalidades descritas en el alcance, cumpliendo con los requisitos académicos de la asignatura de Sistema de Información del IPN CECyT No. 13.]

## 17. REFERENCIAS BIBLIOGRÁFICAS
1. Microsoft. (2024). ASP.NET Core 8 Documentation. https://learn.microsoft.com/en-us/aspnet/core/
2. Entity Framework Core. (2024). EF Core 8 Documentation. https://learn.microsoft.com/en-us/ef/core/
3. OpenAI. (2024). GPT-4o-mini API Documentation. https://platform.openai.com/docs/
4. Bootstrap. (2024). Bootstrap 5.3 Documentation. https://getbootstrap.com/docs/5.3/
5. DataTables. (2024). DataTables 1.13 Documentation. https://datatables.net/
6. GreenSock. (2024). GSAP 3 Documentation. https://gsap.com/docs/
7. Railway. (2024). Railway Deployment Documentation. https://docs.railway.app/
8. PostgreSQL. (2024). PostgreSQL 16 Documentation. https://www.postgresql.org/docs/
9. Fowler, M. (2012). Patterns of Enterprise Application Architecture. Addison-Wesley.
10. Instituto Politécnico Nacional. (2024). Reglamento Académico del IPN. https://www.ipn.mx/

## 18. PLANIFICACIÓN DEL MANTENIMIENTO PREVENTIVO

### Mantenimiento Mensual
- Monitoreo de logs de error en Railway
- Verificación de estado de la base de datos (conexiones, espacio)
- Revisión de uso de API de OpenAI (costos)
- Backup de base de datos PostgreSQL

### Mantenimiento Trimestral
- Actualización de paquetes NuGet
- Revisión de seguridad (dependencias, vulnerabilidades)
- Limpieza de notificaciones antiguas (>30 días)
- Optimización de índices de base de datos

### Mantenimiento Semestral
- Renovación de API key de OpenAI
- Actualización del framework (.NET 8 → .NET 9 cuando disponible)
- Revisión de contraseñas de demo
- Pruebas de rendimiento con datos reales

## 19. FORMATO DE BITÁCORA DE MANTENIMIENTO CORRECTIVO

| Fecha | Hora | Responsable | Incidencia | Causa | Acción Correctiva | Tiempo de Resolución | Estado |
|-------|------|-------------|------------|-------|-------------------|---------------------|--------|
| | | | | | | | |

[Incluir formato de tabla vacía para registro futuro]

## 20. CONCLUSIONES POR INTEGRANTE

### Sergio — Seguridad, Roles y Bitácora
Mi responsabilidad principal fue implementar y verificar la capa de seguridad del sistema. Configuré ASP.NET Core Identity con bloqueo tras 3 intentos fallidos (20 minutos), políticas de contraseñas robustas que exigen 8 caracteres con mayúsculas, minúsculas, dígitos, caracteres especiales y 4 caracteres únicos. Implementé cookies HttpOnly, Secure y SameSite=Strict para proteger las sesiones. Agregué rate limiting en 4 endpoints críticos (login 10/min, registro 3/5min, postulación 5/min, análisis IA 5/min) para prevenir abusos y proteger los créditos de la API de OpenAI. Configuré headers de seguridad (CSP, HSTS, X-Frame-Options: DENY, X-Content-Type-Options: nosniff, Referrer-Policy, Permissions-Policy) y protección contra Open Redirect usando Url.IsLocalUrl. La bitácora de auditoría registra automáticamente todas las mutaciones críticas mediante el filtro [AuditLog], proporcionando trazabilidad completa de quién hizo qué y cuándo. El mayor desafío fue configurar correctamente la Content Security Policy para permitir los CDNs externos (Google Fonts, CDNJS para GSAP, jsDelivr para DataTables) sin romper la funcionalidad del frontend. Aprendí que la seguridad en aplicaciones web no es una característica que se agrega al final, sino una capa transversal que debe considerarse desde la arquitectura inicial: cada controlador, cada formulario POST, cada cookie y cada header tiene implicaciones de seguridad que afectan la integridad de todo el sistema. También comprendí la importancia del control de acceso centralizado — mover la lógica de visibilidad de estudiantes a StudentAccessService eliminó la duplicación de verificaciones IDOR que existía en múltiples controladores.

### Sara — Convocatorias, Postulaciones y Reportes
Desarrollé el flujo completo de convocatorias y postulaciones académicas. Implementé el catálogo de convocatorias con filtros por tipo, el formulario de postulación con validación integral de elegibilidad (promedio mínimo, semestre requerido, carrera específica, cupo disponible y carga académica del alumno), y la gestión de estados de postulación (En Revisión, Aceptado, Rechazado) con notificaciones automáticas al alumno en cada cambio de estado. Un logro importante fue extraer la lógica de elegibilidad al servicio ConvocatoriaEligibilityService, garantizando que las mismas reglas de negocio se apliquen tanto en la vista GET del formulario como en el procesamiento POST, cerrando una vulnerabilidad donde un alumno podía omitir validaciones enviando una solicitud POST directa. El sistema de almacenamiento de archivos usa FileStorageService con validación de tipo MIME y extensión, almacenamiento privado fuera de wwwroot (configurable mediante variables de entorno), y nombres de archivo seguros basados en GUID. En el módulo de reportes, implementé la exportación CSV con protección contra inyección de fórmulas (prefijado de apóstrofe para valores iniciados con =, +, -, @, y escapado de tabulaciones y retornos de carro). El mayor reto fue la refactorización de las reglas de elegibilidad, que originalmente estaban duplicadas en tres lugares distintos con ligeras variaciones. Aprendí que la validación del lado del servidor nunca debe confiar en la validación del lado del cliente, y que centralizar las reglas de negocio en servicios especializados previene inconsistencias y vulnerabilidades.

### Alejandra — Dashboard, Perfil Académico y Estadísticas
Mi trabajo se centró en diseñar e implementar la experiencia del usuario final en el sistema. Creé el dashboard dinámico que muestra información y accesos rápidos diferentes según el rol del usuario: el Administrador ve métricas institucionales y accesos a gestión; el Profesor ve—de hecho, es redirigido directamente a—la página de Mis Grupos con predicciones IA, resumen de riesgo y sugerencias inteligentes; el Alumno ve un resumen de su situación académica con semáforo de riesgo; y la Autoridad accede a estadísticas institucionales, catálogo de alumnos y reportes. Implementé el perfil académico completo del alumno que incluye promedio global con indicador de color, promedio histórico por período, conteo de materias aprobadas, reprobadas, ETS presentados y recursamientos, semáforo de riesgo, sugerencias personalizadas generadas por IA, y el simulador "What-If" donde el alumno puede modificar sus calificaciones hipotéticas y ver en tiempo real cómo cambia su promedio, nivel de riesgo y elegibilidad para convocatorias. El mayor desafío fue el rediseño completo del hub de profesor: lo que comenzó como una página con solo dos botones evolucionó a un panel integral que muestra distribución de riesgo con barra visual, tarjetas de grupo con indicadores de riesgo, predicciones de deserción por grupo (alta/media/baja), sugerencias inteligentes con enlaces contextuales, y un panel de acciones rápidas. También trabajé en la migración completa de 60+ vistas al sistema de diseño ra-* con soporte para modo oscuro TRUE BLACK. Aprendí que la mejor interfaz es aquella que anticipa lo que el usuario necesita hacer y elimina pasos innecesarios — el profesor ahora ve todo en un solo lugar sin navegar entre múltiples páginas.

### Elías — Administración y Operación Institucional
Implementé el panel de administración completo con operaciones CRUD para todas las entidades del sistema: alumnos, tutores, carreras, ciclos escolares y usuarios. Desarrollé la gestión de grupos académicos con asignación de profesores, donde cada grupo pertenece a un semestre, turno y carrera específicos. Trabajé en el sistema de notificaciones con badge en tiempo real — un contador que se actualiza cada 30 segundos mediante polling asíncrono y muestra el número de notificaciones no leídas. Configuré el seeding de datos demo con 308 estudiantes distribuidos en 20 grupos, 12 profesores, 6 carreras técnicas y calificaciones para 4 períodos académicos, generando un conjunto de datos realista con distribución variada de promedios y niveles de riesgo. También implementé la vista de Integridad de Datos, que al iniciar la aplicación evalúa y corrige automáticamente cualquier estudiante cuyo RiesgoAcademico esté nulo. El mayor reto fue la transición del modelo de asignación de tutorados: originalmente el sistema usaba una tabla AsignacionesTutor que asignaba aleatoriamente profesores a estudiantes, pero migramos a un modelo basado en grupos (Grupo.ProfesorId) que refleja mejor la realidad institucional. Esto requirió actualizar todas las consultas de notificación, el dashboard de profesor, y el control de acceso para que usaran consistentemente el modelo de grupos. Aprendí que en sistemas de información institucionales, la integridad referencial y la consistencia del modelo de datos son la base sobre la que se construye toda la funcionalidad — un error en el modelo se propaga a todas las capas superiores.

### Buenfil — Inteligencia Artificial y Predicciones
Desarrollé el sistema de predicción de deserción académica con un enfoque de dos capas complementarias. La primera capa es una heurística institucional basada en reglas explícitas y auditables: calcula una probabilidad de deserción (0-95%) ponderando el promedio global, materias reprobadas, ausencias, parciales bajos, ETS presentados y recursamientos, con umbrales calibrados según criterios académicos del IPN. Esta heurística es instantánea, no requiere API externa, y funciona incluso sin conexión. La segunda capa es una integración con OpenAI GPT-4o-mini que genera un análisis narrativo personalizado para cada estudiante: un prompt estructurado envía el perfil académico completo al modelo, y la respuesta se parsea en secciones (RESUMEN_RIESGO, ANALISIS, RECOMENDACIONES, ALERTAS) que se muestran con una animación de tipeo progresivo estilo Cursor en la interfaz. Implementé la persistencia de predicciones con auditoría — cada análisis guardado queda registrado en PrediccionesDesercion con el filtro [AuditLog]. También desarrollé el fallback graceful: cuando la variable OPENAI_API_KEY no está configurada, la interfaz muestra un mensaje informativo claro ("La clave de API de OpenAI no está configurada. El análisis heurístico institucional sigue activo.") en lugar de un error confuso o un botón que falla. El mayor desafío fue diseñar prompts efectivos que produzcan respuestas consistentes en español mexicano con un tono profesional pero cercano, y que el formato estructurado de la respuesta sea parseable de manera confiable. Aprendí que la inteligencia artificial en aplicaciones educativas debe ser una capa de valor agregado, no un reemplazo de la lógica de negocio — la heurística base proporciona predicciones consistentes y explicables, mientras que la IA añade narrativa, contexto y recomendaciones personalizadas que enriquecen la experiencia sin comprometer la confiabilidad del sistema.

---

## 21. CONCLUSIONES GENERALES DEL EQUIPO

El desarrollo de Rescate Académico representó un aprendizaje integral que abarcó desde la arquitectura de software hasta el despliegue en producción. Comprobamos que un sistema bien diseñado puede tener un impacto real en la operación académica: nuestros profesores ahora pueden identificar estudiantes en riesgo en segundos, cuando antes les tomaba horas revisar expedientes manualmente. La combinación de reglas heurísticas con inteligencia artificial generativa demostró ser un enfoque equilibrado que proporciona tanto confiabilidad como profundidad analítica. La experiencia de desplegar en Railway nos enseñó la importancia de la configuración de infraestructura como código (Dockerfile, railway.toml) y la gestión adecuada de variables de entorno. Si bien el proyecto cumple con los objetivos planteados, identificamos áreas de mejora futura: la adición de pruebas automatizadas, la migración a paginación server-side para escalar a miles de estudiantes, y la implementación de notificaciones por correo electrónico real. El proyecto nos deja como principales lecciones que la seguridad debe ser transversal desde el diseño inicial, que centralizar la lógica de negocio en servicios previene inconsistencias, y que la experiencia de usuario —eliminar pasos innecesarios y anticipar necesidades— es tan importante como la funcionalidad técnica.

---

FIN DEL DOCUMENTO.
