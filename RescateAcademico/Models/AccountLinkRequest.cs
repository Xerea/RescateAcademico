using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class AccountLinkRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Matricula { get; set; } = string.Empty;

        [Required]
        public string NombreSolicitado { get; set; } = string.Empty;

        public string Estado { get; set; } = "Pendiente";

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        public string? RevisadoPorUserId { get; set; }

        public DateTime? FechaRevision { get; set; }

        public string? MotivoRechazo { get; set; }
    }
}
