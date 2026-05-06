using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Services
{
    public class ConvocatoriaEligibilityService
    {
        private readonly ApplicationDbContext _context;

        public ConvocatoriaEligibilityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EligibilityResult> EvaluarAsync(Alumno alumno, Convocatoria convocatoria)
        {
            if (!convocatoria.EstaActiva || convocatoria.FechaCierre < DateTime.Now)
            {
                return EligibilityResult.Fail("Esta convocatoria ya esta cerrada");
            }

            if (convocatoria.CupoMaximo > 0 && convocatoria.PostulacionesActuales >= convocatoria.CupoMaximo)
            {
                return EligibilityResult.Fail("Esta convocatoria ya no tiene cupo disponible");
            }

            if (convocatoria.PromedioMinimo.HasValue && alumno.PromedioGlobal < convocatoria.PromedioMinimo.Value)
            {
                return EligibilityResult.Fail($"Tu promedio ({alumno.PromedioGlobal:F2}) no cumple con el minimo requerido ({convocatoria.PromedioMinimo:F2})");
            }

            if (convocatoria.SemestreMinimo.HasValue && alumno.SemestreActual < convocatoria.SemestreMinimo.Value)
            {
                return EligibilityResult.Fail($"No cumples el semestre minimo requerido ({convocatoria.SemestreMinimo}).");
            }

            if (!string.IsNullOrWhiteSpace(convocatoria.CarreraRequerida) &&
                !string.Equals(convocatoria.CarreraRequerida, alumno.Carrera, StringComparison.OrdinalIgnoreCase))
            {
                return EligibilityResult.Fail($"La convocatoria requiere carrera '{convocatoria.CarreraRequerida}' y tu carrera actual es '{alumno.Carrera}'.");
            }

            if ((alumno.CargaAcademicaActual ?? 0) >= 8 && alumno.PromedioGlobal < 8.0m)
            {
                return EligibilityResult.Fail("Tu carga academica actual es alta. Se recomienda regularizar materias antes de postularte a mas actividades.");
            }

            var yaPostulado = await _context.Postulaciones
                .AnyAsync(p => p.AlumnoId == alumno.Matricula && p.ProyectoId == convocatoria.ProyectoId);

            if (yaPostulado)
            {
                return EligibilityResult.Fail("Ya te has postulado a este proyecto");
            }

            return EligibilityResult.Success();
        }
    }

    public class EligibilityResult
    {
        public bool IsEligible { get; init; }
        public string? Message { get; init; }

        public static EligibilityResult Success() => new() { IsEligible = true };

        public static EligibilityResult Fail(string message) => new() { IsEligible = false, Message = message };
    }
}
