# AGENTS.md — Rescate Académico

## Project Overview
ASP.NET Core 8 MVC app for IPN (Instituto Politécnico Nacional) student academic risk monitoring. Deployed on Railway with PostgreSQL. ~308 demo students, 12 professors, 6 careers, group-based assignment.

## Critical Files
- `Program.cs` — PostgreSQL detection, `DATABASE_URL` parsing, `Npgsql.EnableLegacyTimestampBehavior`, antiforgery + rate limiter + security headers + audit filter registration
- `Data/DemoDataSeeder.cs` — All seeding (roles, users, 308 students, grades, interventions, etc.). Guard: skips if `Alumnos.Count >= 50`
- `Data/ApplicationDbContext.cs` — EF Core context
- `Dockerfile` — .NET 8 multi-stage build, repo root
- `railway.toml` — Railway deploy config, repo root
- `Services/DesercionPredictionService.cs` — Heuristics + OpenAI GPT-4o-mini integration
- `Filters/AuditLogAttribute.cs` — `[AuditLog]` action filter for critical mutations

## Build & Deploy
```bash
cd RescateAcademico
dotnet build   # must be 0 warnings, 0 errors
git push       # triggers Railway auto-deploy
```

## Database
- **Local**: SQLite (`app.db`), `DefaultConnection` in `appsettings.json`
- **Production**: PostgreSQL via `DATABASE_URL` env var (Railway auto-injects)
- **No EF Migrations used**. `EnsureCreated()` on startup. If model changes, database must be wiped and recreated (drop + redeploy on Railway)
- First deploy seeding takes 8-10 minutes. Increase Railway health check timeout.

## Environment Variables
| Variable | Purpose | Default |
|----------|---------|---------|
| `DATABASE_URL` | PostgreSQL connection (Railway) | — |
| `DEMO_ADMIN_PASSWORD` | Admin demo password | `Admin123!` |
| `DEMO_AUTORIDAD_PASSWORD` | Autoridad demo password | `Autoridad123!` |
| `DEMO_TUTOR_PASSWORD` | Professor demo passwords | `Demo123!` |
| `DEMO_ALUMNO_PASSWORD` | Student demo passwords | `Demo123!` |
| `OPENAI_API_KEY` | OpenAI GPT-4o-mini API key | — |
| `RAILWAY_ENVIRONMENT` | Detected by app to know it's on Railway | — |

## Security Architecture
- **Global antiforgery**: `AutoValidateAntiforgeryTokenAttribute` filter + `[ValidateAntiForgeryToken]` on all POSTs
- **Rate limiting**: Fixed window "login" policy (10 req/min)
- **Security headers middleware**: CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy
- **Cookie settings**: HttpOnly, Secure, SameSite=Strict for auth, session, and antiforgery cookies
- **Over-posting protection**: `[Bind("Prop1,Prop2")]` on all model-bound POST actions
- **File uploads**: 5MB limit, whitelist extensions (pdf, doc, docx, jpg, jpeg, png), MIME type verification
- **XSS prevention**: `JsonSerializer.Serialize` instead of `Html.Raw` in Chart.js; `textContent` in AJAX modals
- **Open redirect prevention**: `Url.IsLocalUrl(returnUrl)` in login
- **Audit logging**: `[AuditLog(Accion = "...", Tabla = "...")]` on critical mutations (delete, block, validate, AI analysis)

## Code Style
- Use `string` (not `String`), `decimal` for grades, `DateTime.Now` for timestamps
- Keep internal code names: `Alumno.Matricula`, `Tutor` model, `"Tutor"` role — only UI labels change to `Boleta`/`Profesor`
- Burgundy color: `#6c1d45` (IPN institutional)
- All list views use DataTables with Spanish i18n
- Empty states for all index views
- Breadcrumbs on every page: `Inicio > Page Title`

## Things That Will Break If Changed
1. **Scoped CSS file**: `Views/Shared/_Layout.cshtml.css` overrides `site.css`. Must edit/delete this file, not just `site.css`
2. **Railway file locations**: `Dockerfile` and `railway.toml` MUST be at repo root (not inside `RescateAcademico/`)
3. **PostgreSQL timestamp behavior**: `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` is required
4. **DATABASE_URL password parsing**: Uses `Split(':', 2)` + `WebUtility.UrlDecode` to handle special chars in passwords
5. **Identity role names**: Must remain `"Administrador"`, `"Tutor"`, `"Alumno"`, `"Autoridad"` — used in `[Authorize(Roles = "...")]`
6. **Seeding guard**: `Alumnos.Count >= 50` prevents re-seeding. To force re-seed, wipe database or lower threshold

## Known Issues / Limitations
- No EF Migrations — schema changes require database wipe
- ML.NET FastTree training caused Railway timeouts — completely removed; OpenAI used instead
- Facial recognition API on hold (waiting for professor-provided API key)
- `Register.cshtml` exists but `AccountController.Register` action does not — throws 404
- `RoleSeeder.cs` was deleted — do not recreate

## Testing Checklist Before Demo
- [ ] Build: 0 warnings, 0 errors
- [ ] Login works for all 4 roles
- [ ] Dashboard charts render correctly
- [ ] DataTables load with Spanish i18n
- [ ] AJAX quick-view modal opens without errors
- [ ] File upload accepts only whitelisted types
- [ ] POST mutations require antiforgery token
- [ ] Rate limiting triggers after 10 login attempts/min
