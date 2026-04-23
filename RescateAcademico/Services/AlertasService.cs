using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Services
{
    public class AlertasService
    {
        private readonly ApplicationDbContext _context;

        public AlertasService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Evalúa el riesgo académico de un alumno y genera alertas/notificaciones si cambia.
        /// </summary>
        public async Task<string> EvaluarYAlertarAsync(string alumnoMatricula)
        {
            var alumno = await _context.Alumnos
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.Matricula == alumnoMatricula);

            if (alumno == null) return "Alumno no encontrado";

            var riesgoAnterior = alumno.RiesgoAcademico;
            var riesgoNuevo = CalcularRiesgo(alumno);

            if (riesgoNuevo != riesgoAnterior)
            {
                alumno.RiesgoAcademico = riesgoNuevo;
                alumno.FechaUltimaActualizacion = DateTime.Now;
                _context.Alumnos.Update(alumno);

                // Notificar al alumno
                if (!string.IsNullOrEmpty(alumno.UserId))
                {
                    var notifAlumno = new Notificacion
                    {
                        UserId = alumno.UserId,
                        Titulo = $"Alerta de Riesgo Académico: {riesgoNuevo}",
                        Mensaje = GenerarMensajeRiesgo(alumno, riesgoNuevo, riesgoAnterior),
                        Tipo = riesgoNuevo == "Rojo" ? "Error" : riesgoNuevo == "Amarillo" ? "Advertencia" : "Exito",
                        Enlace = "/PerfilAcademico"
                    };
                    _context.Notificaciones.Add(notifAlumno);
                }

                // Notificar a tutores asignados
                var tutores = await _context.AsignacionesTutor
                    .Where(at => at.AlumnoMatricula == alumnoMatricula && at.EstaActiva)
                    .Include(at => at.Tutor)
                    .Select(at => at.Tutor)
                    .ToListAsync();

                foreach (var tutor in tutores)
                {
                    if (!string.IsNullOrEmpty(tutor?.UserId))
                    {
                        var notifTutor = new Notificacion
                        {
                            UserId = tutor.UserId,
                            Titulo = $"Cambio de Riesgo: {alumno.Nombre} {alumno.Apellidos}",
                            Mensaje = $"El alumno {alumno.Nombre} {alumno.Apellidos} ({alumno.Matricula}) cambió de '{riesgoAnterior ?? "Sin clasificar"}' a '{riesgoNuevo}'.",
                            Tipo = riesgoNuevo == "Rojo" ? "Error" : riesgoNuevo == "Amarillo" ? "Advertencia" : "Informacion",
                            Enlace = $"/Intervenciones/PorAlumno?matricula={alumno.Matricula}"
                        };
                        _context.Notificaciones.Add(notifTutor);
                    }
                }

                // Notificar a administradores
                var admins = await _context.UserRoles
                    .Where(ur => ur.RoleId == _context.Roles.First(r => r.Name == "Administrador").Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                foreach (var adminId in admins)
                {
                    var notifAdmin = new Notificacion
                    {
                        UserId = adminId,
                        Titulo = $"Alerta Institucional: Alumno en {riesgoNuevo}",
                        Mensaje = $"{alumno.Nombre} {alumno.Apellidos} ({alumno.Matricula}, {alumno.Carrera}) cambió a riesgo {riesgoNuevo}.",
                        Tipo = riesgoNuevo == "Rojo" ? "Error" : "Advertencia",
                        Enlace = $"/Alumnos/Detalles/{alumno.Matricula}"
                    };
                    _context.Notificaciones.Add(notifAdmin);
                }

                await _context.SaveChangesAsync();
                return $"Riesgo actualizado de '{riesgoAnterior ?? "Sin clasificar"}' a '{riesgoNuevo}'. Alertas enviadas.";
            }

            return $"Riesgo sin cambios: {riesgoNuevo}";
        }

        /// <summary>
        /// Evalúa todos los alumnos y genera alertas. Retorna resumen.
        /// </summary>
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
            var promedio = alumno.PromedioGlobal;
            var reprobadas = alumno.MateriasReprobadas ?? 0;
            var ausencias = alumno.Ausencias ?? 0;
            var parcialesBajos = alumno.ParcialesBajos ?? 0;
            var ets = alumno.EtsPresentados ?? 0;
            var recursamientos = alumno.Recursamientos ?? 0;

            // Riesgo Rojo
            if (promedio < 6.0m || reprobadas >= 2 || ausencias > 5 || parcialesBajos >= 2 || ets >= 2 || recursamientos >= 2)
                return "Rojo";

            // Riesgo Amarillo
            if (promedio < 7.5m || reprobadas == 1 || ausencias > 3 || parcialesBajos == 1 || ets == 1 || recursamientos == 1)
                return "Amarillo";

            return "Verde";
        }

        private static string GenerarMensajeRiesgo(Alumno alumno, string riesgoNuevo, string? riesgoAnterior)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Tu nivel de riesgo académico cambió de '{riesgoAnterior ?? "Sin clasificar"}' a '{riesgoNuevo}'.");
            sb.AppendLine();
            sb.AppendLine($"Promedio global: {alumno.PromedioGlobal:F2}");
            sb.AppendLine($"Materias reprobadas: {alumno.MateriasReprobadas ?? 0}");
            sb.AppendLine($"Ausencias: {alumno.Ausencias ?? 0}");
            sb.AppendLine($"Parciales bajos (<7): {alumno.ParcialesBajos ?? 0}");

            if (riesgoNuevo == "Rojo")
            {
                sb.AppendLine();
                sb.AppendLine("Se recomienda contactar inmediatamente a tu tutor académico o al Departamento de Apoyo Estudiantil.");
            }
            else if (riesgoNuevo == "Amarillo")
            {
                sb.AppendLine();
                sb.AppendLine("Se recomienda revisar tu plan de estudios y considerar asesorías académicas.");
            }

            return sb.ToString();
        }
    }
}
