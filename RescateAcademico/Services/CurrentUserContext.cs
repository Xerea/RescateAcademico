using System.Security.Claims;

namespace RescateAcademico.Services
{
    public class CurrentUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

        public string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public string? Email => User.Identity?.Name;

        public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

        public bool IsInRole(string role) => User.IsInRole(role);

        public bool IsAdminOrAutoridad => IsInRole("Administrador") || IsInRole("Autoridad");

        public bool IsTutor => IsInRole("Tutor");

        public bool IsAlumno => IsInRole("Alumno");
    }
}
