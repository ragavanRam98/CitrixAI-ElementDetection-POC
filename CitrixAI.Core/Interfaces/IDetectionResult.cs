using System;
using System.Collections.Generic;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Represents the result of an element detection operation.
    /// Contains detected elements, confidence scores, and metadata.
    /// </summary>
    public interface IDetectionResult
    {
        /// <summary>
        /// Gets the unique identifier for this detection result.
        /// </summary>
        Guid ResultId { get; }

        /// <summary>
        /// Gets the strategy that produced this result.
        /// </summary>
        string StrategyId { get; }

        /// <summary>
        /// Gets the timestamp when the detection was performed.
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Gets the collection of detected elements.
        /// </summary>
        IReadOnlyList<IElementInfo> DetectedElements { get; }

        /// <summary>
        /// Gets the overall confidence score for this detection result (0.0 to 1.0).
        /// </summary>
        double OverallConfidence { get; }

        /// <summary>
        /// Gets the processing time taken for this detection.
        /// </summary>
        TimeSpan ProcessingTime { get; }

        /// <summary>
        /// Gets additional metadata about the detection process.
        /// </summary>
        IDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Gets any errors or warnings that occurred during detection.
        /// </summary>
        IReadOnlyList<string> Warnings { get; }

        /// <summary>
        /// Gets a value indicating whether the detection was successful.
        /// </summary>
        bool IsSuccessful { get; }

        /// <summary>
        /// Gets the quality score of the input image (0.0 to 1.0).
        /// </summary>
        double ImageQuality { get; }
    }
}