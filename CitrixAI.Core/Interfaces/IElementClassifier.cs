using System.Threading.Tasks;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for element classification services.
    /// Determines the type and properties of detected UI elements.
    /// </summary>
    public interface IElementClassifier
    {
        /// <summary>
        /// Gets the name of this classifier.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the confidence threshold for this classifier (0.0 to 1.0).
        /// </summary>
        double ConfidenceThreshold { get; }

        /// <summary>
        /// Classifies a detected element to determine its type and properties.
        /// </summary>
        /// <param name="element">The element to classify.</param>
        /// <param name="context">The detection context.</param>
        /// <returns>Classification result with type and confidence.</returns>
        Task<IClassificationResult> ClassifyAsync(IElementInfo element, IDetectionContext context);

        /// <summary>
        /// Determines if this classifier can handle the given element.
        /// </summary>
        /// <param name="element">The element to check.</param>
        /// <returns>True if this classifier can process the element, false otherwise.</returns>
        bool CanClassify(IElementInfo element);

        /// <summary>
        /// Validates the classifier configuration and dependencies.
        /// </summary>
        /// <returns>True if the classifier is properly configured, false otherwise.</returns>
        bool IsConfigured();
    }
}