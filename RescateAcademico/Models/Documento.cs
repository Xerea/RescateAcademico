using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RescateAcademico.Models
{
    public class Documento
    {
        [Key]
        public int Id { get; set; }

        public int PostulacionId { get; set; }
        public Postulacion? Postulacion { get; set; }

        [Required]
        public string NombreOriginal { get; set; } = null!;

        [Required]
        public string NombreGuardado { get; set; } = null!;

        [Required]
        public string Ruta { get; set; } = null!;

        public long Tamano { get; set; }

        [MaxLength(10)]
        public string? Tipo { get; set; }

        public DateTime FechaSubida { get; set; } = DateTime.Now;

        [Required]
        public string UserId { get; set; } = null!;
    }
}
