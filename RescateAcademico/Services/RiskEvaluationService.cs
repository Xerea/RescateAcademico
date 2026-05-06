using RescateAcademico.Models;

namespace RescateAcademico.Services
{
    public class RiskEvaluationService
    {
        public string CalcularRiesgo(Alumno alumno)
        {
            var promedio = alumno.PromedioGlobal;
            var reprobadas = alumno.MateriasReprobadas ?? 0;
            var ausencias = alumno.Ausencias ?? 0;
            var parcialesBajos = alumno.ParcialesBajos ?? 0;
            var ets = alumno.EtsPresentados ?? 0;
            var recursamientos = alumno.Recursamientos ?? 0;

            if (promedio < 6.0m || reprobadas >= 2 || ausencias > 5 || parcialesBajos >= 2 || ets >= 2 || recursamientos >= 2)
            {
                return "Rojo";
            }

            if (promedio < 7.5m || reprobadas == 1 || ausencias > 3 || parcialesBajos == 1 || ets == 1 || recursamientos == 1)
            {
                return "Amarillo";
            }

            return "Verde";
        }

        public decimal CalcularProbabilidadDesercion(Alumno alumno)
        {
            decimal score = 0.1m;

            if (alumno.PromedioGlobal < 6m) score += 0.45m;
            else if (alumno.PromedioGlobal < 7m) score += 0.25m;

            score += Math.Min(0.2m, (alumno.MateriasReprobadas ?? 0) * 0.04m);
            score += Math.Min(0.15m, (alumno.Recursamientos ?? 0) * 0.05m);
            score += Math.Min(0.15m, (alumno.Ausencias ?? 0) * 0.02m);
            if ((alumno.CargaAcademicaActual ?? 0) >= 7) score += 0.15m;

            return Math.Min(0.95m, Math.Round(score, 2));
        }

        public string CalcularNivelPredictivo(decimal probabilidad)
        {
            if (probabilidad >= 0.75m) return "Critico";
            if (probabilidad >= 0.55m) return "Alto";
            if (probabilidad >= 0.35m) return "Medio";
            return "Bajo";
        }

        public List<string> ObtenerFactoresRiesgo(Alumno alumno)
        {
            var factores = new List<string>();
            if (alumno.PromedioGlobal < 7m) factores.Add("Promedio bajo");
            if ((alumno.MateriasReprobadas ?? 0) > 2) factores.Add("Multiples materias reprobadas");
            if ((alumno.Recursamientos ?? 0) > 1) factores.Add("Recursamientos frecuentes");
            if ((alumno.Ausencias ?? 0) > 3) factores.Add("Ausencias elevadas");
            if ((alumno.CargaAcademicaActual ?? 0) >= 7) factores.Add("Carga academica alta");
            if (factores.Count == 0) factores.Add("Rendimiento estable");
            return factores;
        }

        public List<string> GenerarSugerencias(Alumno alumno)
        {
            var sugerencias = new List<string>();

            if (alumno.PromedioGlobal < 6.0m)
                sugerencias.Add("Tu promedio esta por debajo de 6.0. Se recomienda acudir a asesorias academicas.");

            if (alumno.PromedioGlobal < 7.0m && alumno.PromedioGlobal >= 6.0m)
                sugerencias.Add("Tu promedio requiere atencion. Considera solicitar tutorias.");

            if ((alumno.MateriasReprobadas ?? 0) > 3)
                sugerencias.Add($"Tienes {alumno.MateriasReprobadas} materias reprobadas. Es importante regularizarlas pronto.");

            if ((alumno.CargaAcademicaActual ?? 0) > 6)
                sugerencias.Add("Tu carga academica es alta. Evalua cuidadosamente antes de sumarte a un proyecto.");

            if ((alumno.Recursamientos ?? 0) > 2)
                sugerencias.Add($"Has recursado {alumno.Recursamientos} materias. Enfocate en aprobar desde el primer intento.");

            if (sugerencias.Count == 0)
                sugerencias.Add("Excelente. Manten tu buen desempeno academico.");

            return sugerencias;
        }
    }
}
