# Rescate Académico – Git y bases del proyecto

## 1. Git: lo básico para trabajar sin miedo

### Qué hace cada cosa

| Acción | Qué hace |
|--------|----------|
| **Commit** | Guarda un “punto de guardado” en tu historial local. Solo afecta tu PC, no GitHub. |
| **Push** | Envía tus commits locales a GitHub. Es cuando otros (o tú en otra PC) ven los cambios. |
| **Pull** | Trae los cambios que están en GitHub a tu PC. Úsalo antes de trabajar si alguien más subió algo. |

### Flujo seguro para trabajar

1. **Antes de cambiar código**
   - En Visual Studio: clic derecho en el proyecto → **Git** → **Pull** (o en la barra inferior de Git).
   - Así te aseguras de tener la última versión.

2. **Después de hacer cambios que funcionen**
   - En la barra inferior de Visual Studio verás algo como “2 cambios” o “Cambios pendientes”.
   - Clic en el ícono de Git o en **Ver** → **Cambios de Git**.
   - Selecciona los archivos que quieras incluir (o “Incluir todo”).
   - Escribe un mensaje corto, por ejemplo: `Login con validación de correo institucional`.
   - Clic en **Commit** (o **Confirmar**).
   - Luego clic en **Push** para subir a GitHub.

3. **Si algo sale mal**
   - Si no hiciste push, puedes deshacer el último commit desde Git → **Deshacer último commit**.
   - Si ya hiciste push, es mejor no “reescribir” el historial. Mejor haz un nuevo commit que corrija el error.

### Regla práctica

> Haz commit cuando algo funcione. Haz push cuando quieras tener una copia en GitHub o compartir con tu equipo.

---

## 2. NuGets del proyecto

Todos los paquetes actuales son estándar y necesarios:

| Paquete | Para qué sirve |
|---------|----------------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Login, roles, contraseñas encriptadas. |
| `Microsoft.AspNetCore.Identity.UI` | Vistas base de login/registro (aunque usamos las nuestras). |
| `Microsoft.EntityFrameworkCore.Design` | Generar migraciones desde la terminal. |
| `Microsoft.EntityFrameworkCore.Sqlite` | Usar SQLite como base de datos. |
| `Microsoft.EntityFrameworkCore.Tools` | Comandos `dotnet ef` para migraciones. |

**Conclusión:** No hay paquetes “extra” ni raros. No hace falta quitar ninguno.

---

## 3. Trabajar por bloques (sin romper lo que ya funciona)

### Bloques actuales

| Bloque | Contenido | Estado |
|-------|-----------|--------|
| **Bloque 1: Autenticación** | Login, logout, recuperación de contraseña, bloqueo por intentos, expiración de sesión | ✅ Implementado |
| **Bloque 2: Roles y menú** | Roles (Admin, Tutor, Alumno), Dashboard con opciones según rol | ✅ Implementado |
| **Bloque 3: Situación académica** | Perfil del alumno, promedios, materias (HU-RA-06) | Pendiente |
| **Bloque 4: Proyectos y convocatorias** | Catálogo, postulación (HU-RA-07, 08) | Pendiente |
| **Bloque 5: Notificaciones y tutoría** | Campana, tutorados, semáforo (HU-RA-09, 10, 11) | Pendiente |

### Regla

> No desarrollar un bloque nuevo hasta que el anterior esté probado y estable. Así evitas que un cambio rompa algo que ya funcionaba.

---

## 4. Nombres de tablas descriptivos (tbUsuarios, tbRoles, etc.)

**Estado actual:** Las tablas usan nombres por defecto de Identity (`AspNetUsers`, `AspNetRoles`, etc.).

**Para cambiarlas** cuando decidas hacerlo:

1. Se configura en `ApplicationDbContext.OnModelCreating`.
2. Se crea una nueva migración.
3. Se elimina `app.db` y se vuelve a crear la base con los nuevos nombres.

**No se ha hecho aún** para no tocar la base que ya funciona. Cuando quieras, se puede aplicar sin afectar la lógica del programa.

---

## 5. Carpeta Migrations

- Contiene el historial de cómo EF Core crea las tablas.
- La aplicación **no la usa en tiempo de ejecución**; solo al ejecutar `dotnet ef database update` o al iniciar con BD vacía.
- Es normal en proyectos con Entity Framework. Puedes explicar: *“La carpeta Migrations guarda los scripts de creación de la base de datos para poder recrearla en cualquier máquina.”*
