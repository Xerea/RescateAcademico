using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RescateAcademico.Models
{
    public class Alumno
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // La boleta es la llave
        public string Matricula { get; set; } = null!;
        
        [Required]
        public string Nombre { get; set; } = null!;
        
        [Required]
        public string Apellidos { get; set; } = null!;
        
        public string Carrera { get; set; } = null!;
        
        public decimal PromedioGlobal { get; set; }
        
        public int SemestreActual { get; set; }
        
        public string? RiesgoAcademico { get; set; } // Verde, Amarillo, Rojo

        [ForeignKey("Usuario")]
        public string? UserId { get; set; }
        public ApplicationUser? Usuario { get; set; }
    }
}
