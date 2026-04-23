using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;
using RescateAcademico.Services;
using RescateAcademico.Seeders;

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
    var uri = new Uri(railwayDbUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else if (isRailway)
{
    startupLogger.LogError("Railway environment detected (RAILWAY_ENVIRONMENT is set) but DATABASE_URL is missing.");
    startupLogger.LogError("Ensure a PostgreSQL service is created and linked to this application service in the Railway dashboard.");
    throw new InvalidOperationException(
        "Railway deployment misconfiguration: DATABASE_URL is missing. " +
        "Please add a PostgreSQL service to your Railway project and verify it is linked to this app service.");
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
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<DataProtectionTokenProviderOptions>(opts =>
{
    opts.TokenLifespan = TimeSpan.FromMinutes(10);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddScoped<AlertasService>();
builder.Services.AddScoped<DesercionPredictionService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

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

    context.Database.EnsureCreated();
    
    try
    {
        // First seed roles and basic admin data
        await RoleSeeder.InitializeAsync(services, context);
        // Then populate with comprehensive mock SAES demo data
        await DemoDataSeeder.SeedAsync(services, context);
        logger.LogInformation("Demo data seeded successfully. Database ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Un error ocurrió durante el seeding de datos.");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
