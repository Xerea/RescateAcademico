using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RescateAcademico.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration, ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            Request.Form.TryGetValue("g-recaptcha-response", out StringValues recaptchaValues);
            var recaptchaResponse = recaptchaValues.ToString();
            if (!await VerifyRecaptchaAsync(recaptchaResponse))
            {
                ModelState.AddModelError(string.Empty, "Verificación de seguridad fallida. Por favor intenta de nuevo.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Debes ingresar tu correo.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            var isInstitutionalEmail = email.EndsWith("@ipn.mx", StringComparison.OrdinalIgnoreCase) || email.EndsWith("@alumno.ipn.mx", StringComparison.OrdinalIgnoreCase);
            if (user == null && !isInstitutionalEmail)
            {
                ModelState.AddModelError(string.Empty, "El correo no institucional debe estar registrado y validado antes de iniciar sesión.");
                return View();
            }
            if (user != null && !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Esta cuenta no está activa.");
                return View();
            }
            if (user != null && user.PendienteVerificacion)
            {
                ModelState.AddModelError(string.Empty, "Tu cuenta está pendiente de validación institucional. Contacta a tu coordinador académico.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                if (user != null && await _userManager.IsInRoleAsync(user, "Tutor"))
                {
                    return RedirectToAction("Index", "Profesor");
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "La cuenta ha sido bloqueada temporalmente por demasiados intentos fallidos. Intente de nuevo en 20 minutos.");
                return View();
            }
            else
            {
                int failedAttempts = 0;
                if (user != null)
                {
                    failedAttempts = await _userManager.GetAccessFailedCountAsync(user);
                }

                ModelState.AddModelError(string.Empty, $"Correo o contraseña incorrectos. Intentos fallidos: {failedAttempts}/3.");
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("register")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (string.IsNullOrWhiteSpace(model.Matricula))
            {
                ModelState.AddModelError(nameof(model.Matricula), "La boleta es obligatoria.");
                return View(model);
            }

            var alumnoExistente = await _context.Alumnos.FirstOrDefaultAsync(a => a.Matricula == model.Matricula);

            var isInstitucional = model.Email.EndsWith("@alumno.ipn.mx", StringComparison.OrdinalIgnoreCase) || model.Email.EndsWith("@ipn.mx", StringComparison.OrdinalIgnoreCase);
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                IsActive = true,
                PendienteVerificacion = !isInstitucional || alumnoExistente != null
            };

            if (alumnoExistente != null && !string.IsNullOrWhiteSpace(model.CodigoVinculacion))
            {
                var codeHash = HashClaimCode(model.CodigoVinculacion);
                var claim = await _context.AlumnoClaimCodes
                    .FirstOrDefaultAsync(c => c.Matricula == model.Matricula && c.CodeHash == codeHash && c.UsedAt == null && c.ExpiresAt > DateTime.Now);
                if (claim != null)
                {
                    user.PendienteVerificacion = false;
                    isInstitucional = true;
                }
                else
                {
                    ModelState.AddModelError(nameof(model.CodigoVinculacion), "El código de vinculación no es válido o ya expiró.");
                    return View(model);
                }
            }

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Alumno");
                if (alumnoExistente != null)
                {
                    var claim = await _context.AlumnoClaimCodes
                        .FirstOrDefaultAsync(c => c.Matricula == model.Matricula && c.CodeHash == HashClaimCode(model.CodigoVinculacion ?? "") && c.UsedAt == null && c.ExpiresAt > DateTime.Now);
                    if (claim != null)
                    {
                        alumnoExistente.UserId = user.Id;
                        alumnoExistente.Correo = model.Email;
                        claim.UsedAt = DateTime.Now;
                        claim.UsedByUserId = user.Id;
                    }
                    else
                    {
                        _context.AccountLinkRequests.Add(new AccountLinkRequest
                        {
                            UserId = user.Id,
                            Email = model.Email,
                            Matricula = model.Matricula,
                            NombreSolicitado = $"{model.Nombre} {model.Apellidos}"
                        });
                    }
                }
                else
                {
                    var riesgo = new RescateAcademico.Services.RiskEvaluationService().CalcularRiesgo(new Alumno
                    {
                        Matricula = model.Matricula,
                        PromedioGlobal = 0,
                        MateriasReprobadas = 0,
                        Ausencias = 0,
                        ParcialesBajos = 0,
                        EtsPresentados = 0,
                        Recursamientos = 0
                    });
                    _context.Alumnos.Add(new Alumno
                    {
                        Matricula = model.Matricula,
                        Nombre = model.Nombre,
                        Apellidos = model.Apellidos,
                        Carrera = model.Carrera,
                        SemestreActual = model.SemestreActual,
                        Correo = model.Email,
                        UserId = user.Id,
                        PromedioGlobal = 0,
                        Estatus = "Activo",
                        RiesgoAcademico = riesgo
                    });
                    if (user.PendienteVerificacion)
                    {
                        _context.AccountLinkRequests.Add(new AccountLinkRequest
                        {
                            UserId = user.Id,
                            Email = model.Email,
                            Matricula = model.Matricula,
                            NombreSolicitado = $"{model.Nombre} {model.Apellidos}"
                        });
                    }
                }
                await _context.SaveChangesAsync();

                if (user.PendienteVerificacion)
                {
                    TempData["Success"] = "Cuenta creada correctamente. Tu cuenta está pendiente de validación institucional. Contacta a tu coordinador académico.";
                    return RedirectToAction("Login");
                }
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> DemoLogin(string role)
        {
            var demoEnabled = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                || string.Equals(_configuration["SHOW_DEMO_ACCESS"], "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(_configuration["SHOW_DEMO_CREDENTIALS"], "true", StringComparison.OrdinalIgnoreCase);
            if (!demoEnabled)
                return NotFound();

            var normalizedRole = (role ?? string.Empty).Trim().ToLowerInvariant();
            var (email, password, controller) = normalizedRole switch
            {
                "alumno" => ("demo.alumno@alumno.ipn.mx", _configuration["DEMO_ALUMNO_PASSWORD"] ?? "Demo123!", "Dashboard"),
                "tutor" => ("profesor1@ipn.mx", _configuration["DEMO_TUTOR_PASSWORD"] ?? "Demo123!", "Profesor"),
                "autoridad" => ("autoridad@ipn.mx", _configuration["DEMO_AUTORIDAD_PASSWORD"] ?? "Autoridad123!", "Dashboard"),
                _ => ("", "", "")
            };

            if (string.IsNullOrEmpty(email))
                return BadRequest();

            var result = await _signInManager.PasswordSignInAsync(email, password, false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                TempData["Error"] = "No se pudo abrir el modo demo. Verifica que la base demo esté sembrada.";
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", controller);
        }

        [Authorize]
        [HttpGet]
        public IActionResult CambiarContrasena()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasena(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Las contraseñas nuevas no coinciden.");
                return View();
            }
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                TempData["Success"] = "Contraseña actualizada correctamente.";
                return RedirectToAction("Index", "Dashboard");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || (!email.EndsWith("@ipn.mx", StringComparison.OrdinalIgnoreCase) && !email.EndsWith("@alumno.ipn.mx", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(string.Empty, "Solo se aceptan correos institucionales registrados (@ipn.mx o @alumno.ipn.mx).");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { email = user.Email, code }, protocol: Request.Scheme);

            TempData["ResetLink"] = callbackUrl;

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string code = null!)
        {
            if (code == null)
            {
                return BadRequest("Se debe suministrar un código para restablecer la contraseña.");
            }

            return View(new ResetPasswordViewModel { Email = email, Code = code });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        private async Task<bool> VerifyRecaptchaAsync(string? token)
        {
            var enforceRecaptcha = string.Equals(_configuration["ENFORCE_RECAPTCHA"], "true", StringComparison.OrdinalIgnoreCase);
            var siteKey = _configuration["RECAPTCHA_SITE_KEY"];
            var secretKey = _configuration["RECAPTCHA_SECRET_KEY"];
            if (!enforceRecaptcha)
            {
                _logger.LogInformation("reCAPTCHA: Advisory mode; allowing login. Set ENFORCE_RECAPTCHA=true to block failed checks.");
                return true;
            }

            if (string.IsNullOrEmpty(siteKey) || string.IsNullOrEmpty(secretKey))
            {
                _logger.LogInformation("reCAPTCHA: Site key or secret key not configured; allowing login");
                return true;
            }

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("reCAPTCHA: Empty token received from form");
                return false;
            }

            try
            {
                using var httpClient = new HttpClient();
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", secretKey),
                    new KeyValuePair<string, string>("response", token)
                });
                var response = await httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("reCAPTCHA Google response: {Response}", json);

                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var success = doc.RootElement.GetProperty("success").GetBoolean();
                if (!success)
                {
                    var errors = doc.RootElement.TryGetProperty("error-codes", out var errEl)
                        ? string.Join(", ", errEl.EnumerateArray().Select(e => e.GetString()))
                        : "unknown";
                    _logger.LogWarning("reCAPTCHA verification failed: {Errors}", errors);
                    return false;
                }

                var hasScore = doc.RootElement.TryGetProperty("score", out var scoreEl);
                return !hasScore || scoreEl.GetDouble() >= 0.5;
            }
            catch
            {
                return true; // Fail open — don't block users if verification service is down
            }
        }

        private static string HashClaimCode(string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes((code ?? string.Empty).Trim().ToUpperInvariant()));
            return Convert.ToHexString(bytes);
        }

    }

    public class ResetPasswordViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Matricula { get; set; } = string.Empty;

        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Apellidos { get; set; } = string.Empty;

        [Required]
        public string Carrera { get; set; } = string.Empty;
        public int SemestreActual { get; set; } = 1;
        public string? CodigoVinculacion { get; set; }
    }
}
