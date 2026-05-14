# Explicación de Migraciones vs RoleSeeder

## ¿Qué son las Migraciones?

Las **migraciones** en Entity Framework son un sistema para crear y actualizar la base de datos de manera controlada. Cada migración representa un cambio en el esquema de la base de datos (crear tabla, agregar columna, etc.).

### En MiniTareas (Proyecto de clase)
En MiniTareas **NO se usan migraciones**. El proyecto usa:
- **MemoriaRepo**: Una clase estática con listas que guarda todo en memoria
- Cuando reinicias el servidor, los datos se pierden
- Ejemplo:
```csharp
public static List<Usuario> Usuarios = new List<Usuario>();
public static void AgregarUsuario(Usuario u) { ... }
```

### En Rescate Académico (Tu proyecto)
En Rescate Académico **tampoco usamos migraciones activamente**. En su lugar usamos:

## RoleSeeder - El equivalente a MemoriaRepo pero con base de datos real

**RoleSeeder** es una clase que se ejecuta cuando inicia la aplicación y:
1. Crea la base de datos (si no existe)
2. Crea las tablas automáticamente
3. Inserta datos de prueba

```csharp
// En Program.cs se llama así:
context.Database.EnsureCreated();
await RoleSeeder.InitializeAsync(services, context);
```

## Diferencia Clave

| Aspecto | MemoriaRepo (MiniTareas) | RoleSeeder (Rescate) |
|--------|---------------------------|----------------------|
| Almacenamiento | Memoria RAM | Archivo SQLite (app.db) |
| Persistencia | Se pierde al reiniciar | Se guarda en archivo |
| Base de datos real | No | Sí (SQLite) |
| Velocidad | Más rápido | Más lento pero persistente |

## ¿Por qué usamos RoleSeeder y no migraciones?

Las migraciones son para cuando ya tienes una base de datos en producción y necesitas hacer cambios incrementales. En desarrollo, `EnsureCreated()` + RoleSeeder es más rápido y simpler.

## ¿Cómo funciona el guardado de datos ahora?

1. **ApplicationDbContext** = Representa la base de datos
2. **DbSet<T>** = Cada tabla
3. **SaveChangesAsync()** = Guardar todos los cambios pendientes

```csharp
// Agregar un nuevo alumno
_context.Alumnos.Add(nuevoAlumno);
await _context.SaveChangesAsync();  // Se guarda en app.db

// Actualizar
_context.Alumnos.Update(alumno);
await _context.SaveChangesAsync();

// Eliminar
_context.Alumnos.Remove(alumno);
await _context.SaveChangesAsync();
```

## En Resumen

- **Migrations**: No las usamos activamente (son para producción)
- **RoleSeeder**: nuestro sistema de seed que crea la BD y datos de prueba
- **EnsureCreated()**: Crea la BD automáticamente si no existe
- **MemoriaRepo vs RoleSeeder**: Lo mismo pero RoleSeeder usa SQLite real

La carpeta "Migrations" que ves es dondeestarían las migraciones si las usáramos, pero no son necesarias para el funcionamiento actual del proyecto.
