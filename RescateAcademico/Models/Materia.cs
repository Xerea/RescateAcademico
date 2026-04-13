using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RescateAcademico.Models
{
    public class Materia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Clave { get; set; } = null!;

        [Required]
        public string Nombre { get; set; } = null!;

        public int Creditos { get; set; }

        public int Semestre { get; set; }

        public string? Carrera { get; set; }

        public ICollection<Calificacion> Calificaciones { get; set; } = new List<Calificacion>();
    }
}
