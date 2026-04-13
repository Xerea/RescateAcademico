using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RescateAcademico.Models
{
    public class Tutor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; } = null!;

        [Required]
        public string Apellidos { get; set; } = null!;

        [Required]
        public string Email { get; set; } = null!;

        [MaxLength(20)]
        public string? Telefono { get; set; }

        public string? Especialidad { get; set; }

        public bool EstaActivo { get; set; } = true;

        [ForeignKey("Usuario")]
        public string? UserId { get; set; }
        public ApplicationUser? Usuario { get; set; }

        public ICollection<AsignacionTutor> AsignacionesTutor { get; set; } = new List<AsignacionTutor>();
    }
}
