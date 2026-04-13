using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class Carrera
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Clave { get; set; } = null!;

        [Required]
        public string Nombre { get; set; } = null!;

        [MaxLength(100)]
        public string? Descripcion { get; set; }

        public int Semestres { get; set; } = 6;

        public bool EstaActiva { get; set; } = true;
    }
}
