using System.Collections.Generic;
using System.Drawing;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Defines search criteria for element detection operations.
    /// </summary>
    public interface IElementSearchCriteria
    {
        /// <summary>
        /// Gets the types of elements to search for.
        /// </summary>
        IReadOnlyList<ElementType> ElementTypes { get; }

        /// <summary>
        /// Gets the template image to match (for template-based detection).
        /// </summary>
        Bitmap TemplateImage { get; }

        /// <summary>
        /// Gets the text to search for within elements.
        /// </summary>
        string SearchText { get; }

        /// <summary>
        /// Gets the minimum match threshold (0.0 to 1.0).
        /// </summary>
        double MatchThreshold { get; }

        /// <summary>
        /// Gets additional search parameters.
        /// </summary>
        IDictionary<string, object> Parameters { get; }

        /// <summary>
        /// Gets a value indicating whether the search is case-sensitive.
        /// </summary>
        bool CaseSensitive { get; }

        /// <summary>
        /// Gets a value indicating whether to use fuzzy text matching.
        /// </summary>
        bool UseFuzzyMatching { get; }
    }
}