using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class DictamenAcademico
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AlumnoMatricula { get; set; } = null!;
        public Alumno? Alumno { get; set; }

        [Required]
        [MaxLength(50)]
        public string Tipo { get; set; } = null!; // Baja Temporal, Baja Definitiva, Alta, Cambio de Carrera, Dictamen Técnico, etc.

        [Required]
        public string Descripcion { get; set; } = null!;

        public DateTime FechaEmision { get; set; } = DateTime.Now;

        public DateTime? FechaResolucion { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "Activo"; // Activo, Resuelto, Cancelado

        public string? EmitidoPor { get; set; }

        public string? Observaciones { get; set; }
    }
}
