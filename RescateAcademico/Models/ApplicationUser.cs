using Microsoft.AspNetCore.Identity;

namespace RescateAcademico.Models
{
    public class ApplicationUser : IdentityUser
    {
        // HU-RA-01: "Acceso disponible solo a matriculados activos"
        public bool IsActive { get; set; } = true;
    }
}
