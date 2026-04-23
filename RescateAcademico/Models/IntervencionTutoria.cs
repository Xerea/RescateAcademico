using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RescateAcademico.Models
{
    public class IntervencionTutoria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TutorId { get; set; }
        public Tutor? Tutor { get; set; }

        [Required]
        public string AlumnoMatricula { get; set; } = null!;
        public Alumno? Alumno { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string Tipo { get; set; } = "Acercamiento"; // Acercamiento, Tutoria, Canalizacion, Otro

        [Required]
        public string Descripcion { get; set; } = null!;

        public string? Resultado { get; set; }

        public bool RequiereSeguimiento { get; set; } = false;

        public DateTime? FechaSeguimiento { get; set; }

        public string? NotasSeguimiento { get; set; }
    }
}
