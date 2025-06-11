using CitrixAI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace CitrixAI.Core.Models
{
    /// <summary>
    /// Implementation of IDetectionContext providing context for element detection.
    /// Immutable class containing all necessary information for detection operations.
    /// </summary>
    public sealed class DetectionContext : IDetectionContext, IDisposable
    {
        private readonly Dictionary<string, object> _metadata;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the DetectionContext class.
        /// </summary>
        /// <param name="sourceImage">The image to search for elements in.</param>
        /// <param name="searchCriteria">The search criteria for element detection.</param>
        /// <param name="environmentInfo">Environment information.</param>
        /// <param name="regionOfInterest">Region of interest for detection.</param>
        /// <param name="metadata">Additional metadata.</param>
        /// <param name="timeout">Timeout for detection operations.</param>
        /// <param name="minimumConfidence">Minimum confidence threshold.</param>
        /// <param name="maxResults">Maximum number of elements to detect.</param>
        /// <param name="enableOCR">Whether to perform OCR on detected elements.</param>
        /// <param name="enableClassification">Whether to classify detected elements.</param>
        public DetectionContext(
            Bitmap sourceImage,
            IElementSearchCriteria searchCriteria,
            IEnvironmentInfo environmentInfo,
            Rectangle? regionOfInterest = null,
            IDictionary<string, object> metadata = null,
            TimeSpan? timeout = null,
            double minimumConfidence = 0.7,
            int maxResults = 100,
            bool enableOCR = true,
            bool enableClassification = true)
        {
            if (sourceImage == null)
                throw new ArgumentNullException(nameof(sourceImage));

            if (searchCriteria == null)
                throw new ArgumentNullException(nameof(searchCriteria));

            if (environmentInfo == null)
                throw new ArgumentNullException(nameof(environmentInfo));

            if (minimumConfidence < 0.0 || minimumConfidence > 1.0)
                throw new ArgumentOutOfRangeException(nameof(minimumConfidence), "Minimum confidence must be between 0.0 and 1.0.");

            if (maxResults <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxResults), "Max results must be greater than 0.");

            ContextId = Guid.NewGuid();
            SourceImage = sourceImage;
            SearchCriteria = searchCriteria;
            EnvironmentInfo = environmentInfo;
            RegionOfInterest = regionOfInterest;
            _metadata = new Dictionary<string, object>(metadata ?? new Dictionary<string, object>());
            Timeout = timeout ?? TimeSpan.FromSeconds(30);
            MinimumConfidence = minimumConfidence;
            MaxResults = maxResults;
            EnableOCR = enableOCR;
            EnableClassification = enableClassification;
        }

        /// <inheritdoc />
        public Guid ContextId { get; }

        /// <inheritdoc />
        public Bitmap SourceImage { get; }

        /// <inheritdoc />
        public IElementSearchCriteria SearchCriteria { get; }

        /// <inheritdoc />
        public IEnvironmentInfo EnvironmentInfo { get; }

        /// <inheritdoc />
        public Rectangle? RegionOfInterest { get; }

        /// <inheritdoc />
        public IDictionary<string, object> Metadata => new Dictionary<string, object>(_metadata);

        /// <inheritdoc />
        public TimeSpan Timeout { get; }

        /// <inheritdoc />
        public double MinimumConfidence { get; }

        /// <inheritdoc />
        public int MaxResults { get; }

        /// <inheritdoc />
        public bool EnableOCR { get; }

        /// <inheritdoc />
        public bool EnableClassification { get; }

        /// <summary>
        /// Creates a detection context for template matching.
        /// </summary>
        /// <param name="sourceImage">The source image to search in.</param>
        /// <param name="templateImage">The template image to find.</param>
        /// <param name="threshold">The confidence threshold.</param>
        /// <returns>A configured detection context.</returns>
        public static DetectionContext ForTemplateMatching(Bitmap sourceImage, Bitmap templateImage, double threshold = 0.8)
        {
            var searchCriteria = new ElementSearchCriteria();
            searchCriteria.TemplateImage = templateImage;
            searchCriteria.SetElementTypes(new[] { ElementType.Unknown });
            searchCriteria.MatchThreshold = threshold;

            var environmentInfo = new EnvironmentInfo
            {
                ScreenResolution = new Size(sourceImage.Width, sourceImage.Height),
                DpiX = 96,
                DpiY = 96,
                Platform = "Citrix"
            };

            return new DetectionContext(
                sourceImage,
                searchCriteria,
                environmentInfo,
                minimumConfidence: threshold,
                enableOCR: false,
                enableClassification: false);
        }

        /// <summary>
        /// Creates a detection context for AI-powered element detection.
        /// </summary>
        /// <param name="sourceImage">The source image to search in.</param>
        /// <param name="elementTypes">The types of elements to detect.</param>
        /// <param name="minimumConfidence">The minimum confidence threshold.</param>
        /// <returns>A configured detection context.</returns>
        public static DetectionContext ForAIDetection(Bitmap sourceImage, ElementType[] elementTypes = null, double minimumConfidence = 0.7)
        {
            var searchCriteria = new ElementSearchCriteria();
            searchCriteria.SetElementTypes(elementTypes ?? new[] { ElementType.Button, ElementType.TextBox, ElementType.Label });
            searchCriteria.MatchThreshold = minimumConfidence;

            var environmentInfo = new EnvironmentInfo
            {
                ScreenResolution = new Size(sourceImage.Width, sourceImage.Height),
                DpiX = 96,
                DpiY = 96,
                Platform = "Citrix"
            };

            return new DetectionContext(
                sourceImage,
                searchCriteria,
                environmentInfo,
                minimumConfidence: minimumConfidence,
                enableOCR: true,
                enableClassification: true);
        }

        /// <summary>
        /// Releases resources used by the DetectionContext.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                SourceImage?.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Returns a string representation of the detection context.
        /// </summary>
        /// <returns>String representation of the context.</returns>
        public override string ToString()
        {
            return $"DetectionContext[Id={ContextId}, Size={SourceImage.Width}x{SourceImage.Height}, MinConf={MinimumConfidence:F2}]";
        }
    }
}