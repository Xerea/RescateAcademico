using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class PrediccionDesercion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AlumnoMatricula { get; set; } = null!;

        public decimal? ProbabilidadDesercion { get; set; }

        public string? NivelRiesgo { get; set; } // Bajo, Medio, Alto, Critico

        public string? FactoresDetectados { get; set; }

        public string? Recomendaciones { get; set; }

        public int? AusenciasTotales { get; set; }

        public decimal? PromedioParcial { get; set; }

        public int? MateriasReprobadas { get; set; }

        public bool IntervencionRealizada { get; set; } = false;

        public DateTime FechaPrediccion { get; set; } = DateTime.Now;

        public string? PeriodoEvaluado { get; set; }
    }
}
