using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;
using RescateAcademico.Services;
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
        private readonly IEmailDeliveryService _emailDeliveryService;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration, ILogger<AccountController> logger, IEmailDeliveryService emailDeliveryService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailDeliveryService = emailDeliveryService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null, string? registro = null, string? expediente = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (registro == "pendiente")
            {
                ViewData["RegistroPendiente"] = expediente == "faltante"
                    ? "Solicitud creada. Tu cuenta queda pendiente porque la boleta aun no existe en la base academica. Coordinacion debe importar o crear tu expediente antes de aprobarla."
                    : "Solicitud creada. Tu cuenta queda pendiente de validacion para vincularla con tu expediente academico.";
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Debes ingresar tu correo.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Debes ingresar tu contrasena.");
                return View();
            }

            Request.Form.TryGetValue("g-recaptcha-response", out StringValues recaptchaValues);
            var recaptchaResponse = recaptchaValues.ToString();
            if (!await VerifyRecaptchaAsync(recaptchaResponse))
            {
                ModelState.AddModelError(string.Empty, "Verificación de seguridad fallida. Por favor intenta de nuevo.");
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
            if (user != null && !user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Confirma tu correo antes de iniciar sesión. Revisa tu bandeja de entrada o solicita apoyo a coordinación.");
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

            model.Email = model.Email.Trim();
            model.Matricula = model.Matricula.Trim();
            model.Nombre = model.Nombre.Trim();
            model.Apellidos = model.Apellidos.Trim();
            model.Carrera = model.Carrera.Trim();
            model.CodigoVinculacion = model.CodigoVinculacion?.Trim();

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Ya existe una cuenta con este correo.");
                return View(model);
            }

            var alumnoExistente = await _context.Alumnos.FirstOrDefaultAsync(a => a.Matricula == model.Matricula);
            if (alumnoExistente?.UserId != null)
            {
                ModelState.AddModelError(nameof(model.Matricula), "Esta boleta ya tiene una cuenta vinculada. Si perdiste acceso, usa recuperacion de contrasena o contacta a coordinacion.");
                return View(model);
            }

            var isInstitucional = model.Email.EndsWith("@alumno.ipn.mx", StringComparison.OrdinalIgnoreCase) || model.Email.EndsWith("@ipn.mx", StringComparison.OrdinalIgnoreCase);
            var vinculaConCodigo = false;
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                IsActive = true,
                EmailConfirmed = isInstitucional,
                PendienteVerificacion = true
            };

            if (alumnoExistente != null && !string.IsNullOrWhiteSpace(model.CodigoVinculacion))
            {
                var codeHash = HashClaimCode(model.CodigoVinculacion);
                var claim = await _context.AlumnoClaimCodes
                    .FirstOrDefaultAsync(c => c.Matricula == model.Matricula && c.CodeHash == codeHash && c.UsedAt == null && c.ExpiresAt > DateTime.Now);
                if (claim != null)
                {
                    user.PendienteVerificacion = false;
                    user.EmailConfirmed = true;
                    vinculaConCodigo = true;
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
                if (alumnoExistente != null && vinculaConCodigo)
                {
                    var claim = await _context.AlumnoClaimCodes
                        .FirstOrDefaultAsync(c => c.Matricula == model.Matricula && c.CodeHash == HashClaimCode(model.CodigoVinculacion ?? "") && c.UsedAt == null && c.ExpiresAt > DateTime.Now);
                    alumnoExistente.UserId = user.Id;
                    alumnoExistente.Correo = model.Email;
                    if (claim != null)
                    {
                        claim.UsedAt = DateTime.Now;
                        claim.UsedByUserId = user.Id;
                    }
                }
                else
                {
                    var solicitudDuplicada = await _context.AccountLinkRequests.AnyAsync(r =>
                        r.Estado == "Pendiente" &&
                        (r.Email == model.Email || r.Matricula == model.Matricula));
                    if (!solicitudDuplicada)
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

                if (!user.EmailConfirmed)
                {
                    await SendConfirmationEmailAsync(user);
                }

                if (user.PendienteVerificacion)
                {
                    return RedirectToAction("Login", new
                    {
                        registro = "pendiente",
                        expediente = alumnoExistente == null ? "faltante" : "existente"
                    });
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
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Ingresa el correo registrado en tu cuenta.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { email = user.Email, code }, protocol: Request.Scheme);

            await _emailDeliveryService.SendAsync(user.Email!, "Restablece tu contraseña", $"<p>Solicitaste restablecer tu contraseña.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(callbackUrl)}\">Restablecer contraseña</a></p>");

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

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, code);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Correo confirmado. Ahora espera la validación institucional de tu expediente."
                : "No fue posible confirmar el correo. Solicita un enlace nuevo a coordinación.";
            return RedirectToAction(nameof(Login));
        }

        private async Task SendConfirmationEmailAsync(ApplicationUser user)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var url = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code }, Request.Scheme);
            if (url != null)
            {
                await _emailDeliveryService.SendAsync(user.Email!, "Confirma tu correo de Rescate Académico", $"<p>Confirma tu correo para continuar con tu solicitud de acceso.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">Confirmar correo</a></p>");
            }
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
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo valido.")]
        [StringLength(120, ErrorMessage = "El correo no puede superar 120 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contrasena es obligatoria.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contrasena debe tener entre 8 y 100 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma tu contrasena.")]
        [Compare("Password", ErrorMessage = "Las contrasenas no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La boleta es obligatoria.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "La boleta debe tener exactamente 10 digitos.")]
        public string Matricula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 80 caracteres.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ\s'.-]+$", ErrorMessage = "El nombre solo puede contener letras y espacios.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 100 caracteres.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ\s'.-]+$", ErrorMessage = "Los apellidos solo pueden contener letras y espacios.")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "La carrera es obligatoria.")]
        [RegularExpression(@"^(Técnico en Administración|Técnico en Administración de Empresas Turísticas|Técnico en Contaduría|Técnico en Gastronomía|Técnico en Gestión de la Ciberseguridad|Técnico en Informática)$", ErrorMessage = "Selecciona una carrera valida.")]
        public string Carrera { get; set; } = string.Empty;

        [Range(1, 6, ErrorMessage = "El semestre debe estar entre 1 y 6.")]
        public int SemestreActual { get; set; } = 1;

        [StringLength(40, ErrorMessage = "El codigo de vinculacion no puede superar 40 caracteres.")]
        public string? CodigoVinculacion { get; set; }
    }
}
