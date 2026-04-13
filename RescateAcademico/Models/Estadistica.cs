using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class Estadistica
    {
        [Key]
        public int Id { get; set; }

        public string? Carrera { get; set; }

        public string? Periodo { get; set; }

        public int TotalAlumnos { get; set; }

        public int AlumnosInscritos { get; set; }

        public decimal? PromedioGeneral { get; set; }

        public int? MateriasReprobadasTotal { get; set; }

        public int? AlumnosEnRiesgo { get; set; }

        public int? AlumnosEnRiesgoAlto { get; set; }

        public decimal? TasaDesercion { get; set; }

        public decimal? TasaReprobacion { get; set; }

        public int? PostulacionesTotales { get; set; }

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}
