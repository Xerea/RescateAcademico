using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class ReporteCosecovi
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AlumnoMatricula { get; set; } = null!;
        public Alumno? Alumno { get; set; }

        [Required]
        public string Periodo { get; set; } = null!; // e.g., "2026-A", "2025-B"

        public DateTime FechaReporte { get; set; } = DateTime.Now;

        public string? SituacionObservada { get; set; }

        public string? Recomendaciones { get; set; }

        public string? AccionesPropuestas { get; set; }

        public string? Canalizacion { get; set; } // e.g., "Psicología", "Trabajo Social", "Orientación Educativa"

        [MaxLength(20)]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Atendido, Seguimiento

        public string? ElaboradoPor { get; set; }
    }
}
