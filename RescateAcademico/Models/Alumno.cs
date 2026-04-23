using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RescateAcademico.Models
{
    public class Alumno
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Matricula { get; set; } = null!;
        
        [Required]
        public string Nombre { get; set; } = null!;
        
        [Required]
        public string Apellidos { get; set; } = null!;
        
        public string? Carrera { get; set; }
        
        public decimal PromedioGlobal { get; set; }
        
        public int SemestreActual { get; set; } = 1;
        
        public string? RiesgoAcademico { get; set; }

        public int? CargaAcademicaActual { get; set; }

        public int? MateriasReprobadas { get; set; }

        public int? EtsPresentados { get; set; }

        public int? Recursamientos { get; set; }

        public int? Ausencias { get; set; } = 0;

        public int? ParcialesBajos { get; set; } = 0;

        public DateTime? FechaUltimaActualizacion { get; set; }

        public string? Estatus { get; set; } = "Activo";

        public string? Correo { get; set; }

        [ForeignKey("Usuario")]
        public string? UserId { get; set; }
        public ApplicationUser? Usuario { get; set; }

        public int? GrupoId { get; set; }
        public Grupo? Grupo { get; set; }

        public ICollection<Calificacion> Calificaciones { get; set; } = new List<Calificacion>();
        public ICollection<AsignacionTutor> TutoresAsignados { get; set; } = new List<AsignacionTutor>();
        public ICollection<Postulacion> Postulaciones { get; set; } = new List<Postulacion>();
        public ICollection<DictamenAcademico> Dictamenes { get; set; } = new List<DictamenAcademico>();
        public ICollection<ReporteCosecovi> ReportesCosecovi { get; set; } = new List<ReporteCosecovi>();
    }
}
