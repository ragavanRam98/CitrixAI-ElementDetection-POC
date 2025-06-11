using CitrixAI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CitrixAI.Core.Models
{
    /// <summary>
    /// Implementation of IElementInfo representing a detected UI element.
    /// Immutable class following value object pattern.
    /// </summary>
    public sealed class ElementInfo : IElementInfo
    {
        private readonly Dictionary<string, object> _properties;
        private readonly List<IElementInfo> _children;

        /// <summary>
        /// Initializes a new instance of the ElementInfo class.
        /// </summary>
        /// <param name="boundingBox">The bounding rectangle of the element.</param>
        /// <param name="elementType">The type of the UI element.</param>
        /// <param name="confidence">The confidence score for detection.</param>
        /// <param name="text">The text content of the element.</param>
        /// <param name="properties">Additional properties of the element.</param>
        /// <param name="features">Visual features used for detection.</param>
        /// <param name="typeConfidence">Classification confidence for the element type.</param>
        /// <param name="parent">The parent element.</param>
        /// <param name="children">Child elements.</param>
        public ElementInfo(
            Rectangle boundingBox,
            ElementType elementType,
            double confidence,
            string text = null,
            IDictionary<string, object> properties = null,
            IElementFeatures features = null,
            double typeConfidence = 1.0,
            IElementInfo parent = null,
            IEnumerable<IElementInfo> children = null)
        {
            if (confidence < 0.0 || confidence > 1.0)
                throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0.0 and 1.0.");

            if (typeConfidence < 0.0 || typeConfidence > 1.0)
                throw new ArgumentOutOfRangeException(nameof(typeConfidence), "Type confidence must be between 0.0 and 1.0.");

            ElementId = Guid.NewGuid();
            BoundingBox = boundingBox;
            CenterPoint = new Point(
                boundingBox.X + boundingBox.Width / 2,
                boundingBox.Y + boundingBox.Height / 2);
            ElementType = elementType;
            Confidence = confidence;
            Text = text ?? string.Empty;
            _properties = new Dictionary<string, object>(properties ?? new Dictionary<string, object>());
            Features = features;
            TypeConfidence = typeConfidence;
            Parent = parent;
            _children = new List<IElementInfo>(children ?? Enumerable.Empty<IElementInfo>());

            // Determine element capabilities based on type
            IsClickable = DetermineClickability(elementType);
            IsTextInput = DetermineTextInputCapability(elementType);
        }

        /// <inheritdoc />
        public Guid ElementId { get; }

        /// <inheritdoc />
        public Rectangle BoundingBox { get; }

        /// <inheritdoc />
        public Point CenterPoint { get; }

        /// <inheritdoc />
        public ElementType ElementType { get; }

        /// <inheritdoc />
        public double Confidence { get; }

        /// <inheritdoc />
        public string Text { get; }

        /// <inheritdoc />
        public IDictionary<string, object> Properties => new Dictionary<string, object>(_properties);

        /// <inheritdoc />
        public IElementFeatures Features { get; }

        /// <inheritdoc />
        public double TypeConfidence { get; }

        /// <inheritdoc />
        public bool IsClickable { get; }

        /// <inheritdoc />
        public bool IsTextInput { get; }

        /// <inheritdoc />
        public IElementInfo Parent { get; }

        /// <inheritdoc />
        public IReadOnlyList<IElementInfo> Children => _children.AsReadOnly();

        /// <summary>
        /// Determines if an element type is clickable.
        /// </summary>
        /// <param name="elementType">The element type to check.</param>
        /// <returns>True if the element type is typically clickable.</returns>
        private static bool DetermineClickability(ElementType elementType)
        {
            return elementType == ElementType.Button ||
                   elementType == ElementType.Checkbox ||
                   elementType == ElementType.RadioButton ||
                   elementType == ElementType.Dropdown ||
                   elementType == ElementType.Menu ||
                   elementType == ElementType.Tab ||
                   elementType == ElementType.Link;
        }

        /// <summary>
        /// Determines if an element type accepts text input.
        /// </summary>
        /// <param name="elementType">The element type to check.</param>
        /// <returns>True if the element type typically accepts text input.</returns>
        private static bool DetermineTextInputCapability(ElementType elementType)
        {
            return elementType == ElementType.TextBox;
        }

        /// <summary>
        /// Creates a copy of this element with updated properties.
        /// </summary>
        /// <param name="newProperties">Properties to update or add.</param>
        /// <returns>A new ElementInfo instance with updated properties.</returns>
        public ElementInfo WithUpdatedProperties(IDictionary<string, object> newProperties)
        {
            var updatedProperties = new Dictionary<string, object>(_properties);
            foreach (var kvp in newProperties ?? new Dictionary<string, object>())
            {
                updatedProperties[kvp.Key] = kvp.Value;
            }

            return new ElementInfo(
                BoundingBox,
                ElementType,
                Confidence,
                Text,
                updatedProperties,
                Features,
                TypeConfidence,
                Parent,
                Children);
        }

        /// <summary>
        /// Returns a string representation of the element.
        /// </summary>
        /// <returns>String representation of the element.</returns>
        public override string ToString()
        {
            return $"ElementInfo[Type={ElementType}, Bounds={BoundingBox}, Confidence={Confidence:F2}, Text='{Text}']";
        }

        /// <summary>
        /// Determines equality based on element properties.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>True if objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is ElementInfo other)
            {
                return ElementId == other.ElementId;
            }
            return false;
        }

        /// <summary>
        /// Gets the hash code for this element.
        /// </summary>
        /// <returns>Hash code based on element ID.</returns>
        public override int GetHashCode()
        {
            return ElementId.GetHashCode();
        }
    }
}