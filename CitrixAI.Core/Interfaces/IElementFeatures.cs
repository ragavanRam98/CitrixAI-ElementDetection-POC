using System.Collections.Generic;
using System.Drawing;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Represents visual features of a detected element used for identification.
    /// </summary>
    public interface IElementFeatures
    {
        /// <summary>
        /// Gets the dominant colors in the element.
        /// </summary>
        IReadOnlyList<Color> DominantColors { get; }

        /// <summary>
        /// Gets the edge density score (0.0 to 1.0).
        /// </summary>
        double EdgeDensity { get; }

        /// <summary>
        /// Gets the texture complexity score (0.0 to 1.0).
        /// </summary>
        double TextureComplexity { get; }

        /// <summary>
        /// Gets the aspect ratio of the element.
        /// </summary>
        double AspectRatio { get; }

        /// <summary>
        /// Gets the contrast ratio within the element.
        /// </summary>
        double ContrastRatio { get; }

        /// <summary>
        /// Gets key visual features for matching.
        /// </summary>
        IDictionary<string, double> VisualFeatures { get; }

        /// <summary>
        /// Gets text-related features if the element contains text.
        /// </summary>
        ITextFeatures TextFeatures { get; }
    }
}