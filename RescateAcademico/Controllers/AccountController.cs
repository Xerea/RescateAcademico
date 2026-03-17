using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RescateAcademico.Models;
using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (!string.IsNullOrWhiteSpace(email) && !email.EndsWith("@ipn.mx", StringComparison.OrdinalIgnoreCase) && !email.EndsWith("@alumno.ipn.mx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Debes iniciar sesión con tu correo institucional (@ipn.mx o @alumno.ipn.mx).");
            }
            
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null && !user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Esta cuenta no está activa.");
                    return View();
                }

                var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Admin"))
                    {
                        return RedirectToAction("AdminDashboard", "Dashboard");
                    }
                    else if (roles.Contains("Tutor"))
                    {
                        return RedirectToAction("TutorDashboard", "Dashboard");
                    }
                    else if (roles.Contains("Authority"))
                    {
                        return RedirectToAction("AuthorityDashboard", "Dashboard");
                    }
                    else
                    {
                        return RedirectToAction("StudentDashboard", "Dashboard");
                    }
                }
                
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "La cuenta ha sido bloqueada temporalmente por demasiados intentos fallidos. Intente de nuevo en 20 minutos.");
                    return View();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                    return View();
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
        
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        
        // --- HU-RA-02: Recuperación de Contraseña ---
        
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (!string.IsNullOrWhiteSpace(email) && !email.EndsWith("@ipn.mx", StringComparison.OrdinalIgnoreCase) && !email.EndsWith("@alumno.ipn.mx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Solo se aceptan correos institucionales registrados (@ipn.mx o @alumno.ipn.mx).");
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // No revelar que el usuario no existe (seguridad)
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // En un proyecto real esto enviaría un correo. Para práctica local,
                // utilizamos un enlace temporal que el docente puede revisar.
                var callbackUrl = Url.Action("ResetPassword", "Account", new { email = user.Email, code }, protocol: Request.Scheme);
                
                TempData["ResetLink"] = callbackUrl; 
                
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            return View(email);
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

            // Evitar rehusar la contraseña anterior (Lógica manual aproximada)
            if (await _userManager.CheckPasswordAsync(user, model.Password))
            {
                 ModelState.AddModelError(string.Empty, "No puedes usar la misma contraseña que la anterior.");
                 return View(model);
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

        // --- Registro de Nuevo Usuario Temporal ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, IsActive = true, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, model.Password);
                
                if (result.Succeeded)
                {
                    // Asignamos el rol de Alumno por defecto.
                    await _userManager.AddToRoleAsync(user, "Alumno");
                    
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Dashboard");
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }
    }
    
    // View Models para Account
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "Correo inválido")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} caracteres.", MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
        
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
