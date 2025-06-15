using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitrixAI.Core.ML.Models;
using CitrixAI.Core.ML.Interfaces;
using CitrixAI.Core.Interfaces;

namespace CitrixAI.Core.ML.Training
{
    /// <summary>
    /// Provides comprehensive model evaluation and performance assessment capabilities.
    /// Supports A/B testing, accuracy metrics, and detailed performance analysis.
    /// </summary>
    public class ModelEvaluator : IModelEvaluator, IDisposable
    {
        private bool _disposed = false;

        /// <summary>
        /// Evaluates model performance against a test dataset.
        /// Calculates comprehensive metrics including accuracy, precision, recall, and F1-score.
        /// </summary>
        public async Task<EvaluationMetrics> EvaluateAsync(string modelPath, IEnumerable<TestData> testData)
        {
            if (string.IsNullOrEmpty(modelPath))
                throw new ArgumentException("Model path cannot be null or empty", nameof(modelPath));

            if (testData == null || !testData.Any())
                throw new ArgumentException("Test data cannot be null or empty", nameof(testData));

            var metrics = new EvaluationMetrics
            {
                ModelPath = modelPath,
                EvaluationDate = DateTime.UtcNow,
                TestDataCount = testData.Count()
            };

            var elementTypeMetrics = new Dictionary<ElementType, ElementTypeMetrics>();
            var allPredictions = new List<PredictionResult>();

            foreach (var testItem in testData)
            {
                try
                {
                    var predictions = await EvaluateTestItem(testItem, modelPath);
                    allPredictions.AddRange(predictions);

                    foreach (var prediction in predictions)
                    {
                        if (!elementTypeMetrics.ContainsKey(prediction.ExpectedType))
                        {
                            elementTypeMetrics[prediction.ExpectedType] = new ElementTypeMetrics
                            {
                                ElementType = prediction.ExpectedType
                            };
                        }

                        var typeMetrics = elementTypeMetrics[prediction.ExpectedType];
                        UpdateElementTypeMetrics(typeMetrics, prediction);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to evaluate test item {testItem.ImagePath}: {ex.Message}");
                    metrics.FailedEvaluations++;
                }
            }

            metrics.OverallAccuracy = CalculateOverallAccuracy(allPredictions);
            metrics.ElementTypeMetrics = elementTypeMetrics.Values.ToList();
            metrics.ConfusionMatrix = await GenerateConfusionMatrixAsync(allPredictions);
            metrics.MacroAverageF1 = CalculateMacroAverageF1(elementTypeMetrics.Values);
            metrics.WeightedAverageF1 = CalculateWeightedAverageF1(elementTypeMetrics.Values);

            return metrics;
        }

        /// <summary>
        /// Generates a confusion matrix from prediction results.
        /// Provides detailed breakdown of classification performance.
        /// </summary>
        public async Task<ConfusionMatrix> GenerateConfusionMatrixAsync(ValidationResult validationResult)
        {
            if (validationResult?.Predictions == null)
                throw new ArgumentNullException(nameof(validationResult));

            return await GenerateConfusionMatrixAsync(validationResult.Predictions);
        }

        /// <summary>
        /// Compares two models using the same test dataset.
        /// Provides detailed performance comparison and recommendations.
        /// </summary>
        public async Task<ModelComparisonResult> CompareModelsAsync(string baseModelPath, string candidateModelPath, IEnumerable<TestData> testData)
        {
            if (string.IsNullOrEmpty(baseModelPath))
                throw new ArgumentException("Base model path cannot be null or empty", nameof(baseModelPath));

            if (string.IsNullOrEmpty(candidateModelPath))
                throw new ArgumentException("Candidate model path cannot be null or empty", nameof(candidateModelPath));

            var baselineMetrics = await EvaluateAsync(baseModelPath, testData);
            var candidateMetrics = await EvaluateAsync(candidateModelPath, testData);

            var comparison = new ModelComparisonResult
            {
                BaselineModel = baseModelPath,
                CandidateModel = candidateModelPath,
                BaselineMetrics = baselineMetrics,
                CandidateMetrics = candidateMetrics,
                ComparisonDate = DateTime.UtcNow
            };

            comparison.AccuracyImprovement = candidateMetrics.OverallAccuracy - baselineMetrics.OverallAccuracy;
            comparison.F1ScoreImprovement = candidateMetrics.MacroAverageF1 - baselineMetrics.MacroAverageF1;
            comparison.IsImprovement = comparison.AccuracyImprovement > 0 && comparison.F1ScoreImprovement > 0;
            comparison.Recommendation = GenerateRecommendation(comparison);

            return comparison;
        }

        /// <summary>
        /// Evaluates a single test item and returns prediction results.
        /// Handles element detection and classification assessment.
        /// </summary>
        private async Task<List<PredictionResult>> EvaluateTestItem(TestData testItem, string modelPath)
        {
            var predictions = new List<PredictionResult>();

            foreach (var expectedElement in testItem.ExpectedElements)
            {
                var prediction = new PredictionResult
                {
                    ImagePath = testItem.ImagePath,
                    ExpectedType = expectedElement.ElementType,
                    ExpectedBoundingBox = expectedElement.BoundingBox,
                    PredictedType = PredictElementType(expectedElement),
                    PredictedBoundingBox = PredictBoundingBox(expectedElement),
                    Confidence = CalculatePredictionConfidence(expectedElement)
                };

                prediction.IsCorrectClassification = prediction.ExpectedType == prediction.PredictedType;
                prediction.BoundingBoxIoU = CalculateIntersectionOverUnion(
                    prediction.ExpectedBoundingBox,
                    prediction.PredictedBoundingBox);
                prediction.IsCorrectDetection = prediction.BoundingBoxIoU >= 0.5;

                predictions.Add(prediction);
            }

            return await Task.FromResult(predictions);
        }

        /// <summary>
        /// Updates element type metrics based on prediction results.
        /// Calculates precision, recall, and F1-score for each element type.
        /// </summary>
        private void UpdateElementTypeMetrics(ElementTypeMetrics metrics, PredictionResult prediction)
        {
            metrics.TotalPredictions++;

            if (prediction.IsCorrectClassification)
            {
                metrics.TruePositives++;
            }
            else
            {
                metrics.FalsePositives++;
            }

            if (prediction.IsCorrectDetection)
            {
                metrics.CorrectDetections++;
            }

            metrics.AverageConfidence = ((metrics.AverageConfidence * (metrics.TotalPredictions - 1)) +
                                       prediction.Confidence) / metrics.TotalPredictions;

            metrics.Precision = metrics.TruePositives / (double)Math.Max(1, metrics.TruePositives + metrics.FalsePositives);
            metrics.Recall = metrics.TruePositives / (double)Math.Max(1, metrics.TotalPredictions);
            metrics.F1Score = 2 * (metrics.Precision * metrics.Recall) / Math.Max(0.001, metrics.Precision + metrics.Recall);
            metrics.DetectionRate = metrics.CorrectDetections / (double)Math.Max(1, metrics.TotalPredictions);
        }

        /// <summary>
        /// Calculates overall accuracy across all predictions.
        /// Considers both classification and detection accuracy.
        /// </summary>
        private double CalculateOverallAccuracy(List<PredictionResult> predictions)
        {
            if (!predictions.Any()) return 0.0;

            var correctClassifications = predictions.Count(p => p.IsCorrectClassification);
            var correctDetections = predictions.Count(p => p.IsCorrectDetection);

            var classificationAccuracy = correctClassifications / (double)predictions.Count;
            var detectionAccuracy = correctDetections / (double)predictions.Count;

            return (classificationAccuracy + detectionAccuracy) / 2.0;
        }

        /// <summary>
        /// Generates confusion matrix from prediction results.
        /// Shows classification performance across all element types.
        /// </summary>
        private async Task<ConfusionMatrix> GenerateConfusionMatrixAsync(IEnumerable<PredictionResult> predictions)
        {
            var matrix = new ConfusionMatrix();
            var elementTypes = Enum.GetValues(typeof(ElementType)).Cast<ElementType>().ToList();

            matrix.Labels = elementTypes.Select(t => t.ToString()).ToList();
            matrix.Matrix = new int[elementTypes.Count][];

            for (int i = 0; i < elementTypes.Count; i++)
            {
                matrix.Matrix[i] = new int[elementTypes.Count];
            }

            foreach (var prediction in predictions)
            {
                var expectedIndex = elementTypes.IndexOf(prediction.ExpectedType);
                var predictedIndex = elementTypes.IndexOf(prediction.PredictedType);

                if (expectedIndex >= 0 && predictedIndex >= 0)
                {
                    matrix.Matrix[expectedIndex][predictedIndex]++;
                }
            }

            return await Task.FromResult(matrix);
        }

        /// <summary>
        /// Calculates macro-averaged F1 score across all element types.
        /// Treats all element types equally regardless of frequency.
        /// </summary>
        private double CalculateMacroAverageF1(IEnumerable<ElementTypeMetrics> typeMetrics)
        {
            var f1Scores = typeMetrics.Select(m => m.F1Score).Where(f1 => !double.IsNaN(f1));
            return f1Scores.Any() ? f1Scores.Average() : 0.0;
        }

        /// <summary>
        /// Calculates weighted-averaged F1 score across all element types.
        /// Weights F1 scores by the frequency of each element type.
        /// </summary>
        private double CalculateWeightedAverageF1(IEnumerable<ElementTypeMetrics> typeMetrics)
        {
            var totalPredictions = typeMetrics.Sum(m => m.TotalPredictions);
            if (totalPredictions == 0) return 0.0;

            var weightedSum = typeMetrics.Sum(m => m.F1Score * m.TotalPredictions);
            return weightedSum / totalPredictions;
        }

        /// <summary>
        /// Generates recommendations based on model comparison results.
        /// Provides actionable insights for model selection and improvement.
        /// </summary>
        private string GenerateRecommendation(ModelComparisonResult comparison)
        {
            if (comparison.IsImprovement)
            {
                if (comparison.AccuracyImprovement > 0.05)
                {
                    return "Significant improvement detected. Recommend deploying the candidate model.";
                }
                else
                {
                    return "Moderate improvement detected. Consider additional validation before deployment.";
                }
            }
            else if (Math.Abs(comparison.AccuracyImprovement) < 0.01)
            {
                return "Performance is equivalent. Consider other factors like model size and inference speed.";
            }
            else
            {
                return "Performance regression detected. Do not deploy the candidate model.";
            }
        }

        /// <summary>
        /// Mock implementation for element type prediction.
        /// In production, this would interface with the actual ML model.
        /// </summary>
        private ElementType PredictElementType(ElementAnnotation expected)
        {
            return expected.ElementType;
        }

        /// <summary>
        /// Mock implementation for bounding box prediction.
        /// In production, this would interface with the actual ML model.
        /// </summary>
        private System.Drawing.Rectangle PredictBoundingBox(ElementAnnotation expected)
        {
            var variance = 5;
            var random = new Random();

            return new System.Drawing.Rectangle(
                expected.BoundingBox.X + random.Next(-variance, variance),
                expected.BoundingBox.Y + random.Next(-variance, variance),
                expected.BoundingBox.Width + random.Next(-variance, variance),
                expected.BoundingBox.Height + random.Next(-variance, variance)
            );
        }

        /// <summary>
        /// Mock implementation for prediction confidence calculation.
        /// In production, this would come from the actual ML model.
        /// </summary>
        private double CalculatePredictionConfidence(ElementAnnotation expected)
        {
            var random = new Random();
            return 0.7 + (random.NextDouble() * 0.3);
        }

        /// <summary>
        /// Calculates Intersection over Union for bounding box overlap assessment.
        /// Standard metric for object detection evaluation.
        /// </summary>
        private double CalculateIntersectionOverUnion(System.Drawing.Rectangle rect1, System.Drawing.Rectangle rect2)
        {
            var intersectionArea = System.Drawing.Rectangle.Intersect(rect1, rect2);
            if (intersectionArea.IsEmpty) return 0.0;

            var intersection = intersectionArea.Width * intersectionArea.Height;
            var union = (rect1.Width * rect1.Height) + (rect2.Width * rect2.Height) - intersection;

            return union > 0 ? intersection / (double)union : 0.0;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Comprehensive evaluation metrics for model performance assessment.
    /// Contains overall statistics and per-element-type detailed metrics.
    /// </summary>
    public class EvaluationMetrics
    {
        public string ModelPath { get; set; }
        public DateTime EvaluationDate { get; set; }
        public int TestDataCount { get; set; }
        public int FailedEvaluations { get; set; }
        public double OverallAccuracy { get; set; }
        public double MacroAverageF1 { get; set; }
        public double WeightedAverageF1 { get; set; }
        public List<ElementTypeMetrics> ElementTypeMetrics { get; set; }
        public ConfusionMatrix ConfusionMatrix { get; set; }

        public EvaluationMetrics()
        {
            ElementTypeMetrics = new List<ElementTypeMetrics>();
        }
    }

    /// <summary>
    /// Performance metrics for a specific element type.
    /// Provides detailed classification and detection statistics.
    /// </summary>
    public class ElementTypeMetrics
    {
        public ElementType ElementType { get; set; }
        public int TotalPredictions { get; set; }
        public int TruePositives { get; set; }
        public int FalsePositives { get; set; }
        public int CorrectDetections { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double F1Score { get; set; }
        public double DetectionRate { get; set; }
        public double AverageConfidence { get; set; }
    }

    /// <summary>
    /// Results of model comparison analysis.
    /// Provides detailed comparison metrics and deployment recommendations.
    /// </summary>
    public class ModelComparisonResult
    {
        public string BaselineModel { get; set; }
        public string CandidateModel { get; set; }
        public EvaluationMetrics BaselineMetrics { get; set; }
        public EvaluationMetrics CandidateMetrics { get; set; }
        public DateTime ComparisonDate { get; set; }
        public double AccuracyImprovement { get; set; }
        public double F1ScoreImprovement { get; set; }
        public bool IsImprovement { get; set; }
        public string Recommendation { get; set; }
    }

    /// <summary>
    /// Confusion matrix for detailed classification analysis.
    /// Shows the relationship between expected and predicted classifications.
    /// </summary>
    public class ConfusionMatrix
    {
        public List<string> Labels { get; set; }
        public int[][] Matrix { get; set; }

        public ConfusionMatrix()
        {
            Labels = new List<string>();
        }
    }

    /// <summary>
    /// Individual prediction result for evaluation analysis.
    /// Contains both expected and predicted values with confidence metrics.
    /// </summary>
    public class PredictionResult
    {
        public string ImagePath { get; set; }
        public ElementType ExpectedType { get; set; }
        public ElementType PredictedType { get; set; }
        public System.Drawing.Rectangle ExpectedBoundingBox { get; set; }
        public System.Drawing.Rectangle PredictedBoundingBox { get; set; }
        public double Confidence { get; set; }
        public bool IsCorrectClassification { get; set; }
        public bool IsCorrectDetection { get; set; }
        public double BoundingBoxIoU { get; set; }
    }

    /// <summary>
    /// Test data item for model evaluation.
    /// Contains image path and expected element annotations.
    /// </summary>
    public class TestData
    {
        public string ImagePath { get; set; }
        public List<ElementAnnotation> ExpectedElements { get; set; }

        public TestData()
        {
            ExpectedElements = new List<ElementAnnotation>();
        }
    }

    /// <summary>
    /// Validation result container for model evaluation.
    /// Aggregates prediction results for analysis.
    /// </summary>
    public class ValidationResult
    {
        public string ModelPath { get; set; }
        public DateTime ValidationDate { get; set; }
        public List<PredictionResult> Predictions { get; set; }
        public double OverallAccuracy { get; set; }
        public TimeSpan EvaluationDuration { get; set; }

        public ValidationResult()
        {
            Predictions = new List<PredictionResult>();
        }
    }
}