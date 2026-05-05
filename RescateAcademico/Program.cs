using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Filters;
using RescateAcademico.Models;
using RescateAcademico.Services;
using RescateAcademico.Seeders;
using System.Threading.RateLimiting;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Setup early logging for database diagnostics
var startupLoggerFactory = LoggerFactory.Create(b => b.AddConsole());
var startupLogger = startupLoggerFactory.CreateLogger<Program>();

// Railway provides DATABASE_URL for PostgreSQL
var railwayDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var isRailway = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT"));

if (!string.IsNullOrEmpty(railwayDbUrl))
{
    startupLogger.LogInformation("DATABASE_URL detected. Configuring PostgreSQL for Railway.");
    // Convert PostgreSQL URL to connection string format
    // Railway passwords may contain special characters (:, @, /, %). Use Split(':', 2) and UrlDecode.
    var uri = new Uri(railwayDbUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = WebUtility.UrlDecode(userInfo[0]);
    var password = userInfo.Length > 1 ? WebUtility.UrlDecode(userInfo[1]) : "";
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    startupLogger.LogInformation("PostgreSQL target: Host={Host}, Database={Db}", uri.Host, uri.AbsolutePath.TrimStart('/'));
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else if (isRailway)
{
    // Fallback: try individual Railway PostgreSQL env vars
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    var pgDb = Environment.GetEnvironmentVariable("PGDATABASE");
    var pgUser = Environment.GetEnvironmentVariable("PGUSER");
    var pgPass = Environment.GetEnvironmentVariable("PGPASSWORD");
    var pgPort = Environment.GetEnvironmentVariable("PGPORT");

    if (!string.IsNullOrEmpty(pgHost) && !string.IsNullOrEmpty(pgDb)
        && !string.IsNullOrEmpty(pgUser) && !string.IsNullOrEmpty(pgPass))
    {
        startupLogger.LogInformation("Individual PG env vars detected. Configuring PostgreSQL for Railway.");
        connectionString = $"Host={pgHost};Port={pgPort ?? "5432"};Database={pgDb};Username={pgUser};Password={pgPass};SSL Mode=Require;Trust Server Certificate=true";
        startupLogger.LogInformation("PostgreSQL target: Host={Host}, Database={Db}", pgHost, pgDb);
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
    }
    else
    {
        startupLogger.LogError("Railway environment detected (RAILWAY_ENVIRONMENT is set) but DATABASE_URL is missing.");
        startupLogger.LogError("Ensure a PostgreSQL service is created and linked to this application service in the Railway dashboard.");
        throw new InvalidOperationException(
            "Railway deployment misconfiguration: DATABASE_URL is missing. " +
            "Please add a PostgreSQL service to your Railway project and verify it is linked to this app service.");
    }
}
else if (!string.IsNullOrEmpty(connectionString))
{
    startupLogger.LogInformation("DATABASE_URL not found. Using SQLite with DefaultConnection.");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    throw new InvalidOperationException("No database connection string found. Set 'DefaultConnection' in appsettings or provide 'DATABASE_URL' environment variable.");
}

builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(20);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 4;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<DataProtectionTokenProviderOptions>(opts =>
{
    opts.TokenLifespan = TimeSpan.FromMinutes(10);
});

var cookieSecurePolicy = isRailway ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});

builder.Services.AddScoped<AlertasService>();
builder.Services.AddHttpClient<DesercionPredictionService>();
builder.Services.AddScoped<DesercionPredictionService>();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    options.OnRejected = (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.WriteAsync("Demasiadas solicitudes. Intenta de nuevo en un momento.", token);
        return ValueTask.CompletedTask;
    };
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<AuditLogFilter>();
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { },
    KnownProxies = { },
    RequireHeaderSymmetry = false
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!isRailway)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' cdn.jsdelivr.net cdn.datatables.net; " +
        "style-src 'self' 'unsafe-inline' cdn.jsdelivr.net cdn.datatables.net; " +
        "img-src 'self' data:; " +
        "font-src 'self' cdn.jsdelivr.net; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self';");
    await next();
});

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var config = services.GetRequiredService<IConfiguration>();
    var resetOnStartup = config.GetValue<bool>("Database:ResetOnStartup");
    var logger = services.GetRequiredService<ILogger<Program>>();

    if (resetOnStartup)
    {
        logger.LogWarning("Database:ResetOnStartup=true. Recreating local SQLite database.");
        await context.Database.EnsureDeletedAsync();
    }

    logger.LogInformation("Ensuring database schema exists...");
    try
    {
        context.Database.EnsureCreated();
        logger.LogInformation("Database schema ensured.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "CRITICAL: Database.EnsureCreated() failed. Check connection string and database accessibility.");
        throw;
    }

    try
    {
        // Seed all demo data (roles, users, 300 students, professors, grades, predictions, etc.)
        await DemoDataSeeder.SeedAsync(services, context);
        logger.LogInformation("Demo data seeded successfully. Database ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Un error ocurrió durante el seeding de datos.");
        throw;
    }

    // Data integrity: ensure all students have a computed risk level
    try
    {
        var alumnosSinRiesgo = await context.Alumnos
            .Where(a => string.IsNullOrEmpty(a.RiesgoAcademico))
            .ToListAsync();
        if (alumnosSinRiesgo.Count > 0)
        {
            logger.LogInformation("Found {Count} students without RiesgoAcademico. Re-evaluating...", alumnosSinRiesgo.Count);
            var alertas = new RescateAcademico.Services.AlertasService(context);
            int fixedCount = 0;
            foreach (var alumno in alumnosSinRiesgo)
            {
                try
                {
                    var resultado = await alertas.EvaluarYAlertarAsync(alumno.Matricula);
                    if (resultado.Contains("actualizado")) fixedCount++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to evaluate risk for {Matricula}", alumno.Matricula);
                }
            }
            logger.LogInformation("Re-evaluated {Fixed} student risk levels.", fixedCount);
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Risk re-evaluation check failed.");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
