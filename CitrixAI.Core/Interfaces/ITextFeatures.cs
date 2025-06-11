using System.Collections.Generic;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Represents text-related features of an element.
    /// </summary>
    public interface ITextFeatures
    {
        /// <summary>
        /// Gets the detected text content.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets the estimated font size.
        /// </summary>
        float FontSize { get; }

        /// <summary>
        /// Gets the text confidence score from OCR (0.0 to 1.0).
        /// </summary>
        double TextConfidence { get; }

        /// <summary>
        /// Gets a value indicating whether the text appears to be bold.
        /// </summary>
        bool IsBold { get; }

        /// <summary>
        /// Gets a value indicating whether the text appears to be italic.
        /// </summary>
        bool IsItalic { get; }

        /// <summary>
        /// Gets the detected language of the text.
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Gets additional text properties.
        /// </summary>
        IDictionary<string, object> Properties { get; }
    }
}