using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RescateAcademico.Models
{
    public class PlanMejora
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AlumnoMatricula { get; set; } = null!;
        public Alumno? Alumno { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaCierre { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "Activo"; // Activo, Cumplido, Vencido, Cancelado

        public string? Recomendaciones { get; set; }

        public string? Metas { get; set; }

        public string? AccionesTomadas { get; set; }

        public int? TutorId { get; set; }
        public Tutor? Tutor { get; set; }
    }
}
