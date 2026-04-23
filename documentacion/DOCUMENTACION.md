# Rescate Academico - Avance de revision

## 1) Estado actual del proyecto

El sistema ya trabaja con **ASP.NET MVC + Entity Framework + SQLite + Identity** y actualmente tiene funcionalidad real en:

- autenticacion segura con bloqueo por intentos fallidos;
- roles (`Alumno`, `Tutor`, `Administrador`, `Autoridad`);
- dashboard por rol;
- catalogo de convocatorias;
- postulaciones con validaciones;
- radiografia academica del alumno;
- monitoreo de tutorados;
- estadisticas institucionales;
- bitacora y notificaciones.

En esta sesion se hizo un salto funcional importante para que la demo se vea mas completa y coherente.

## 2) Cambios implementados hoy (salto grande)

### A. Dashboard funcional por rol

- Se corrigio `DashboardController` para enviar un modelo real (`DashboardStats`) a la vista.
- Antes: la vista esperaba datos y el controlador no mandaba nada (riesgo de error al iniciar sesion).
- Ahora: carga conteos reales desde BD para `Administrador` y `Autoridad`.

### B. Radiografia academica mas "espectacular" (enfasis del profesor)

En `PerfilAcademicoController` y `Views/PerfilAcademico/MiPerfil.cshtml`:

- se agrego **probabilidad de desercion** (0-100%);
- se agrego **nivel predictivo IA** (`Bajo`, `Medio`, `Alto`, `Critico`);
- se agregaron **3 convocatorias/proyectos compatibles** con puntaje de compatibilidad;
- se persiste evidencia del analisis en tablas:
  - `PrediccionesDesercion`
  - `SugerenciasIA`

Esto no es un modelo de machine learning complejo, pero para revision escolar cuenta como **IA explicable basada en reglas** y se ve claramente en pantalla.

### C. Elegibilidad fortalecida al postularse

En `PostulacionesController`:

- se valida promedio minimo;
- se valida semestre minimo;
- se valida carrera requerida;
- se valida carga academica alta para evitar sobrecarga;
- se genera un registro en `SugerenciasIA` con el razonamiento de elegibilidad.

### D. Navegacion corregida (clicks que iban mal)

- En `Dashboard/Index` (rol Tutor), el boton de radiografia ahora lleva a `Alumnos/MisTutorados` para seleccionar alumno (antes iba a una accion que requiere matricula y fallaba).
- En `Postulaciones/MisPostulaciones`, el boton "Ver" ahora abre `Proyectos/Details` con `ProyectoId` (antes enviaba `Id` de postulacion a `Convocatorias/Details`, enlace incorrecto).

## 3) Que falta por implementar (priorizado para siguiente avance)

### Prioridad alta

1. **HU-RA-12 Carga masiva CSV/Excel**
   - Subida de archivo, parser, validacion por filas, reporte de errores.
2. **HU-RA-14 Bitacora inmutable automatica**
   - Registrar automaticamente altas/cambios/bajas con middleware o `SaveChanges` interceptado.
3. **HU-RA-16 Exportacion PDF y Excel**
   - Reportes descargables con logo institucional y fecha.

### Prioridad media

4. **HU-RA-17 Recomendador IA mejorado**
   - Ajustar pesos por historial real de aceptaciones/rechazos.
5. **HU-RA-18 Prediccion IA avanzada**
   - Agregar variables reales de inasistencias/parciales cuando se habilite captura.
6. **Flujo documental de postulacion**
   - Subida de PDF de evidencia y validacion de tamano/tipo.

## 4) Historias de usuario nuevas recomendadas (perspectiva usuario)

> Estas HU no son tecnicas, estan escritas desde usuario y ayudan a cerrar huecos funcionales.

- **HU-RA-19 - Simulador de Elegibilidad**
  - Como Alumno quiero simular mi elegibilidad antes de postularme para saber si cumplo y que debo mejorar.
- **HU-RA-20 - Intervencion Temprana**
  - Como Tutor quiero marcar acciones de apoyo (asesoria, canalizacion, seguimiento) para dar trazabilidad a rescates academicos.
- **HU-RA-21 - Seguimiento de Alertas**
  - Como Autoridad quiero ver el historial de alertas rojas y su evolucion para medir impacto de intervenciones.
- **HU-RA-22 - Evidencia de Postulacion**
  - Como Alumno quiero adjuntar documentos PDF a mi postulacion para cumplir requisitos sin entregar en papel.
- **HU-RA-23 - Reporte de Carga por Grupo**
  - Como Autoridad quiero descargar la carga academica y riesgo por carrera/grupo para decisiones en academia.

## 5) Que son las migraciones y como explicarlas al profesor

La carpeta `Migrations` es el **historial versionado de la estructura de la base de datos**.

- Cada archivo de migracion describe cambios al esquema (tablas, columnas, llaves, relaciones).
- `ApplicationDbContextModelSnapshot` es una "foto actual" del modelo.
- Cuando se ejecuta `dotnet ef database update`, EF aplica esas migraciones en orden.

Forma corta de explicarlo en exposicion:

> "Las migraciones son como commits de la base de datos: guardan cada cambio estructural y permiten recrear la BD en cualquier equipo de manera consistente."

## 6) Comparacion con MiniTareas (para alinear enfoque de clase)

MiniTareas usa una estructura didactica muy clara:

- `Controllers/`
- `Models/`
- `Models/ViewModels/`
- `Views/`
- `Data/MemoriaRepo`
- `Program.cs`

Rescate Academico ya va por el camino profesional con EF + Identity. Para acercarlo al estilo de clase sin retroceder:

1. Mantener EF (no regresar a memoria).
2. Crear carpeta `ViewModels/` y mover modelos de vista que hoy estan dentro de controladores.
3. Crear carpeta `Services/` para la logica de IA/elegibilidad.
4. Mantener controladores delgados (solo flujo HTTP).
5. Mantener `Program.cs` simple y declarativo.

## 7) Script base de datos y MemoriaRepo solicitado

El profesor mostro `MemoriaRepo` de MiniTareas para entender consultas tipo:

- `InscripcionesDe`
- `EntregasDe`
- `ComentariosDe`

En este proyecto ya existe equivalente con EF Core (consultas LINQ sobre `ApplicationDbContext`), por lo que **no conviene mezclar un `MemoriaRepo` nuevo** porque duplicaria fuentes de verdad.

Para explicarlo:

> "En MiniTareas era memoria temporal. En Rescate Academico ya estamos en base real con EF; por eso esas consultas se implementan directo en el contexto con `Where(...)` y `ToListAsync()`."

## 8) Guia de exposicion por integrante

### Sergio (seguridad, roles, bitacora)

- Login seguro con bloqueo por intentos.
- Control por roles y accesos.
- Estado actual de bitacora y siguiente paso (inmutabilidad automatica).

### Sara (convocatorias, postulaciones, reportes)

- Flujo completo: explorar convocatoria -> postularse -> estado.
- Validaciones funcionales de elegibilidad.
- Plan de exportacion PDF/Excel.

### Alejandra (dashboard, radiografia, estadisticas)

- Dashboard por rol.
- Radiografia academica del alumno (semaforo + indicadores).
- Estadisticas globales y por carrera.

### Elias (admin y operacion institucional)

- CRUD de alumnos/tutores/carreras/ciclos.
- Asignacion de tutorados.
- Propuesta de carga masiva CSV/Excel.

### Buenfil (IA)

- Explicar IA actual basada en reglas (predictiva + compatibilidad).
- Mostrar tablas `PrediccionesDesercion` y `SugerenciasIA`.
- Roadmap de evolucion a modelo mas avanzado.

## 9) Flujo de demo recomendado (10-15 min)

1. Login como admin y mostrar dashboard.
2. Revisar alumnos con riesgo.
3. Login como alumno y abrir radiografia IA.
4. Ir a convocatorias y postularse.
5. Revisar mis postulaciones y notificaciones.
6. Login como tutor y abrir tutorados.
7. Login como autoridad y mostrar estadisticas/reporte.

## 10) Resultado para revision de hoy

El sistema **no esta terminado al 100%**, pero ya tiene una base funcional fuerte, coherente con HU principales y con una radiografia/IA visible que responde al enfasis del profesor.

---

Si el equipo quiere, el siguiente bloque de trabajo lo enfocamos a cerrar HU-RA-12, HU-RA-14 y HU-RA-16 para dejar el proyecto casi "demo final".
