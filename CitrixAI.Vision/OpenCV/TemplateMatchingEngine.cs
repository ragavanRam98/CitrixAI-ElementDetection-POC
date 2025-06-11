using CitrixAI.Core.Interfaces;
using CitrixAI.Core.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CitrixAI.Vision.OpenCV
{
    /// <summary>
    /// Provides template matching capabilities using OpenCV.
    /// Implements multiple template matching methods with confidence scoring.
    /// </summary>
    public sealed class TemplateMatchingEngine : IDisposable
    {
        private readonly ImageProcessor _imageProcessor;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the TemplateMatchingEngine class.
        /// </summary>
        public TemplateMatchingEngine()
        {
            _imageProcessor = new ImageProcessor();
        }

        /// <summary>
        /// Finds template matches in the source image.
        /// </summary>
        /// <param name="sourceImage">The source image to search in.</param>
        /// <param name="templateImage">The template image to find.</param>
        /// <param name="threshold">Confidence threshold (0.0 to 1.0).</param>
        /// <param name="maxMatches">Maximum number of matches to return.</param>
        /// <returns>List of detected template matches.</returns>
        public List<IElementInfo> FindMatches(Bitmap sourceImage, Bitmap templateImage,
            double threshold = 0.8, int maxMatches = 10)
        {
            if (sourceImage == null)
                throw new ArgumentNullException(nameof(sourceImage));

            if (templateImage == null)
                throw new ArgumentNullException(nameof(templateImage));

            if (threshold < 0.0 || threshold > 1.0)
                throw new ArgumentOutOfRangeException(nameof(threshold));

            var matches = new List<IElementInfo>();

            using (var sourceMat = _imageProcessor.BitmapToMat(sourceImage))
            using (var templateMat = _imageProcessor.BitmapToMat(templateImage))
            {
                try
                {
                    // Try multiple template matching methods
                    var methods = new[]
                    {
                        TemplateMatchModes.CCoeffNormed,
                        TemplateMatchModes.CCorrNormed,
                        TemplateMatchModes.SqDiffNormed
                    };

                    var allMatches = new List<TemplateMatch>();

                    foreach (var method in methods)
                    {
                        var methodMatches = FindMatchesWithMethod(sourceMat, templateMat, method, threshold);
                        allMatches.AddRange(methodMatches);
                    }

                    // Remove duplicate matches (overlapping results from different methods)
                    var uniqueMatches = RemoveDuplicateMatches(allMatches, templateMat.Size());

                    // Convert to ElementInfo objects
                    foreach (var match in uniqueMatches.Take(maxMatches))
                    {
                        var elementInfo = CreateElementInfoFromMatch(match, templateMat.Size());
                        matches.Add(elementInfo);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Template matching failed: {ex.Message}", ex);
                }
            }

            return matches.OrderByDescending(m => m.Confidence).ToList();
        }

        /// <summary>
        /// Finds template matches using a specific OpenCV method.
        /// </summary>
        /// <param name="source">Source image Mat.</param>
        /// <param name="template">Template image Mat.</param>
        /// <param name="method">Template matching method.</param>
        /// <param name="threshold">Confidence threshold.</param>
        /// <returns>List of template matches.</returns>
        private List<TemplateMatch> FindMatchesWithMethod(Mat source, Mat template,
            TemplateMatchModes method, double threshold)
        {
            var matches = new List<TemplateMatch>();

            using (var result = new Mat())
            {
                Cv2.MatchTemplate(source, template, result, method);

                // Find local maxima in the result
                var locations = FindLocalMaxima(result, threshold, method);

                foreach (var location in locations)
                {
                    var confidence = CalculateConfidence(result.At<float>(location.Y, location.X), method);

                    matches.Add(new TemplateMatch
                    {
                        Location = location,
                        Confidence = confidence,
                        Method = method
                    });
                }
            }

            return matches;
        }

        /// <summary>
        /// Finds local maxima in the template matching result.
        /// </summary>
        /// <param name="result">Template matching result Mat.</param>
        /// <param name="threshold">Confidence threshold.</param>
        /// <param name="method">Template matching method used.</param>
        /// <returns>List of potential match locations.</returns>
        private List<OpenCvSharp.Point> FindLocalMaxima(Mat result, double threshold, TemplateMatchModes method)
        {
            var locations = new List<OpenCvSharp.Point>();

            try
            {
                // For SQDIFF methods, we need to find minima instead of maxima
                bool findMinima = method == TemplateMatchModes.SqDiff || method == TemplateMatchModes.SqDiffNormed;

                using (var mask = new Mat())
                {
                    if (findMinima)
                    {
                        Cv2.Threshold(result, mask, threshold, 1.0, ThresholdTypes.BinaryInv);
                    }
                    else
                    {
                        Cv2.Threshold(result, mask, threshold, 1.0, ThresholdTypes.Binary);
                    }

                    // Find contours of potential matches
                    Cv2.FindContours(mask, out var contours, out var hierarchy,
                        RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    foreach (var contour in contours)
                    {
                        var boundingRect = Cv2.BoundingRect(contour);
                        var centerPoint = new OpenCvSharp.Point(
                            boundingRect.X + boundingRect.Width / 2,
                            boundingRect.Y + boundingRect.Height / 2);

                        locations.Add(centerPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to find local maxima: {ex.Message}", ex);
            }

            return locations;
        }

        /// <summary>
        /// Calculates confidence score from template matching value.
        /// </summary>
        /// <param name="matchValue">Raw matching value from OpenCV.</param>
        /// <param name="method">Template matching method used.</param>
        /// <returns>Normalized confidence score (0.0 to 1.0).</returns>
        private double CalculateConfidence(float matchValue, TemplateMatchModes method)
        {
            switch (method)
            {
                case TemplateMatchModes.SqDiff:
                case TemplateMatchModes.SqDiffNormed:
                    // For squared difference, lower values are better
                    return Math.Max(0.0, 1.0 - Math.Abs(matchValue));

                case TemplateMatchModes.CCoeff:
                case TemplateMatchModes.CCoeffNormed:
                case TemplateMatchModes.CCorr:
                case TemplateMatchModes.CCorrNormed:
                    // For correlation methods, higher values are better
                    return Math.Max(0.0, Math.Min(1.0, matchValue));

                default:
                    return Math.Max(0.0, Math.Min(1.0, matchValue));
            }
        }

        /// <summary>
        /// Removes duplicate matches that are too close to each other.
        /// </summary>
        /// <param name="matches">List of all matches.</param>
        /// <param name="templateSize">Size of the template.</param>
        /// <returns>List of unique matches.</returns>
        private List<TemplateMatch> RemoveDuplicateMatches(List<TemplateMatch> matches, OpenCvSharp.Size templateSize)
        {
            if (matches.Count <= 1)
                return matches;

            var uniqueMatches = new List<TemplateMatch>();
            var sortedMatches = matches.OrderByDescending(m => m.Confidence).ToList();

            foreach (var match in sortedMatches)
            {
                bool isDuplicate = false;

                foreach (var uniqueMatch in uniqueMatches)
                {
                    var distance = Math.Sqrt(
                        Math.Pow(match.Location.X - uniqueMatch.Location.X, 2) +
                        Math.Pow(match.Location.Y - uniqueMatch.Location.Y, 2));

                    // Consider as duplicate if centers are within half template size
                    var duplicateThreshold = Math.Max(templateSize.Width, templateSize.Height) * 0.5;

                    if (distance < duplicateThreshold)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    uniqueMatches.Add(match);
                }
            }

            return uniqueMatches;
        }

        /// <summary>
        /// Creates an ElementInfo object from a template match.
        /// </summary>
        /// <param name="match">The template match.</param>
        /// <param name="templateSize">Size of the template.</param>
        /// <returns>ElementInfo representing the match.</returns>
        private IElementInfo CreateElementInfoFromMatch(TemplateMatch match, OpenCvSharp.Size templateSize)
        {
            var boundingBox = new Rectangle(
                match.Location.X - templateSize.Width / 2,
                match.Location.Y - templateSize.Height / 2,
                templateSize.Width,
                templateSize.Height);

            var properties = new Dictionary<string, object>
            {
                ["MatchMethod"] = match.Method.ToString(),
                ["MatchLocation"] = match.Location,
                ["TemplateSize"] = templateSize
            };

            return new ElementInfo(
                boundingBox,
                ElementType.Unknown, // Template matching doesn't determine element type
                match.Confidence,
                properties: properties);
        }

        /// <summary>
        /// Releases resources used by the TemplateMatchingEngine.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _imageProcessor?.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Represents a template match result.
        /// </summary>
        private class TemplateMatch
        {
            public OpenCvSharp.Point Location { get; set; }
            public double Confidence { get; set; }
            public TemplateMatchModes Method { get; set; }
        }
    }
}