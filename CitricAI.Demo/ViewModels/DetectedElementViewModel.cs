using CitrixAI.Core.Interfaces;
using System;
using System.Drawing;
using System.Windows.Media;

namespace CitrixAI.Demo.ViewModels
{
    /// <summary>
    /// View model for displaying detected elements as overlays on the image.
    /// </summary>
    public class DetectedElementViewModel
    {
        private static readonly System.Windows.Media.Color[] HighlightColors =
        {
            Colors.Red, Colors.Blue, Colors.Green, Colors.Orange, Colors.Purple, Colors.Yellow
        };

        /// <summary>
        /// Initializes a new instance of the DetectedElementViewModel class.
        /// </summary>
        /// <param name="elementInfo">The element information to wrap.</param>
        public DetectedElementViewModel(IElementInfo elementInfo)
        {
            ElementInfo = elementInfo ?? throw new ArgumentNullException(nameof(elementInfo));

            // Assign color based on element type
            var colorIndex = (int)elementInfo.ElementType % HighlightColors.Length;
            HighlightColor = new SolidColorBrush(HighlightColors[colorIndex]);
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
        /// Gets the bounding box.
        /// </summary>
        public Rectangle BoundingBox => ElementInfo.BoundingBox;

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
        /// Gets the highlight color for this element.
        /// </summary>
        public SolidColorBrush HighlightColor { get; }
    }
}