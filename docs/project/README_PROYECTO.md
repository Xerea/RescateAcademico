# Rescate Académico - Documentación de Funcionalidad y Exposición

## Resumen del Proyecto
Rescate Académico es una plataforma web institucional para la gestión y monitoreo académico de alumnos, facilitando la asignación a proyectos de servicio social, investigación y apoyo académico, con enfoque en prevención de riesgo y toma de decisiones basada en datos.

## Funcionalidades Implementadas

### 1. Registro y Autenticación
- **Registro de usuario alumno:** Al crear una cuenta, se solicita matrícula, nombre, apellidos, carrera y semestre. Se crea automáticamente el perfil de alumno y se vincula con el usuario.
- **Inicio de sesión seguro:** Solo usuarios activos pueden acceder. Se valida el correo institucional.
- **Recuperación de contraseña:** Envío de enlace de reseteo al correo institucional.
- **Bloqueo tras intentos fallidos:** Implementado mediante Identity.

### 2. Dashboard y Roles
- **Dashboard dinámico:** Muestra opciones y estadísticas según el rol (Alumno, Tutor, Autoridad, Administrador).
- **Gestión de roles y privilegios:** Los menús y accesos se adaptan automáticamente.

### 3. Perfil Académico Integral
- **Consulta de situación académica:** Alumnos pueden ver su historial, promedio, materias reprobadas, recursamientos, carga y nivel de riesgo.
- **Radiografía académica para tutor:** Los tutores pueden ver el desempeño de sus tutorados asignados.

### 4. Proyectos y Convocatorias
- **Catálogo de convocatorias:** Alumnos pueden explorar y filtrar proyectos activos.
- **Postulación digital:** Formulario para postularse, con validaciones de elegibilidad y restricciones de cupo.
- **Gestión de postulaciones:** Alumnos ven el estado de sus solicitudes; administradores y autoridades pueden aceptar/rechazar.

### 5. Notificaciones
- **Notificaciones automáticas:** Al postularse o cambiar el estado de una solicitud, se genera una notificación para el alumno.

### 6. Administración
- **Gestión de usuarios, alumnos, tutores, carreras, ciclos escolares, bitácora y carga masiva:** Accesible solo para administradores.

### 7. IA y Analítica
- **Sugerencias automáticas de proyectos:** El sistema recomienda proyectos compatibles según el perfil académico.
- **Predicción de riesgo de deserción:** Se calcula y muestra el nivel de riesgo académico.

## Funcionalidades Pendientes o Parcialmente Implementadas
- **Carga masiva de alumnos desde Excel/CSV:** Interfaz lista, lógica pendiente de robustecer.
- **Generación de reportes oficiales (PDF/Excel):** Interfaz y botones listos, generación de archivos pendiente.
- **Panel de analítica avanzada y semáforo de rendimiento:** Visualización básica lista, falta profundizar en gráficos y colores.
- **Módulo de IA avanzado:** Sugerencias y predicción básica implementadas, falta integración de modelos externos.

## Correcciones Realizadas
- Se corrigió el flujo de registro para asociar automáticamente el usuario con el perfil de alumno.
- Se revisaron y validaron los botones de navegación y acción en dashboard, postulaciones, convocatorias y perfil académico.
- Se validó que ningún botón principal esté roto o cause errores.

## Qué debe decir cada miembro del equipo en la exposición

### Sergio (Seguridad, Roles y Bitácora)
- Explicar el flujo de registro y login seguro.
- Mencionar el bloqueo tras intentos fallidos y la gestión de roles.
- Mostrar la bitácora de auditoría y cómo se registra cada acción importante.

### Sara (Recuperación, Convocatorias y Postulación)
- Demostrar la recuperación de contraseña.
- Explicar el catálogo de convocatorias y el proceso de postulación.
- Mostrar las notificaciones automáticas al postularse.

### Alejandra (Dashboard, Perfil y Analítica)
- Navegar por el dashboard y mostrar cómo cambia según el rol.
- Mostrar el perfil académico y la radiografía para tutor.
- Explicar el semáforo de riesgo y las sugerencias automáticas.

### Elias (Carga Masiva, Ciclos y Reportes)
- Explicar la carga masiva de alumnos y la gestión de ciclos escolares.
- Mostrar la interfaz de reportes y explicar lo que falta para la generación de archivos.

## Recomendaciones para la Exposición
- Enfatizar que la plataforma ya es funcional y cubre los flujos principales.
- Mencionar que la estética puede mejorar, pero la funcionalidad es la prioridad.
- Explicar que la base de datos es propia y no depende de sistemas externos.
- Indicar qué módulos están listos y cuáles están en desarrollo.

## Lo que falta por implementar
- Mejorar la carga masiva y generación de reportes.
- Profundizar en analítica avanzada y visualización de datos.
- Integrar modelos de IA más sofisticados.
- Mejorar la experiencia visual (CSS/JS) en futuras iteraciones.

---

**¡Listo para la revisión!**

Cualquier duda, cada miembro puede consultar esta guía para exponer su parte y demostrar la funcionalidad del sistema.