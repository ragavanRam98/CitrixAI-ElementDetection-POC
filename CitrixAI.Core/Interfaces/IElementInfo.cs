using System;
using System.Collections.Generic;
using System.Drawing;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Represents information about a detected UI element.
    /// Contains position, type, properties, and confidence data.
    /// </summary>
    public interface IElementInfo
    {
        /// <summary>
        /// Gets the unique identifier for this element.
        /// </summary>
        Guid ElementId { get; }

        /// <summary>
        /// Gets the bounding rectangle of the element in screen coordinates.
        /// </summary>
        Rectangle BoundingBox { get; }

        /// <summary>
        /// Gets the center point of the element.
        /// </summary>
        Point CenterPoint { get; }

        /// <summary>
        /// Gets the type of the UI element (button, textbox, label, etc.).
        /// </summary>
        ElementType ElementType { get; }

        /// <summary>
        /// Gets the confidence score for this element detection (0.0 to 1.0).
        /// </summary>
        double Confidence { get; }

        /// <summary>
        /// Gets the text content of the element (if any).
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets additional properties of the element.
        /// </summary>
        IDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets the visual features used for detection.
        /// </summary>
        IElementFeatures Features { get; }

        /// <summary>
        /// Gets the classification confidence for the element type.
        /// </summary>
        double TypeConfidence { get; }

        /// <summary>
        /// Gets a value indicating whether the element is clickable.
        /// </summary>
        bool IsClickable { get; }

        /// <summary>
        /// Gets a value indicating whether the element contains text input.
        /// </summary>
        bool IsTextInput { get; }

        /// <summary>
        /// Gets the parent element (if this is a child element).
        /// </summary>
        IElementInfo Parent { get; }

        /// <summary>
        /// Gets child elements (if this is a container element).
        /// </summary>
        IReadOnlyList<IElementInfo> Children { get; }
    }
}