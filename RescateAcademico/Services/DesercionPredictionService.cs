using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Services
{
    public class DesercionPredictionService
    {
        private readonly ApplicationDbContext _context;
        private MLContext? _mlContext;
        private ITransformer? _model;
        private PredictionEngine<DesercionInput, DesercionOutput>? _predictionEngine;
        private bool _isTrained = false;

        public DesercionPredictionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool IsTrained => _isTrained;

        public void EntrenarModelo()
        {
            _mlContext = new MLContext(seed: 42);

            // Load training data from the database
            var datos = _context.Alumnos
                .Select(a => new DesercionInput
                {
                    PromedioGlobal = (float)a.PromedioGlobal,
                    MateriasReprobadas = a.MateriasReprobadas ?? 0,
                    Ausencias = a.Ausencias ?? 0,
                    ParcialesBajos = a.ParcialesBajos ?? 0,
                    EtsPresentados = a.EtsPresentados ?? 0,
                    Recursamientos = a.Recursamientos ?? 0,
                    CargaAcademica = a.CargaAcademicaActual ?? 6,
                    Semestre = a.SemestreActual,
                    // Label: true if student is at high risk (Rojo) or has high probability heuristic
                    Abandono = (a.PromedioGlobal < 6.0m || (a.MateriasReprobadas ?? 0) >= 2 || (a.Ausencias ?? 0) > 6) ? true : false
                })
                .ToList();

            if (datos.Count < 10)
            {
                _isTrained = false;
                return;
            }

            var dataView = _mlContext.Data.LoadFromEnumerable(datos);

            var pipeline = _mlContext.Transforms.Concatenate("Features",
                    nameof(DesercionInput.PromedioGlobal),
                    nameof(DesercionInput.MateriasReprobadas),
                    nameof(DesercionInput.Ausencias),
                    nameof(DesercionInput.ParcialesBajos),
                    nameof(DesercionInput.EtsPresentados),
                    nameof(DesercionInput.Recursamientos),
                    nameof(DesercionInput.CargaAcademica),
                    nameof(DesercionInput.Semestre))
                .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                    labelColumnName: nameof(DesercionInput.Abandono),
                    featureColumnName: "Features",
                    numberOfLeaves: 10,
                    numberOfTrees: 50,
                    minimumExampleCountPerLeaf: 5));

            _model = pipeline.Fit(dataView);
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<DesercionInput, DesercionOutput>(_model);
            _isTrained = true;
        }

        public DesercionOutput Predecir(Alumno alumno)
        {
            if (!_isTrained || _predictionEngine == null)
            {
                // Fallback to heuristic if model not trained
                var prob = CalcularHeuristica(alumno);
                return new DesercionOutput
                {
                    Prediccion = prob > 0.5m,
                    Probability = (float)prob,
                    EsHeuristica = true
                };
            }

            var input = new DesercionInput
            {
                PromedioGlobal = (float)alumno.PromedioGlobal,
                MateriasReprobadas = alumno.MateriasReprobadas ?? 0,
                Ausencias = alumno.Ausencias ?? 0,
                ParcialesBajos = alumno.ParcialesBajos ?? 0,
                EtsPresentados = alumno.EtsPresentados ?? 0,
                Recursamientos = alumno.Recursamientos ?? 0,
                CargaAcademica = alumno.CargaAcademicaActual ?? 6,
                Semestre = alumno.SemestreActual
            };

            try
            {
                var result = _predictionEngine.Predict(input);
                result.EsHeuristica = false;
                return result;
            }
            catch
            {
                var prob = CalcularHeuristica(alumno);
                return new DesercionOutput
                {
                    Prediccion = prob > 0.5m,
                    Probability = (float)prob,
                    EsHeuristica = true
                };
            }
        }

        public async Task<List<PrediccionAlumnoViewModel>> PredecirTodosAsync()
        {
            if (!_isTrained)
                EntrenarModelo();

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
                    Metodo = pred.EsHeuristica ? "Heurística" : "ML.NET (FastTree)",
                    Color = pred.Probabilidad > 0.7m ? "danger" : pred.Probabilidad > 0.5m ? "warning" : pred.Probabilidad > 0.3m ? "info" : "success"
                });
            }

            return resultados.OrderByDescending(r => r.ProbabilidadDesercion).ToList();
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

    public class DesercionInput
    {
        public float PromedioGlobal { get; set; }
        public int MateriasReprobadas { get; set; }
        public int Ausencias { get; set; }
        public int ParcialesBajos { get; set; }
        public int EtsPresentados { get; set; }
        public int Recursamientos { get; set; }
        public int CargaAcademica { get; set; }
        public int Semestre { get; set; }
        [ColumnName("Label")] public bool Abandono { get; set; }
    }

    public class DesercionOutput
    {
        [ColumnName("PredictedLabel")]
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
}
