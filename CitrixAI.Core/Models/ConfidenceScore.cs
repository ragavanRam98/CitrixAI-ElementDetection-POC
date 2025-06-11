using System;
using System.Collections.Generic;
using System.Linq;
using CitrixAI.Core.Utilities;

namespace CitrixAI.Core.Models
{
    /// <summary>
    /// Represents a confidence score with detailed breakdown and calculation methodology.
    /// Immutable value object for confidence calculations.
    /// </summary>
    public sealed class ConfidenceScore : IEquatable<ConfidenceScore>
    {
        private readonly Dictionary<string, double> _componentScores;

        /// <summary>
        /// Initializes a new instance of the ConfidenceScore class.
        /// </summary>
        /// <param name="overallScore">The overall confidence score (0.0 to 1.0).</param>
        /// <param name="componentScores">Individual component scores that make up the overall score.</param>
        /// <param name="calculationMethod">The method used to calculate the overall score.</param>
        public ConfidenceScore(double overallScore, IDictionary<string, double> componentScores = null, string calculationMethod = "Weighted Average")
        {
            if (overallScore < 0.0 || overallScore > 1.0)
                throw new ArgumentOutOfRangeException(nameof(overallScore), "Overall score must be between 0.0 and 1.0.");

            if (string.IsNullOrWhiteSpace(calculationMethod))
                throw new ArgumentException("Calculation method cannot be null or empty.", nameof(calculationMethod));

            OverallScore = overallScore;
            _componentScores = new Dictionary<string, double>(componentScores ?? new Dictionary<string, double>());
            CalculationMethod = calculationMethod;
            Timestamp = DateTime.UtcNow;

            // Validate component scores
            foreach (var component in _componentScores)
            {
                if (component.Value < 0.0 || component.Value > 1.0)
                    throw new ArgumentOutOfRangeException(nameof(componentScores), $"Component score '{component.Key}' must be between 0.0 and 1.0.");
            }
        }

        /// <summary>
        /// Gets the overall confidence score (0.0 to 1.0).
        /// </summary>
        public double OverallScore { get; }

        /// <summary>
        /// Gets the individual component scores that contribute to the overall score.
        /// </summary>
        public IReadOnlyDictionary<string, double> ComponentScores =>
            new Dictionary<string, double>(_componentScores);

        /// <summary>
        /// Gets the method used to calculate the overall score.
        /// </summary>
        public string CalculationMethod { get; }

        /// <summary>
        /// Gets the timestamp when this confidence score was calculated.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets a value indicating whether this is considered a high confidence score.
        /// </summary>
        public bool IsHighConfidence => OverallScore >= 0.8;

        /// <summary>
        /// Gets a value indicating whether this is considered a medium confidence score.
        /// </summary>
        public bool IsMediumConfidence => OverallScore >= 0.5 && OverallScore < 0.8;

        /// <summary>
        /// Gets a value indicating whether this is considered a low confidence score.
        /// </summary>
        public bool IsLowConfidence => OverallScore < 0.5;

        /// <summary>
        /// Creates a confidence score using weighted average calculation.
        /// </summary>
        /// <param name="componentScores">Component scores with their weights.</param>
        /// <returns>A new ConfidenceScore instance.</returns>
        public static ConfidenceScore CreateWeightedAverage(IDictionary<string, (double score, double weight)> componentScores)
        {
            if (componentScores == null || !componentScores.Any())
                return new ConfidenceScore(0.0, null, "Weighted Average");

            double totalWeightedScore = 0.0;
            double totalWeight = 0.0;
            var scores = new Dictionary<string, double>();

            foreach (var component in componentScores)
            {
                if (component.Value.weight <= 0)
                    throw new ArgumentException($"Weight for component '{component.Key}' must be greater than 0.");

                totalWeightedScore += component.Value.score * component.Value.weight;
                totalWeight += component.Value.weight;
                scores[component.Key] = component.Value.score;
            }

            double overallScore = totalWeight > 0 ? totalWeightedScore / totalWeight : 0.0;
            return new ConfidenceScore(overallScore, scores, "Weighted Average");
        }

        /// <summary>
        /// Creates a confidence score using simple average calculation.
        /// </summary>
        /// <param name="componentScores">Component scores to average.</param>
        /// <returns>A new ConfidenceScore instance.</returns>
        public static ConfidenceScore CreateSimpleAverage(IDictionary<string, double> componentScores)
        {
            if (componentScores == null || !componentScores.Any())
                return new ConfidenceScore(0.0, null, "Simple Average");

            double average = componentScores.Values.Average();
            return new ConfidenceScore(average, componentScores, "Simple Average");
        }

        /// <summary>
        /// Creates a confidence score using minimum value calculation.
        /// </summary>
        /// <param name="componentScores">Component scores to evaluate.</param>
        /// <returns>A new ConfidenceScore instance.</returns>
        public static ConfidenceScore CreateMinimum(IDictionary<string, double> componentScores)
        {
            if (componentScores == null || !componentScores.Any())
                return new ConfidenceScore(0.0, null, "Minimum");

            double minimum = componentScores.Values.Min();
            return new ConfidenceScore(minimum, componentScores, "Minimum");
        }

        /// <summary>
        /// Creates a confidence score using maximum value calculation.
        /// </summary>
        /// <param name="componentScores">Component scores to evaluate.</param>
        /// <returns>A new ConfidenceScore instance.</returns>
        public static ConfidenceScore CreateMaximum(IDictionary<string, double> componentScores)
        {
            if (componentScores == null || !componentScores.Any())
                return new ConfidenceScore(0.0, null, "Maximum");

            double maximum = componentScores.Values.Max();
            return new ConfidenceScore(maximum, componentScores, "Maximum");
        }

        /// <summary>
        /// Combines this confidence score with another using weighted average.
        /// </summary>
        /// <param name="other">The other confidence score to combine with.</param>
        /// <param name="thisWeight">The weight for this confidence score.</param>
        /// <param name="otherWeight">The weight for the other confidence score.</param>
        /// <returns>A new combined ConfidenceScore.</returns>
        public ConfidenceScore CombineWith(ConfidenceScore other, double thisWeight = 1.0, double otherWeight = 1.0)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (thisWeight <= 0 || otherWeight <= 0)
                throw new ArgumentException("Weights must be greater than 0.");

            double combinedScore = (OverallScore * thisWeight + other.OverallScore * otherWeight) / (thisWeight + otherWeight);

            var combinedComponents = new Dictionary<string, double>(_componentScores);
            foreach (var component in other.ComponentScores)
            {
                if (combinedComponents.ContainsKey(component.Key))
                {
                    combinedComponents[component.Key] = (combinedComponents[component.Key] * thisWeight + component.Value * otherWeight) / (thisWeight + otherWeight);
                }
                else
                {
                    combinedComponents[component.Key] = component.Value;
                }
            }

            return new ConfidenceScore(combinedScore, combinedComponents, "Combined Weighted Average");
        }

        /// <summary>
        /// Determines equality based on overall score and calculation method.
        /// </summary>
        /// <param name="other">The other ConfidenceScore to compare with.</param>
        /// <returns>True if the confidence scores are equal.</returns>
        public bool Equals(ConfidenceScore other)
        {
            if (other == null) return false;
            return Math.Abs(OverallScore - other.OverallScore) < 0.001 &&
                   CalculationMethod == other.CalculationMethod;
        }

        /// <summary>
        /// Determines equality based on overall score and calculation method.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>True if the objects are equal.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ConfidenceScore);
        }

        /// <summary>
        /// Gets the hash code for this confidence score.
        /// </summary>
        /// <returns>Hash code based on overall score and calculation method.</returns>
        public override int GetHashCode()
        {
            return HashCodeHelper.Combine(OverallScore, CalculationMethod);
        }

        /// <summary>
        /// Returns a string representation of the confidence score.
        /// </summary>
        /// <returns>String representation of the confidence score.</returns>
        public override string ToString()
        {
            return $"ConfidenceScore[Overall={OverallScore:F3}, Method={CalculationMethod}, Components={ComponentScores.Count}]";
        }

        /// <summary>
        /// Gets a detailed breakdown of the confidence score.
        /// </summary>
        /// <returns>Detailed string representation including component scores.</returns>
        public string GetDetailedBreakdown()
        {
            var breakdown = new System.Text.StringBuilder();
            breakdown.AppendLine($"Overall Score: {OverallScore:F3} ({GetConfidenceLevel()})");
            breakdown.AppendLine($"Calculation Method: {CalculationMethod}");
            breakdown.AppendLine($"Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss} UTC");

            if (ComponentScores.Any())
            {
                breakdown.AppendLine("Component Scores:");
                foreach (var component in ComponentScores.OrderByDescending(c => c.Value))
                {
                    breakdown.AppendLine($"  {component.Key}: {component.Value:F3}");
                }
            }

            return breakdown.ToString();
        }

        /// <summary>
        /// Gets the confidence level as a string.
        /// </summary>
        /// <returns>Confidence level description.</returns>
        private string GetConfidenceLevel()
        {
            if (IsHighConfidence) return "High";
            if (IsMediumConfidence) return "Medium";
            return "Low";
        }

        /// <summary>
        /// Implicit conversion from double to ConfidenceScore.
        /// </summary>
        /// <param name="score">The score value to convert.</param>
        public static implicit operator ConfidenceScore(double score)
        {
            return new ConfidenceScore(score);
        }

        /// <summary>
        /// Implicit conversion from ConfidenceScore to double.
        /// </summary>
        /// <param name="confidenceScore">The ConfidenceScore to convert.</param>
        public static implicit operator double(ConfidenceScore confidenceScore)
        {
            return confidenceScore?.OverallScore ?? 0.0;
        }
    }
}