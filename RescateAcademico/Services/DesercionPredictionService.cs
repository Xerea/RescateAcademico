using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RescateAcademico.Services
{
    public class DesercionPredictionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DesercionPredictionService> _logger;
        private readonly HttpClient _httpClient;
        private readonly RiskEvaluationService _riskEvaluationService;

        public DesercionPredictionService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<DesercionPredictionService> logger,
            HttpClient httpClient,
            RiskEvaluationService riskEvaluationService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _riskEvaluationService = riskEvaluationService;
        }

        public bool IsAvailable => true; // Heuristics are always available

        public DesercionOutput Predecir(Alumno alumno)
        {
            var prob = _riskEvaluationService.CalcularProbabilidadDesercion(alumno);
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
            return PredecirAlumnos(alumnos);
        }

        public async Task<List<PrediccionAlumnoViewModel>> PredecirAsync(IEnumerable<string> matriculas)
        {
            var ids = matriculas.ToList();
            var alumnos = await _context.Alumnos
                .Include(a => a.Grupo)
                .Where(a => ids.Contains(a.Matricula))
                .ToListAsync();
            return PredecirAlumnos(alumnos);
        }

        private List<PrediccionAlumnoViewModel> PredecirAlumnos(IEnumerable<Alumno> alumnos)
        {
            var resultados = new List<PrediccionAlumnoViewModel>();

            foreach (var alumno in alumnos)
            {
                var pred = Predecir(alumno);
                resultados.Add(new PrediccionAlumnoViewModel
                {
                    Matricula = alumno.Matricula,
                    Nombre = $"{alumno.Nombre} {alumno.Apellidos}",
                    Carrera = alumno.Carrera,
                    Semestre = alumno.SemestreActual,
                    Grupo = alumno.Grupo?.Clave,
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
                    new { role = "system", content = "Eres un consejero academico experimentado del Instituto Politecnico Nacional. Devuelve exclusivamente JSON valido, sin markdown, sin bloques de codigo y sin texto adicional. Usa un tono institucional, claro, empatico y accionable en espanol mexicano." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.25,
                max_tokens = 700,
                response_format = new { type = "json_object" }
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
            sb.AppendLine("Devuelve exclusivamente un objeto JSON con esta forma exacta:");
            sb.AppendLine("{");
            sb.AppendLine("  \"resumenRiesgo\": \"Una oracion breve que explique el nivel de riesgo general.\",");
            sb.AppendLine("  \"analisis\": \"Dos o tres oraciones que expliquen por que existe ese riesgo. No uses markdown.\",");
            sb.AppendLine("  \"factoresClave\": [\"Factor medible 1\", \"Factor medible 2\", \"Factor medible 3\"],");
            sb.AppendLine("  \"recomendaciones\": [\"Accion concreta 1\", \"Accion concreta 2\", \"Accion concreta 3\"],");
            sb.AppendLine("  \"alertas\": [\"Alerta si aplica\"],");
            sb.AppendLine("  \"prioridad\": \"Baja|Media|Alta|Critica\",");
            sb.AppendLine("  \"intervencionSugerida\": \"Acercamiento|Tutoria|Canalizacion|Otro\",");
            sb.AppendLine("  \"fechaSeguimientoSugerida\": \"YYYY-MM-DD\",");
            sb.AppendLine("  \"mensajeSugerido\": \"Texto breve que el profesor puede usar como nota inicial\"");
            sb.AppendLine("}");
            sb.AppendLine("Reglas: no incluyas diagnosticos medicos, no inventes datos no proporcionados, no uses asteriscos ni etiquetas como RESUMEN_RIESGO.");
            return sb.ToString();
        }

        private static AnalisisIAResultado ParsearRespuestaIA(string content, Alumno alumno)
        {
            var resultado = new AnalisisIAResultado
            {
                Matricula = alumno.Matricula,
                FechaGeneracion = DateTime.Now
            };

            if (TryParseJson(content, resultado))
            {
                resultado.Normalize();
                return resultado;
            }

            var normalizedContent = NormalizeAiText(content);
            resultado.ResumenRiesgo = ExtractSection(normalizedContent, "RESUMEN_RIESGO", "ANALISIS");
            resultado.Analisis = ExtractSection(normalizedContent, "ANALISIS", "RECOMENDACIONES");
            resultado.Recomendaciones = ExtractSection(normalizedContent, "RECOMENDACIONES", "ALERTAS");
            resultado.Alertas = ExtractSection(normalizedContent, "ALERTAS", null);

            if (string.IsNullOrEmpty(resultado.ResumenRiesgo))
                resultado.ResumenRiesgo = "Análisis de riesgo generado por IA.";
            if (string.IsNullOrEmpty(resultado.Analisis))
                resultado.Analisis = normalizedContent;
            if (string.IsNullOrEmpty(resultado.Alertas))
                resultado.Alertas = "Ninguna";

            resultado.Normalize();
            return resultado;
        }

        private static bool TryParseJson(string content, AnalisisIAResultado resultado)
        {
            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                resultado.ResumenRiesgo = GetString(root, "resumenRiesgo");
                resultado.Analisis = GetString(root, "analisis");
                resultado.FactoresClave = GetStringList(root, "factoresClave");
                resultado.RecomendacionesLista = GetStringList(root, "recomendaciones");
                resultado.AlertasLista = GetStringList(root, "alertas");
                resultado.Prioridad = GetString(root, "prioridad");
                resultado.IntervencionSugerida = GetString(root, "intervencionSugerida");
                resultado.FechaSeguimientoSugerida = GetString(root, "fechaSeguimientoSugerida");
                resultado.MensajeSugerido = GetString(root, "mensajeSugerido");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetString(JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
                return NormalizeAiText(property.GetString() ?? "");
            return "";
        }

        private static List<string> GetStringList(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var property))
                return new List<string>();

            if (property.ValueKind == JsonValueKind.Array)
            {
                return property.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => NormalizeAiText(item.GetString() ?? ""))
                    .Where(item => !string.IsNullOrWhiteSpace(item) && !item.Equals("Ninguna", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (property.ValueKind == JsonValueKind.String)
                return SplitList(property.GetString() ?? "");

            return new List<string>();
        }

        private static string ExtractSection(string content, string startLabel, string? endLabel)
        {
            var startMatch = Regex.Match(content, $@"(?i)\b{Regex.Escape(startLabel)}\s*:");
            if (!startMatch.Success)
                return "";

            var startIndex = startMatch.Index + startMatch.Length;
            var endIndex = content.Length;
            if (!string.IsNullOrEmpty(endLabel))
            {
                var endMatch = Regex.Match(content[startIndex..], $@"(?i)\b{Regex.Escape(endLabel)}\s*:");
                if (endMatch.Success)
                    endIndex = startIndex + endMatch.Index;
            }

            return NormalizeAiText(content[startIndex..endIndex]);
        }

        private static string NormalizeAiText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            var cleaned = value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("**", "")
                .Replace("__", "")
                .Replace("`", "")
                .Trim();

            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            return cleaned.Trim();
        }

        private static List<string> SplitList(string value)
        {
            var cleaned = NormalizeAiText(value);
            if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Equals("Ninguna", StringComparison.OrdinalIgnoreCase))
                return new List<string>();

            return Regex.Split(cleaned, @"(?:^|\s)(?:\d+[\.\)]\s+|[-•]\s+)")
                .Select(NormalizeAiText)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToList();
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
        public int Semestre { get; set; }
        public string? Grupo { get; set; }
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
        public string Prioridad { get; set; } = "";
        public List<string> FactoresClave { get; set; } = new();
        public List<string> RecomendacionesLista { get; set; } = new();
        public List<string> AlertasLista { get; set; } = new();
        public string IntervencionSugerida { get; set; } = "";
        public string FechaSeguimientoSugerida { get; set; } = "";
        public string MensajeSugerido { get; set; } = "";
        public DateTime FechaGeneracion { get; set; }

        public void Normalize()
        {
            ResumenRiesgo = Clean(ResumenRiesgo);
            Analisis = Clean(Analisis);
            Recomendaciones = Clean(Recomendaciones);
            Alertas = Clean(Alertas);
            Prioridad = Clean(Prioridad);
            IntervencionSugerida = Clean(IntervencionSugerida);
            FechaSeguimientoSugerida = Clean(FechaSeguimientoSugerida);
            MensajeSugerido = Clean(MensajeSugerido);
            FactoresClave = FactoresClave.Select(Clean).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            RecomendacionesLista = RecomendacionesLista.Select(Clean).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            AlertasLista = AlertasLista.Select(Clean).Where(v => !string.IsNullOrWhiteSpace(v) && !v.Equals("Ninguna", StringComparison.OrdinalIgnoreCase)).ToList();

            if (!RecomendacionesLista.Any())
                RecomendacionesLista = SplitRecommendations(Recomendaciones);
            if (!AlertasLista.Any() && !string.IsNullOrWhiteSpace(Alertas) && !Alertas.Equals("Ninguna", StringComparison.OrdinalIgnoreCase))
                AlertasLista = SplitRecommendations(Alertas);
            if (string.IsNullOrWhiteSpace(Recomendaciones) && RecomendacionesLista.Any())
                Recomendaciones = string.Join("\n", RecomendacionesLista.Select((r, i) => $"{i + 1}. {r}"));
            if (string.IsNullOrWhiteSpace(Alertas))
                Alertas = AlertasLista.Any() ? string.Join("; ", AlertasLista) : "Ninguna";
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            var cleaned = value.Replace("**", "").Replace("__", "").Replace("`", "").Trim();
            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            return cleaned.Trim();
        }

        private static List<string> SplitRecommendations(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();
            return Regex.Split(value, @"(?:^|\s)(?:\d+[\.\)]\s+|[-•]\s+)")
                .Select(Clean)
                .Where(item => !string.IsNullOrWhiteSpace(item) && !item.Equals("Ninguna", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
