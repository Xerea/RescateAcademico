using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RescateAcademico.Models
{
    public class Convocatoria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Titulo { get; set; } = null!;

        public string? Descripcion { get; set; }

        public string Tipo { get; set; } = null!; // ServicioSocial, Investigacion, Laboratorio, ApoyoAcademico

        public int? ProyectoId { get; set; }
        public Proyecto? Proyecto { get; set; }

        public int CupoMaximo { get; set; }

        public int PostulacionesActuales { get; set; } = 0;

        public DateTime FechaPublicacion { get; set; } = DateTime.Now;

        public DateTime FechaCierre { get; set; }

        public string? Requisitos { get; set; }

        public decimal? PromedioMinimo { get; set; }

        public int? SemestreMinimo { get; set; }

        public string? CarreraRequerida { get; set; }

        public bool EstaActiva { get; set; } = true;

        public bool ValidadaPorAcademia { get; set; } = false;

        public string? Area { get; set; }

        // Modalidad: Presencial, En linea, Hibrida
        [MaxLength(20)]
        public string? Modalidad { get; set; }

        public string? Ubicacion { get; set; }

        public string? Horario { get; set; }

        public string? RequisitosTecnicos { get; set; }

        public ICollection<Postulacion> Postulaciones { get; set; } = new List<Postulacion>();
    }
}
