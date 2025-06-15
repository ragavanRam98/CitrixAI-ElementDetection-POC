using System.Collections.Generic;
using System.Threading.Tasks;
using CitrixAI.Core.ML.Models;
using CitrixAI.Core.ML.Training;

namespace CitrixAI.Core.ML.Interfaces
{
    /// <summary>
    /// Contract for model evaluation and performance assessment.
    /// Provides standardized interface for model validation and comparison.
    /// </summary>
    public interface IModelEvaluator
    {
        /// <summary>
        /// Evaluates model performance against a test dataset.
        /// Returns comprehensive metrics including accuracy, precision, recall, and F1-score.
        /// </summary>
        /// <param name="modelPath">Path to the model file to evaluate</param>
        /// <param name="testData">Test dataset for evaluation</param>
        /// <returns>Comprehensive evaluation metrics</returns>
        Task<EvaluationMetrics> EvaluateAsync(string modelPath, IEnumerable<TestData> testData);

        /// <summary>
        /// Generates confusion matrix from validation results.
        /// Provides detailed breakdown of classification performance.
        /// </summary>
        /// <param name="validationResult">Validation result containing predictions</param>
        /// <returns>Confusion matrix showing classification patterns</returns>
        Task<ConfusionMatrix> GenerateConfusionMatrixAsync(ValidationResult validationResult);
    }
}