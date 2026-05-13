using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;
using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
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

            var recaptchaResponse = Request.Form["g-recaptcha-response"].FirstOrDefault();
            if (!await VerifyRecaptchaAsync(recaptchaResponse))
            {
                ModelState.AddModelError(string.Empty, "Verificación de seguridad fallida. Por favor intenta de nuevo.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(email) || (!email.EndsWith("@ipn.mx", StringComparison.OrdinalIgnoreCase) && !email.EndsWith("@alumno.ipn.mx", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(string.Empty, "Debes iniciar sesión con tu correo institucional (@ipn.mx o @alumno.ipn.mx).");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
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

            if (await _context.Alumnos.AnyAsync(a => a.Matricula == model.Matricula))
            {
                ModelState.AddModelError(nameof(model.Matricula), "Ya existe un alumno con esta boleta.");
                return View(model);
            }

            var isInstitucional = model.Email.EndsWith("@alumno.ipn.mx", StringComparison.OrdinalIgnoreCase) || model.Email.EndsWith("@ipn.mx", StringComparison.OrdinalIgnoreCase);
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                IsActive = true,
                PendienteVerificacion = !isInstitucional
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Alumno");
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
            var secretKey = _configuration["RECAPTCHA_SECRET_KEY"];
            if (string.IsNullOrEmpty(secretKey))
                return true; // reCAPTCHA not configured — allow login (development)

            if (string.IsNullOrEmpty(token))
                return false;

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
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var success = doc.RootElement.GetProperty("success").GetBoolean();
                var score = doc.RootElement.TryGetProperty("score", out var scoreEl) ? scoreEl.GetDouble() : 0;
                return success && score >= 0.5;
            }
            catch
            {
                return true; // Fail open — don't block users if verification service is down
            }
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
        [StringLength(100, MinimumLength = 6)]
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
    }
}
