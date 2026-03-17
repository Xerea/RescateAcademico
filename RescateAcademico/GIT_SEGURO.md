# 🔐 GUÍA SEGURA DE GIT - EVITAR ROMPER EL CÓDIGO

> **IMPORTANTE:** Lee esto ANTES de hacer `git push`

---

## ⚠️ QUÉ PASÓ ANTES

```
❌ MALO: Hiciste cambios sin intención → Push automático → Rompió todo
```

## ✅ QUÉ HACER AHORA (Proceso Seguro)

### PASO 1: Verificar Estado (SIEMPRE)

```powershell
git status
```

**Deberías ver:**
```
On branch db-model-roles-3a701
nothing to commit, working tree clean
```

Si ves archivos listados, significa que hay cambios sin guardar. **NO HAGAS PUSH AÚN.**

---

### PASO 2: Hacer Cambios en Local

Ahora puedes editar archivos en VS como lo haces normalmente.

```
✅ Abre archivo en VS
✅ Haz cambios
✅ Guarda el archivo (Ctrl+S)
✅ Cierra el archivo
```

**NO hagas push inmediatamente.**

---

### PASO 3: Verificar que Compila (ANTES de Commit)

**En VS:**
1. Click derecho en proyecto → **Build** (o Ctrl+Shift+B)
2. Ver en "Output" que dice: `Build succeeded`
3. **NO** hagas git commit si hay errores

**En Package Manager Console:**
```powershell
Update-Database
```

Si no hay errores rojos, está bien.

---

### PASO 4: Hacer Commit (Paso-a-paso)

```powershell
# 1. Ver qué archivos cambiaron
git status

# 2. Agregar SOLO los archivos que querías cambiar
git add .\Controllers\AccountController.cs
# O si cambiste varias cosas:
git add .

# 3. Ver que está bien
git status
# Deberían estar en verde (staged)

# 4. Hacer commit CON MENSAJE CLARO
git commit -m "HU-RA-01: Corregir validación de login"
# NO hagas: git commit -m "fixes"

# 5. Verificar que funcionó
git log --oneline -1
```

---

### PASO 5: Push (SOLO DESPUÉS de lo anterior)

```powershell
# 1. Ver en qué rama estás
git branch
# Deberías estar en: db-model-roles-3a701

# 2. Push
git push origin db-model-roles-3a701

# 3. Verificar
git log --oneline -1
```

---

## 🛑 CHECKLIST ANTES DE GIT PUSH

```
ANTES DE HACER git add:
□ ¿Abrí VS y ejecuté Build?
□ ¿Esperé a que compile sin errores?
□ ¿Guardé todos los archivos (Ctrl+S)?

ANTES DE HACER git commit:
□ ¿Verifiqué con git status qué cambia?
□ ¿Solo cambié lo que quería cambiar?
□ ¿Escribí un mensaje claro?

ANTES DE HACER git push:
□ ¿Estoy en la rama correcta (db-model-roles-3a701)?
□ ¿El commit está bien (git log --oneline)?
□ ¿Quiero realmente enviar esto a GitHub?
```

---

## 🚨 ERRORES COMUNES Y SOLUCIONES

### Error 1: "Build failed"

```
❌ MALO: Hacer git push de todas formas
✅ BIEN:
  1. Leer el error en VS Output
  2. Arreglar el código
  3. Build nuevamente
  4. Después sí hacer git commit/push
```

### Error 2: "Changes not staged"

```
❌ MALO: git push (no hace nada porque no hay commit)
✅ BIEN:
  1. git add .
  2. git commit -m "..."
  3. Después git push
```

### Error 3: "Your branch is ahead of 'origin'"

```
✅ BIEN: Es normal, significa que hiciste commits locales
   git push para enviarlos a GitHub
```

### Error 4: "Conflict" (Si hay conflictos)

```
❌ NO hagas git push
✅ BIEN:
  1. Abre VS Code
  2. Resuelve conflictos (elige cuál versión quieres)
  3. git add .
  4. git commit -m "Resolve merge conflict"
  5. git push
```

---

## 📋 FLUJO COMPLETO DE 5 MINUTOS

```
1. Verificar estado:
   git status

2. Cambiar archivo en VS:
   [Editar AccountController.cs]
   [Guardar Ctrl+S]

3. Compilar (Build):
   Ctrl+Shift+B

4. Si compila sin errores:
   git add .
   git commit -m "HU-RA-01: [Descripción]"
   git log --oneline -1

5. Push:
   git push origin db-model-roles-3a701

6. Verificar en GitHub:
   https://github.com/Xerea/RescateAcademico
```

---

## 💡 TIPS DE ORO

### Tip 1: Usar ramas para trabajo grande
```powershell
# Crear rama nueva
git checkout -b feature/my-feature

# Trabajar en esa rama (no afecta master)
# git add/commit/push normal

# Después hacer Pull Request en GitHub
```

### Tip 2: Commit pequeños
```
✅ BIEN: 5 commits pequeños, cada uno con mensaje claro
❌ MALO: 1 commit gigante con "fixed stuff"
```

### Tip 3: Mensaje de commit
```
✅ BIEN:
   "HU-RA-01: Implementar validación de login"
   "HU-RA-05: Corregir bloqueo a 20 minutos"
   
❌ MALO:
   "fixed"
   "update"
   "v2"
```

### Tip 4: Revisar antes de push
```powershell
# Ver qué va a ir
git log --oneline -3

# Ver archivos que cambiaron
git diff --name-status HEAD~1
```

---

## 🆘 SI ALGO SALE MAL

### Opción 1: Deshacer último commit (no está en GitHub)
```powershell
git reset --soft HEAD~1
# Ahora puedes arreglarlo y hacer commit de nuevo
```

### Opción 2: Deshacer cambios en un archivo
```powershell
git checkout -- Controllers/AccountController.cs
# El archivo vuelve a su estado anterior
```

### Opción 3: Ver qué cambié
```powershell
git diff Controllers/AccountController.cs
# Muestra línea por línea qué cambió
```

### Opción 4: Volver todo a la rama anterior
```powershell
git reset --hard origin/master
# ⚠️ CUIDADO: Borra TODO lo que no esté en GitHub
```

---

## 🎯 RESUMEN DE SEGURIDAD

| Acción | Segura | Razón |
|--------|:------:|-------|
| **git status** | ✅ | Solo mira, no cambia nada |
| **git add .** | ⚠️ | Marca cambios para commit |
| **git commit -m ""** | ✅ | Guarda en local (no sube) |
| **git push** | ⚠️ | SUBE a GitHub (cuidado) |
| **git reset --hard** | ❌ | Borra TODO sin recuperar |
| **Build en VS** | ✅ | Verifica antes de commit |

---

## 📞 FLUJO SEGURO = 5 PASOS

```
1️⃣  Cambiar código en VS
2️⃣  Ctrl+Shift+B (Build) → Sin errores
3️⃣  git add . + git commit -m "HU-RA-XX: ..."
4️⃣  git log --oneline -1 (Verificar)
5️⃣  git push origin db-model-roles-3a701
```

**Si algo falla en paso 2, NO hagas push.**

---

**Proyecto:** Rescate Académico
**Estado:** Protegido ✅
**Última modificación:** Hoy
