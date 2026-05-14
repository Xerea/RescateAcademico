# Historias de Usuario - Rescate Académico

## Implementadas (Funcionales)

### Fase 1: Seguridad y Acceso
| ID | Título | Estado | Responsable |
|----|--------|--------|-------------|
| HU-RA-01 | Inicio de Sesión Seguro | ✅ Implementada | Sergio |
| HU-RA-02 | Recuperación de Contraseña | ✅ Implementada | Sara |
| HU-RA-05 | Prevención de Intrusiones | ✅ Implementada | Alexis |

**Detalles técnicos:**
- Login valida correos institucionales (@ipn.mx, @alumno.ipn.mx)
- Bloqueo automático tras 3 intentos fallidos (20 minutos)
- Sesión expira en 15 minutos de inactividad
- **Auto-registro eliminado**: las cuentas son creadas por administración institucional (HU-RA-19)

### Fase 2: Dashboard y Navegación
| ID | Título | Estado | Responsable |
|----|--------|--------|-------------|
| HU-RA-03 | Menú Principal (Dashboard) | ✅ Implementada | Alejandra |
| HU-RA-04 | Gestión de Roles y Privilegios | ✅ Implementada | Sergio |

**Detalles técnicos:**
- Dashboard dinámico según rol (4 roles: Admin, Tutor, Alumno, Autoridad)
- Menús diferenciados por permisos
- Acceso denegado con redirección automática

### Fase 3: Perfil Académico
| ID | Título | Estado | Responsable |
|----|--------|--------|-------------|
| HU-RA-06 | Consulta de Situación Académica | ✅ Implementada | Alejandra |
| HU-RA-11 | Semáforo de Rendimiento | ✅ Implementada | Alejandra |

**Detalles técnicos:**
- Radiografía académica completa: promedio, materias aprobadas/reprobadas, ETS, recursamientos
- Semáforo: Verde (>=7.0), Amarillo (6.0-6.9), Rojo (<6.0)
- Sugerencias automáticas basadas en indicadores
- Predicción de deserción con ML.NET (modelo funcional, entrenado con mock data)

### Fase 4: Convocatorias y Postulación
| ID | Título | Estado | Responsable |
|----|--------|--------|-------------|
| HU-RA-07 | Catálogo de Convocatorias | ✅ Implementada | Sara |
| HU-RA-08 | Postulación a Proyectos | ✅ Implementada | Sara |
| HU-RA-09 | Notificaciones de Acción | ✅ Implementada | Alejandra |
| HU-RA-20 | Subida de Documentos en Postulación | ✅ Implementada | Sara |

**Detalles técnicos:**
- Convocatorias con filtros por tipo, área, búsqueda
- Validación de elegibilidad: promedio mínimo, semestre, carrera, cupo disponible
- Formulario de postulación con adjunto de documentos (PDF, DOC, DOCX)
- Notificaciones automáticas al cambiar estado de postulación
- Documentos almacenados en `/wwwroot/uploads/postulaciones/`

### Fase 5: Monitoreo y Tutoría
| ID | Título | Estado | Responsable |
|----|--------|--------|-------------|
| HU-RA-10 | Monitoreo Consolidado de Tutorados | ✅ Implementada | Elias |
| HU-RA-22 | Registro de Intervenciones Tutoriales | ✅ Implementada | Elias |
| HU-RA-24 | Plan de Mejora Académica Personalizado | ✅ Implementada | Alejandra |

**Detalles técnicos:**
- Tabla consolidada de tutorados con indicadores clave
- Acceso a radiografía completa del tutorado
- Registro de intervenciones con seguimiento
- Planes de mejora: CRUD completo con metas, recomendaciones y seguimiento
- Solo tutores ven a sus asignados

### Fase 6: Administración
| ID | Título | Estado | Responsable |
|----|--------|--------|-------------|
| HU-RA-12 | Carga Masiva de Alumnado | ✅ Implementada | Elias |
| HU-RA-13 | Ajustes de Ciclos Escolares | ✅ Implementada | Sergio |
| HU-RA-14 | Bitácora de Auditoría | ✅ Implementada | Sergio |
| HU-RA-19 | Creación de Cuentas Institucionales | ✅ Implementada | Sergio |
| HU-RA-26 | Dashboard de Integridad de Datos | ✅ Implementada | Elias |

**Detalles técnicos:**
- Carga masiva vía CSV (drag & drop o selector)
- CRUD de ciclos escolares y carreras
- Bitácora inmutable con filtros
- Creación de cuentas por administrador (no auto-registro)
- Verificación de consistencia de datos

### Fase 7: Estadísticas y Reportes
| ID | Título | Estado | Responsable |
|----|--------|--------|-------------|
| HU-RA-15 | Tablero Estadístico (Analytics) | ✅ Implementada | Sara |
| HU-RA-16 | Emisión de Reportes Oficiales (CSV/Imprimible) | ✅ Implementada | Alejandra |
| HU-RA-25 | Exportación con Logo Institucional | ✅ Implementada (HTML imprimible) | Alejandra |

**Detalles técnicos:**
- Estadísticas globales institucionales
- Distribución por nivel de riesgo
- Filtros por carrera
- Exportación a CSV (compatible con Excel)
- Vista de reporte imprimible con encabezado institucional (IPN)
- Botón "Imprimir / Guardar como PDF" en navegador

### Fase 8: Alertas y Riesgo Académico
| ID | Título | Estado | Responsable |
|----|--------|--------|-------------|
| HU-RA-23 | Alertas Automáticas de Riesgo | ✅ Implementada | Alexis |

**Detalles técnicos:**
- Evaluación automática de riesgo basada en: promedio, materias reprobadas, ausencias, parciales bajos, ETS, recursamientos
- Clasificación: Rojo / Amarillo / Verde
- Notificaciones automáticas a alumno, tutor y administrador cuando cambia el riesgo
- Re-evaluación automática al editar datos del alumno
- Botón de evaluación masiva en panel de administración

### Fase 9: Inteligencia Artificial
| ID | Título | Estado | Responsable |
|----|--------|--------|-------------|
| HU-RA-17 | Sugerencia Optimizada (IA) | ✅ Implementada (Heurísticas) | Alexis |
| HU-RA-18 | Prevención de Deserción (IA) | ✅ Implementada (ML.NET básico) | Alexis |

**Detalles técnicos:**
- Compatibilidad de proyectos evaluada por puntuación (0-100%)
- Predicción de deserción con ML.NET (modelo de regresión)
- **Nota**: Los modelos funcionan con datos mock. Requieren datos reales de SAES para precisión óptima.

---

## Pendientes (Por implementar)

| ID | Título | Prioridad | Responsable |
|----|--------|-----------|-------------|
| *Ninguna* | Todas las historias de usuario planificadas han sido implementadas. | - | - |

## Notas
- Fecha de última actualización: Abril 2026
- Estado actual: **23/23 HUs implementadas**
- Datos: Demo con 50 alumnos, 10 tutores, 8 proyectos y datos académicos simulados de SAES
- Base de datos: SQLite local con `ResetOnStartup: false` (persistencia activada)
- Próxima entrega: Finales de abril 2026
