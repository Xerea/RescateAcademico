using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RescateAcademico.Models
{
    public class Calificacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AlumnoMatricula { get; set; } = null!;
        public Alumno? Alumno { get; set; }

        public int MateriaId { get; set; }
        public Materia? Materia { get; set; }

        [MaxLength(10)]
        public string? Periodo { get; set; }

        [MaxLength(10)]
        public string? CicloEscolar { get; set; }

        public decimal? Valor { get; set; }

        public int? VecesCursada { get; set; } = 1;

        public bool Aprobada { get; set; }

        public bool EsETS { get; set; }

        public DateTime? FechaEvaluacion { get; set; }

        [MaxLength(20)]
        public string Tipo { get; set; } = "Ordinario";
    }
}
