using CitrixAI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CitrixAI.Core.Models
{
    /// <summary>
    /// Implementation of IDetectionResult containing element detection results.
    /// Immutable class following value object pattern.
    /// </summary>
    public sealed class DetectionResult : IDetectionResult
    {
        private readonly List<IElementInfo> _detectedElements;
        private readonly Dictionary<string, object> _metadata;
        private readonly List<string> _warnings;

        /// <summary>
        /// Initializes a new instance of the DetectionResult class.
        /// </summary>
        /// <param name="strategyId">The strategy that produced this result.</param>
        /// <param name="detectedElements">The collection of detected elements.</param>
        /// <param name="overallConfidence">The overall confidence score.</param>
        /// <param name="processingTime">The processing time taken.</param>
        /// <param name="metadata">Additional metadata.</param>
        /// <param name="warnings">Any warnings that occurred.</param>
        /// <param name="imageQuality">The quality score of the input image.</param>
        public DetectionResult(
            string strategyId,
            IEnumerable<IElementInfo> detectedElements,
            double overallConfidence,
            TimeSpan processingTime,
            IDictionary<string, object> metadata = null,
            IEnumerable<string> warnings = null,
            double imageQuality = 1.0)
        {
            if (string.IsNullOrWhiteSpace(strategyId))
                throw new ArgumentException("Strategy ID cannot be null or empty.", nameof(strategyId));

            if (overallConfidence < 0.0 || overallConfidence > 1.0)
                throw new ArgumentOutOfRangeException(nameof(overallConfidence), "Confidence must be between 0.0 and 1.0.");

            if (imageQuality < 0.0 || imageQuality > 1.0)
                throw new ArgumentOutOfRangeException(nameof(imageQuality), "Image quality must be between 0.0 and 1.0.");

            ResultId = Guid.NewGuid();
            StrategyId = strategyId;
            Timestamp = DateTime.UtcNow;
            _detectedElements = new List<IElementInfo>(detectedElements ?? Enumerable.Empty<IElementInfo>());
            OverallConfidence = overallConfidence;
            ProcessingTime = processingTime;
            _metadata = new Dictionary<string, object>(metadata ?? new Dictionary<string, object>());
            _warnings = new List<string>(warnings ?? Enumerable.Empty<string>());
            ImageQuality = imageQuality;
            IsSuccessful = _detectedElements.Count > 0 && overallConfidence > 0.0;
        }

        /// <inheritdoc />
        public Guid ResultId { get; }

        /// <inheritdoc />
        public string StrategyId { get; }

        /// <inheritdoc />
        public DateTime Timestamp { get; }

        /// <inheritdoc />
        public IReadOnlyList<IElementInfo> DetectedElements => _detectedElements.AsReadOnly();

        /// <inheritdoc />
        public double OverallConfidence { get; }

        /// <inheritdoc />
        public TimeSpan ProcessingTime { get; }

        /// <inheritdoc />
        public IDictionary<string, object> Metadata => new Dictionary<string, object>(_metadata);

        /// <inheritdoc />
        public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

        /// <inheritdoc />
        public bool IsSuccessful { get; }

        /// <inheritdoc />
        public double ImageQuality { get; }

        /// <summary>
        /// Creates a failed detection result.
        /// </summary>
        /// <param name="strategyId">The strategy that attempted the detection.</param>
        /// <param name="error">The error message.</param>
        /// <param name="processingTime">The processing time taken.</param>
        /// <returns>A failed detection result.</returns>
        public static DetectionResult CreateFailure(string strategyId, string error, TimeSpan processingTime)
        {
            return new DetectionResult(
                strategyId,
                Enumerable.Empty<IElementInfo>(),
                0.0,
                processingTime,
                new Dictionary<string, object> { { "Error", error } },
                new[] { error },
                0.0);
        }

        /// <summary>
        /// Creates an empty detection result (no elements found).
        /// </summary>
        /// <param name="strategyId">The strategy that performed the detection.</param>
        /// <param name="processingTime">The processing time taken.</param>
        /// <param name="imageQuality">The quality of the input image.</param>
        /// <returns>An empty detection result.</returns>
        public static DetectionResult CreateEmpty(string strategyId, TimeSpan processingTime, double imageQuality = 1.0)
        {
            return new DetectionResult(
                strategyId,
                Enumerable.Empty<IElementInfo>(),
                0.0,
                processingTime,
                imageQuality: imageQuality);
        }

        /// <summary>
        /// Returns a string representation of the detection result.
        /// </summary>
        /// <returns>String representation of the result.</returns>
        public override string ToString()
        {
            return $"DetectionResult[Strategy={StrategyId}, Elements={DetectedElements.Count}, Confidence={OverallConfidence:F2}, Time={ProcessingTime.TotalMilliseconds:F0}ms]";
        }
    }
}