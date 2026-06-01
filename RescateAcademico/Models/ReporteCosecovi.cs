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
        public string Periodo { get; set; } = null!;

        public DateTime FechaReporte { get; set; } = DateTime.Now;

        public DateTime FechaIncidente { get; set; } = DateTime.Now;

        [MaxLength(60)]
        public string TipoIncidente { get; set; } = "Conducta disruptiva";

        [MaxLength(20)]
        public string Gravedad { get; set; } = "Media";

        [MaxLength(80)]
        public string? Lugar { get; set; }

        public string? SituacionObservada { get; set; }

        public string? Recomendaciones { get; set; }

        public string? AccionesPropuestas { get; set; }

        public string? MedidasTomadas { get; set; }

        public bool TutorNotificado { get; set; }

        public bool PadreTutorCitado { get; set; }

        public string? Canalizacion { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "Registrado";

        public string? ElaboradoPor { get; set; }
    }
}
