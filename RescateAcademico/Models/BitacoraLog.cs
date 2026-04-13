using System.ComponentModel.DataAnnotations;

namespace RescateAcademico.Models
{
    public class BitacoraLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = null!;

        public string? UsuarioEmail { get; set; }

        [Required]
        public string Accion { get; set; } = null!;

        [Required]
        public string TablaAfectada { get; set; } = null!;

        public string? RegistroAnterior { get; set; }

        public string? RegistroNuevo { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;
    }
}
