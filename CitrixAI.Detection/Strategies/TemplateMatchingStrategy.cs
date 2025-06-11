using CitrixAI.Core.Interfaces;
using CitrixAI.Core.Models;
using CitrixAI.Vision.OpenCV;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace CitrixAI.Detection.Strategies
{
    /// <summary>
    /// Template matching detection strategy using OpenCV.
    /// Implements IDetectionStrategy for backward compatibility with SikuliX approach.
    /// </summary>
    public sealed class TemplateMatchingStrategy : IDetectionStrategy, IDisposable
    {
        private readonly TemplateMatchingEngine _templateEngine;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the TemplateMatchingStrategy class.
        /// </summary>
        public TemplateMatchingStrategy()
        {
            _templateEngine = new TemplateMatchingEngine();
            StrategyId = "Template_Matching";
            Name = "Template Matching Strategy";
            Priority = 60; // Medium priority
        }

        /// <inheritdoc />
        public string StrategyId { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public int Priority { get; }

        /// <inheritdoc />
        public bool CanHandle(IDetectionContext context)
        {
            if (context?.SearchCriteria?.TemplateImage != null)
                return true;

            // Can handle any context as a fallback strategy
            return context?.SourceImage != null;
        }

        /// <inheritdoc />
        public async Task<IDetectionResult> DetectAsync(IDetectionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // For Day 1, create a mock template or use a simple pattern
                var templateImage = context.SearchCriteria.TemplateImage ?? CreateMockTemplate();

                var matches = _templateEngine.FindMatches(
                    context.SourceImage,
                    templateImage,
                    context.MinimumConfidence,
                    context.MaxResults);

                stopwatch.Stop();

                var metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["Strategy"] = Name,
                    ["TemplateSize"] = templateImage != null ? $"{templateImage.Width}x{templateImage.Height}" : "Mock",
                    ["SearchThreshold"] = context.MinimumConfidence
                };

                return new DetectionResult(
                    StrategyId,
                    matches,
                    matches.Any() ? matches.Average(m => m.Confidence) : 0.0,
                    stopwatch.Elapsed,
                    metadata);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return DetectionResult.CreateFailure(StrategyId, ex.Message, stopwatch.Elapsed);
            }
        }

        /// <inheritdoc />
        public TimeSpan GetEstimatedProcessingTime(Size imageSize)
        {
            // Rough estimation based on image size
            var pixels = imageSize.Width * imageSize.Height;
            var milliseconds = pixels / 10000.0; // Rough estimate
            return TimeSpan.FromMilliseconds(Math.Max(100, Math.Min(5000, milliseconds)));
        }

        /// <inheritdoc />
        public bool IsConfigured()
        {
            return _templateEngine != null;
        }

        /// <summary>
        /// Creates a mock template for testing purposes.
        /// </summary>
        /// <returns>Mock template bitmap.</returns>
        private Bitmap CreateMockTemplate()
        {
            // Create a simple 50x25 button-like template for testing
            var template = new Bitmap(50, 25);
            using (var g = Graphics.FromImage(template))
            {
                g.Clear(Color.LightGray);
                g.DrawRectangle(Pens.DarkGray, 0, 0, 49, 24);
                g.DrawString("OK", SystemFonts.DefaultFont, Brushes.Black, 15, 5);
            }
            return template;
        }

        /// <summary>
        /// Releases resources used by the TemplateMatchingStrategy.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _templateEngine?.Dispose();
                _disposed = true;
            }
        }
    }
}