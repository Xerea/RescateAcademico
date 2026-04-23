using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class Grupo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Clave { get; set; } = null!; // e.g., "6IV8", "3IM2"

        [Required]
        public string Carrera { get; set; } = null!;

        public int Semestre { get; set; }

        [MaxLength(15)]
        public string Turno { get; set; } = "Vespertino"; // Matutino o Vespertino

        public int NumeroGrupo { get; set; }

        public int? ProfesorId { get; set; }
        public Tutor? Profesor { get; set; }

        public ICollection<Alumno> Alumnos { get; set; } = new List<Alumno>();
    }
}
