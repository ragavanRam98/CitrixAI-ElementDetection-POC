using CitrixAI.Core.Interfaces;
using System;

namespace CitrixAI.Demo.ViewModels
{
    /// <summary>
    /// View model for displaying detection results in the UI.
    /// </summary>
    public class DetectionResultViewModel
    {
        /// <summary>
        /// Initializes a new instance of the DetectionResultViewModel class.
        /// </summary>
        /// <param name="elementInfo">The element information to wrap.</param>
        public DetectionResultViewModel(IElementInfo elementInfo)
        {
            ElementInfo = elementInfo ?? throw new ArgumentNullException(nameof(elementInfo));
        }

        /// <summary>
        /// Gets the underlying element information.
        /// </summary>
        public IElementInfo ElementInfo { get; }

        /// <summary>
        /// Gets the element ID.
        /// </summary>
        public Guid ElementId => ElementInfo.ElementId;

        /// <summary>
        /// Gets the element type.
        /// </summary>
        public string ElementType => ElementInfo.ElementType.ToString();

        /// <summary>
        /// Gets the confidence score.
        /// </summary>
        public double Confidence => ElementInfo.Confidence;

        /// <summary>
        /// Gets the text content.
        /// </summary>
        public string Text => ElementInfo.Text;

        /// <summary>
        /// Gets the location as a formatted string.
        /// </summary>
        public string LocationString => $"{ElementInfo.BoundingBox.X},{ElementInfo.BoundingBox.Y}";
    }
}