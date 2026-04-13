using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class Autenticacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        public DateTime FechaIntento { get; set; } = DateTime.Now;

        public bool Exitosa { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public int IntentosFallidos { get; set; } = 0;
    }
}
