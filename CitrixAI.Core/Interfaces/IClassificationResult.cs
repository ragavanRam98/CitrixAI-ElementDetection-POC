using System;
using System.Collections.Generic;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Represents the result of an element classification operation.
    /// Contains the classified element type and confidence information.
    /// </summary>
    public interface IClassificationResult
    {
        /// <summary>
        /// Gets the unique identifier for this classification result.
        /// </summary>
        Guid ResultId { get; }

        /// <summary>
        /// Gets the classified element type.
        /// </summary>
        ElementType ClassifiedType { get; }

        /// <summary>
        /// Gets the confidence score for the classification (0.0 to 1.0).
        /// </summary>
        double Confidence { get; }

        /// <summary>
        /// Gets the classifier that produced this result.
        /// </summary>
        string ClassifierName { get; }

        /// <summary>
        /// Gets the timestamp when the classification was performed.
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Gets additional properties discovered during classification.
        /// </summary>
        IDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets alternative classification results with their confidence scores.
        /// </summary>
        IReadOnlyList<(ElementType type, double confidence)> Alternatives { get; }

        /// <summary>
        /// Gets a value indicating whether the classification was successful.
        /// </summary>
        bool IsSuccessful { get; }

        /// <summary>
        /// Gets any warnings or notes from the classification process.
        /// </summary>
        IReadOnlyList<string> Notes { get; }
    }
}