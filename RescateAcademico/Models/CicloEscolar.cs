using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class CicloEscolar
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Nombre { get; set; } = null!;

        [MaxLength(10)]
        public string? Periodo { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }

        public bool EstaActivo { get; set; } = true;

        public bool EsActual { get; set; } = false;
    }
}
