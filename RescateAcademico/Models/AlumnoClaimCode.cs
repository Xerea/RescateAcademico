using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class AlumnoClaimCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Matricula { get; set; } = string.Empty;

        [Required]
        public string CodeHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; } = DateTime.Now.AddMonths(6);

        public DateTime? UsedAt { get; set; }

        public string? UsedByUserId { get; set; }

        public string CreatedBy { get; set; } = "DemoDataSeeder";
    }
}
