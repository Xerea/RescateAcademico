using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class Notificacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string Titulo { get; set; } = null!;

        [Required]
        public string Mensaje { get; set; } = null!;

        public string? Tipo { get; set; } = "Informacion";

        public bool Leida { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public string? Enlace { get; set; }

        public int? ReferenciaId { get; set; }
    }
}
