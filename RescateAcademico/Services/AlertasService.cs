using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Services
{
    public class AlertasService
    {
        private readonly ApplicationDbContext _context;
        private readonly RiskEvaluationService _riskEvaluationService;
        private readonly NotificationService? _notificationService;

        public AlertasService(ApplicationDbContext context, RiskEvaluationService riskEvaluationService, NotificationService notificationService)
        {
            _context = context;
            _riskEvaluationService = riskEvaluationService;
            _notificationService = notificationService;
        }

        public AlertasService(ApplicationDbContext context)
        {
            _context = context;
            _riskEvaluationService = new RiskEvaluationService();
        }

        public async Task<string> EvaluarYAlertarAsync(string alumnoMatricula)
        {
            var alumno = await _context.Alumnos
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.Matricula == alumnoMatricula);

            if (alumno == null) return "Alumno no encontrado";

            var riesgoAnterior = alumno.RiesgoAcademico;
            var riesgoNuevo = _riskEvaluationService.CalcularRiesgo(alumno);

            if (riesgoNuevo != riesgoAnterior)
            {
                alumno.RiesgoAcademico = riesgoNuevo;
                alumno.FechaUltimaActualizacion = DateTime.Now;
                _context.Alumnos.Update(alumno);

                if (!string.IsNullOrEmpty(alumno.UserId))
                {
                    AddNotification(
                        alumno.UserId,
                        $"Alerta de Riesgo Academico: {riesgoNuevo}",
                        GenerarMensajeRiesgo(alumno, riesgoNuevo, riesgoAnterior),
                        riesgoNuevo == "Rojo" ? "Error" : riesgoNuevo == "Amarillo" ? "Advertencia" : "Exito",
                        "/PerfilAcademico");
                }

                var tutores = await _context.Grupos
                    .Where(g => g.Alumnos.Any(a => a.Matricula == alumnoMatricula) && g.Profesor != null)
                    .Select(g => g.Profesor!)
                    .Distinct()
                    .ToListAsync();

                foreach (var tutor in tutores)
                {
                    if (!string.IsNullOrEmpty(tutor?.UserId))
                    {
                        AddNotification(
                            tutor.UserId,
                            $"Cambio de Riesgo: {alumno.Nombre} {alumno.Apellidos}",
                            $"El alumno {alumno.Nombre} {alumno.Apellidos} ({alumno.Matricula}) cambio de '{riesgoAnterior ?? "Sin clasificar"}' a '{riesgoNuevo}'.",
                            riesgoNuevo == "Rojo" ? "Error" : riesgoNuevo == "Amarillo" ? "Advertencia" : "Informacion",
                            $"/Intervenciones/PorAlumno?matricula={alumno.Matricula}");
                    }
                }

                var adminRoleId = await _context.Roles
                    .Where(r => r.Name == "Administrador")
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(adminRoleId))
                {
                    var admins = await _context.UserRoles
                        .Where(ur => ur.RoleId == adminRoleId)
                        .Select(ur => ur.UserId)
                        .ToListAsync();

                    foreach (var adminId in admins)
                    {
                        AddNotification(
                            adminId,
                            $"Alerta Institucional: Alumno en {riesgoNuevo}",
                            $"{alumno.Nombre} {alumno.Apellidos} ({alumno.Matricula}, {alumno.Carrera}) cambio a riesgo {riesgoNuevo}.",
                            riesgoNuevo == "Rojo" ? "Error" : "Advertencia",
                            $"/Alumnos/Detalles/{alumno.Matricula}");
                    }
                }

                await _context.SaveChangesAsync();
                return $"Riesgo actualizado de '{riesgoAnterior ?? "Sin clasificar"}' a '{riesgoNuevo}'. Alertas enviadas.";
            }

            return $"Riesgo sin cambios: {riesgoNuevo}";
        }

        public async Task<(int evaluados, int cambios, List<string> detalles)> EvaluarTodosAsync()
        {
            var alumnos = await _context.Alumnos.ToListAsync();
            int cambios = 0;
            var detalles = new List<string>();

            foreach (var alumno in alumnos)
            {
                var resultado = await EvaluarYAlertarAsync(alumno.Matricula);
                if (resultado.Contains("actualizado"))
                {
                    cambios++;
                    detalles.Add(resultado);
                }
            }

            return (alumnos.Count, cambios, detalles);
        }

        public static string CalcularRiesgo(Alumno alumno)
        {
            return new RiskEvaluationService().CalcularRiesgo(alumno);
        }

        private static string GenerarMensajeRiesgo(Alumno alumno, string riesgoNuevo, string? riesgoAnterior)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Tu nivel de riesgo academico cambio de '{riesgoAnterior ?? "Sin clasificar"}' a '{riesgoNuevo}'.");
            sb.AppendLine();
            sb.AppendLine($"Promedio global: {alumno.PromedioGlobal:F2}");
            sb.AppendLine($"Materias reprobadas: {alumno.MateriasReprobadas ?? 0}");
            sb.AppendLine($"Ausencias: {alumno.Ausencias ?? 0}");
            sb.AppendLine($"Parciales bajos (<7): {alumno.ParcialesBajos ?? 0}");

            if (riesgoNuevo == "Rojo")
            {
                sb.AppendLine();
                sb.AppendLine("Se recomienda contactar inmediatamente a tu tutor academico o al Departamento de Apoyo Estudiantil.");
            }
            else if (riesgoNuevo == "Amarillo")
            {
                sb.AppendLine();
                sb.AppendLine("Se recomienda revisar tu plan de estudios y considerar asesorias academicas.");
            }

            return sb.ToString();
        }

        private void AddNotification(string userId, string titulo, string mensaje, string tipo, string? enlace = null)
        {
            if (_notificationService != null)
            {
                _notificationService.Add(userId, titulo, mensaje, tipo, enlace);
                return;
            }

            _context.Notificaciones.Add(new Notificacion
            {
                UserId = userId,
                Titulo = titulo,
                Mensaje = mensaje,
                Tipo = tipo,
                Enlace = enlace
            });
        }
    }
}
