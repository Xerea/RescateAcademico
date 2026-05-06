using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(string userId, string titulo, string mensaje, string tipo = "Informacion", string? enlace = null, int? referenciaId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            _context.Notificaciones.Add(new Notificacion
            {
                UserId = userId,
                Titulo = titulo,
                Mensaje = mensaje,
                Tipo = tipo,
                Enlace = enlace,
                ReferenciaId = referenciaId
            });
        }
    }
}
