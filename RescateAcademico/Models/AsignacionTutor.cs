using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RescateAcademico.Models
{
    public class AsignacionTutor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TutorId { get; set; }
        public Tutor? Tutor { get; set; }

        [Required]
        public string AlumnoMatricula { get; set; } = null!;
        public Alumno? Alumno { get; set; }

        public DateTime FechaAsignacion { get; set; } = DateTime.Now;

        public string? Periodo { get; set; }

        public bool EstaActiva { get; set; } = true;
    }
}
