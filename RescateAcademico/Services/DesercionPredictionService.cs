using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RescateAcademico.Services
{
    public class DesercionPredictionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DesercionPredictionService> _logger;
        private readonly HttpClient _httpClient;

        public DesercionPredictionService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<DesercionPredictionService> logger,
            HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public bool IsAvailable => true; // Heuristics are always available

        public DesercionOutput Predecir(Alumno alumno)
        {
            var prob = CalcularHeuristica(alumno);
            return new DesercionOutput
            {
                Prediccion = prob > 0.5m,
                Probability = (float)prob,
                EsHeuristica = true
            };
        }

        public async Task<List<PrediccionAlumnoViewModel>> PredecirTodosAsync()
        {
            var alumnos = await _context.Alumnos.ToListAsync();
            var resultados = new List<PrediccionAlumnoViewModel>();

            foreach (var alumno in alumnos)
            {
                var pred = Predecir(alumno);
                resultados.Add(new PrediccionAlumnoViewModel
                {
                    Matricula = alumno.Matricula,
                    Nombre = $"{alumno.Nombre} {alumno.Apellidos}",
                    Carrera = alumno.Carrera,
                    Promedio = alumno.PromedioGlobal,
                    ProbabilidadDesercion = pred.Probabilidad,
                    Riesgo = pred.Probabilidad > 0.7m ? "Crítico" : pred.Probabilidad > 0.5m ? "Alto" : pred.Probabilidad > 0.3m ? "Medio" : "Bajo",
                    Metodo = "Heurística Institucional",
                    Color = pred.Probabilidad > 0.7m ? "danger" : pred.Probabilidad > 0.5m ? "warning" : pred.Probabilidad > 0.3m ? "info" : "success"
                });
            }

            return resultados.OrderByDescending(r => r.ProbabilidadDesercion).ToList();
        }

        /// <summary>
        /// Genera un análisis narrativo personalizado usando OpenAI GPT-4o-mini.
        /// Requiere OPENAI_API_KEY en las variables de entorno.
        /// </summary>
        public async Task<AnalisisIAResultado?> GenerarAnalisisIAAsync(Alumno alumno)
        {
            var apiKey = _configuration["OPENAI_API_KEY"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OPENAI_API_KEY no configurada. Análisis IA no disponible.");
                return null;
            }

            var prompt = BuildPrompt(alumno);

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "Eres un consejero académico experimentado del Instituto Politécnico Nacional (IPN). Analizas perfiles estudiantiles y generas recomendaciones prácticas, empáticas y específicas en español mexicano. Mantén un tono profesional pero cercano." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 600
            };

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(content))
                    return null;

                // Parse structured response
                return ParsearRespuestaIA(content, alumno);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al llamar a OpenAI para análisis de alumno {Matricula}", alumno.Matricula);
                return null;
            }
        }

        private static string BuildPrompt(Alumno alumno)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Analiza el siguiente perfil estudiantil del IPN:");
            sb.AppendLine($"- Nombre: {alumno.Nombre} {alumno.Apellidos}");
            sb.AppendLine($"- Carrera: {alumno.Carrera}");
            sb.AppendLine($"- Semestre: {alumno.SemestreActual}");
            sb.AppendLine($"- Promedio Global: {alumno.PromedioGlobal:F2}");
            sb.AppendLine($"- Materias reprobadas: {alumno.MateriasReprobadas ?? 0}");
            sb.AppendLine($"- Ausencias: {alumno.Ausencias ?? 0}");
            sb.AppendLine($"- Parciales bajos: {alumno.ParcialesBajos ?? 0}");
            sb.AppendLine($"- ETS presentados: {alumno.EtsPresentados ?? 0}");
            sb.AppendLine($"- Recursamientos: {alumno.Recursamientos ?? 0}");
            sb.AppendLine($"- Carga académica actual: {alumno.CargaAcademicaActual ?? 0} materias");
            sb.AppendLine($"- Riesgo académico institucional: {alumno.RiesgoAcademico ?? "No evaluado"}");
            sb.AppendLine();
            sb.AppendLine("Genera una respuesta estructurada en español con ESTAS SECCIONES EXACTAS:");
            sb.AppendLine("RESUMEN_RIESGO: Una oración evaluando el nivel de riesgo general.");
            sb.AppendLine("ANALISIS: 2-3 oraciones explicando los factores clave que influyen en el riesgo.");
            sb.AppendLine("RECOMENDACIONES: Lista numerada (1, 2, 3) con 3 recomendaciones específicas y accionables.");
            sb.AppendLine("ALERTAS: Menciona si requiere tutoría, canalización psicológica, o reducción de carga académica. Si no hay alertas, escribe 'Ninguna'.");
            return sb.ToString();
        }

        private static AnalisisIAResultado ParsearRespuestaIA(string content, Alumno alumno)
        {
            var resultado = new AnalisisIAResultado
            {
                Matricula = alumno.Matricula,
                FechaGeneracion = DateTime.Now
            };

            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var buffer = new List<string>();

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("RESUMEN_RIESGO:", StringComparison.OrdinalIgnoreCase))
                {
                    buffer.Clear();
                    var val = line.Substring("RESUMEN_RIESGO:".Length).Trim();
                    if (!string.IsNullOrEmpty(val)) resultado.ResumenRiesgo = val;
                }
                else if (line.StartsWith("ANALISIS:", StringComparison.OrdinalIgnoreCase))
                {
                    buffer.Clear();
                    var val = line.Substring("ANALISIS:".Length).Trim();
                    if (!string.IsNullOrEmpty(val)) buffer.Add(val);
                }
                else if (line.StartsWith("RECOMENDACIONES:", StringComparison.OrdinalIgnoreCase))
                {
                    buffer.Clear();
                }
                else if (line.StartsWith("ALERTAS:", StringComparison.OrdinalIgnoreCase))
                {
                    buffer.Clear();
                    var val = line.Substring("ALERTAS:".Length).Trim();
                    if (!string.IsNullOrEmpty(val)) resultado.Alertas = val;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        buffer.Add(line);
                }
            }

            // Post-process sections that accumulated in buffer
            // (simplified: just store full text if parsing is ambiguous)
            if (string.IsNullOrEmpty(resultado.ResumenRiesgo))
                resultado.ResumenRiesgo = "Análisis de riesgo generado por IA.";
            if (string.IsNullOrEmpty(resultado.Analisis))
                resultado.Analisis = content;
            if (string.IsNullOrEmpty(resultado.Alertas))
                resultado.Alertas = "Ninguna";

            return resultado;
        }

        private static decimal CalcularHeuristica(Alumno a)
        {
            double score = 0;
            score += Math.Max(0, (7.0 - (double)a.PromedioGlobal) * 0.15);
            score += (a.MateriasReprobadas ?? 0) * 0.12;
            score += (a.Ausencias ?? 0) * 0.04;
            score += (a.ParcialesBajos ?? 0) * 0.08;
            score += (a.EtsPresentados ?? 0) * 0.06;
            score += (a.Recursamientos ?? 0) * 0.07;
            return Math.Min(0.99m, Math.Round((decimal)score, 2));
        }
    }

    public class DesercionOutput
    {
        public bool Prediccion { get; set; }
        public float Probability { get; set; }
        public decimal Probabilidad => Math.Round((decimal)Probability, 2);
        public bool EsHeuristica { get; set; } = false;
    }

    public class PrediccionAlumnoViewModel
    {
        public string Matricula { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string? Carrera { get; set; }
        public decimal Promedio { get; set; }
        public decimal ProbabilidadDesercion { get; set; }
        public string Riesgo { get; set; } = "";
        public string Metodo { get; set; } = "";
        public string Color { get; set; } = "";
    }

    public class AnalisisIAResultado
    {
        public string Matricula { get; set; } = "";
        public string ResumenRiesgo { get; set; } = "";
        public string Analisis { get; set; } = "";
        public string Recomendaciones { get; set; } = "";
        public string Alertas { get; set; } = "";
        public DateTime FechaGeneracion { get; set; }
    }
}