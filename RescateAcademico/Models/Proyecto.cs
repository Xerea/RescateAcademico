using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class Proyecto
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Titulo { get; set; } = null!;
        
        [Required]
        public string Descripcion { get; set; } = null!;
        
        public string Tipo { get; set; } = null!;
        
        public int CupoMaximo { get; set; }
        
        public DateTime FechaCierre { get; set; }
        
        public bool EstaActivo { get; set; } = true;

        public ICollection<Postulacion> Postulaciones { get; set; } = new List<Postulacion>();
    }
    
    public class Postulacion
    {
        [Key]
        public int Id { get; set; }
        
        public string AlumnoId { get; set; } = null!;
        public Alumno? Alumno { get; set; }
        
        public int ProyectoId { get; set; }
        public Proyecto? Proyecto { get; set; }
        
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        
        public string Estado { get; set; } = "En Revisión";

        public string? DocumentoNombre { get; set; }

        public string? DocumentoRuta { get; set; }

        public long? DocumentoTamano { get; set; }
    }
}
