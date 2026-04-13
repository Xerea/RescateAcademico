using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class SugerenciaIA
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AlumnoMatricula { get; set; } = null!;

        public int? ConvocatoriaId { get; set; }

        public int? ProyectoId { get; set; }

        [Required]
        public string Tipo { get; set; } = null!; // ProyectoSugerido, MejoraAcademica, CargaAcademica

        [Required]
        public string Titulo { get; set; } = null!;

        public string? Descripcion { get; set; }

        public decimal? Puntuacion { get; set; }

        public string? Razonamiento { get; set; }

        public DateTime FechaGeneracion { get; set; } = DateTime.Now;

        public bool Mostrada { get; set; } = false;
    }
}
