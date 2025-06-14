using CitrixAI.Core.Interfaces;
using CitrixAI.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CitrixAI.Core.Classification
{
    /// <summary>
    /// Basic element classifier using visual features and text analysis.
    /// Implements rule-based classification with confidence scoring.
    /// </summary>
    public sealed class BasicElementClassifier : IElementClassifier
    {
        private const double DEFAULT_CONFIDENCE_THRESHOLD = 0.6;
        private readonly double _confidenceThreshold;
        private readonly List<ClassificationRule> _rules;

        /// <summary>
        /// Initializes a new instance of the BasicElementClassifier class.
        /// </summary>
        /// <param name="confidenceThreshold">Minimum confidence threshold for classification.</param>
        public BasicElementClassifier(double confidenceThreshold = DEFAULT_CONFIDENCE_THRESHOLD)
        {
            if (confidenceThreshold < 0.0 || confidenceThreshold > 1.0)
                throw new ArgumentOutOfRangeException(nameof(confidenceThreshold));

            _confidenceThreshold = confidenceThreshold;
            Name = "Basic Element Classifier";
            _rules = InitializeClassificationRules();
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public double ConfidenceThreshold => _confidenceThreshold;

        /// <inheritdoc />
        public async Task<IClassificationResult> ClassifyAsync(IElementInfo element, IDetectionContext context)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            try
            {
                return await Task.Run(() => PerformClassification(element, context));
            }
            catch (Exception ex)
            {
                return ClassificationResult.CreateFailure(
                    Name,
                    $"Classification failed: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public bool CanClassify(IElementInfo element)
        {
            return element != null &&
                   element.BoundingBox.Width > 0 &&
                   element.BoundingBox.Height > 0;
        }

        /// <inheritdoc />
        public bool IsConfigured()
        {
            return _rules.Count > 0;
        }

        /// <summary>
        /// Performs the actual classification logic.
        /// </summary>
        /// <param name="element">Element to classify.</param>
        /// <param name="context">Detection context.</param>
        /// <returns>Classification result.</returns>
        private IClassificationResult PerformClassification(IElementInfo element, IDetectionContext context)
        {
            var analysisResults = new List<(ElementType type, double confidence, string reason)>();

            // Analyze visual features
            var visualFeatures = AnalyzeVisualFeatures(element.BoundingBox, context.SourceImage);

            // Analyze text features
            var textFeatures = AnalyzeTextFeatures(element.Text);

            // Apply classification rules
            foreach (var rule in _rules)
            {
                var confidence = EvaluateRule(rule, visualFeatures, textFeatures, element);
                if (confidence >= _confidenceThreshold)
                {
                    analysisResults.Add((rule.ElementType, confidence, rule.Description));
                }
            }

            // Determine best classification
            if (analysisResults.Count == 0)
            {
                return new ClassificationResult(
                    ElementType.Unknown,
                    0.0,
                    Name,
                    new Dictionary<string, object> { ["Reason"] = "No rules matched with sufficient confidence" },
                    new List<(ElementType, double)>());
            }

            // Sort by confidence and get best result
            var sortedResults = analysisResults.OrderByDescending(r => r.confidence).ToList();
            var bestResult = sortedResults.First();

            // Create alternatives list
            var alternatives = sortedResults.Skip(1)
                .Take(3) // Top 3 alternatives
                .Select(r => (r.type, r.confidence))
                .ToList();

            var properties = new Dictionary<string, object>
            {
                ["VisualFeatures"] = visualFeatures,
                ["TextFeatures"] = textFeatures,
                ["PrimaryReason"] = bestResult.reason,
                ["AlternativeCount"] = alternatives.Count
            };

            return new ClassificationResult(
                bestResult.type,
                bestResult.confidence,
                Name,
                properties,
                alternatives);
        }

        /// <summary>
        /// Analyzes visual features of the element.
        /// </summary>
        /// <param name="bounds">Element bounding box.</param>
        /// <param name="sourceImage">Source image containing the element.</param>
        /// <returns>Visual features analysis.</returns>
        private VisualFeatures AnalyzeVisualFeatures(Rectangle bounds, Bitmap sourceImage)
        {
            var features = new VisualFeatures();

            try
            {
                // Calculate aspect ratio
                features.AspectRatio = (double)bounds.Width / bounds.Height;

                // Calculate size classification
                var area = bounds.Width * bounds.Height;
                features.SizeCategory = ClassifySize(area);

                // Extract element region for detailed analysis
                if (IsValidRegion(bounds, sourceImage))
                {
                    using (var elementImage = ExtractElementRegion(sourceImage, bounds))
                    {
                        features.EdgeDensity = CalculateEdgeDensity(elementImage);
                        features.ColorVariance = CalculateColorVariance(elementImage);
                        features.TextPresence = DetectTextPresence(elementImage);
                        features.BorderPresence = DetectBorderPresence(elementImage);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with partial features
                features.AnalysisError = ex.Message;
            }

            return features;
        }

        /// <summary>
        /// Analyzes text features of the element.
        /// </summary>
        /// <param name="text">Text content of the element.</param>
        /// <returns>Text features analysis.</returns>
        private TextFeatures AnalyzeTextFeatures(string text)
        {
            var features = new TextFeatures
            {
                HasText = !string.IsNullOrWhiteSpace(text),
                TextLength = text?.Length ?? 0,
                WordCount = string.IsNullOrWhiteSpace(text) ? 0 : text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length
            };

            if (features.HasText)
            {
                features.IsUpperCase = text.All(c => !char.IsLetter(c) || char.IsUpper(c));
                features.HasNumbers = text.Any(char.IsDigit);
                features.HasSpecialChars = text.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

                // Common UI text patterns
                features.IsButtonText = IsButtonText(text);
                features.IsLabelText = IsLabelText(text);
                features.IsInputPlaceholder = IsInputPlaceholder(text);
            }

            return features;
        }

        /// <summary>
        /// Evaluates a classification rule against the features.
        /// </summary>
        /// <param name="rule">Rule to evaluate.</param>
        /// <param name="visualFeatures">Visual features.</param>
        /// <param name="textFeatures">Text features.</param>
        /// <param name="element">Original element.</param>
        /// <returns>Confidence score for this rule.</returns>
        private double EvaluateRule(ClassificationRule rule, VisualFeatures visualFeatures,
            TextFeatures textFeatures, IElementInfo element)
        {
            double confidence = 0.0;
            int matchedCriteria = 0;
            int totalCriteria = 0;

            // Evaluate aspect ratio criteria
            if (rule.MinAspectRatio.HasValue || rule.MaxAspectRatio.HasValue)
            {
                totalCriteria++;
                if ((!rule.MinAspectRatio.HasValue || visualFeatures.AspectRatio >= rule.MinAspectRatio.Value) &&
                    (!rule.MaxAspectRatio.HasValue || visualFeatures.AspectRatio <= rule.MaxAspectRatio.Value))
                {
                    matchedCriteria++;
                    confidence += 0.3; // Weight for aspect ratio match
                }
            }

            // Evaluate size criteria
            if (rule.MinArea.HasValue || rule.MaxArea.HasValue)
            {
                totalCriteria++;
                var area = element.BoundingBox.Width * element.BoundingBox.Height;
                if ((!rule.MinArea.HasValue || area >= rule.MinArea.Value) &&
                    (!rule.MaxArea.HasValue || area <= rule.MaxArea.Value))
                {
                    matchedCriteria++;
                    confidence += 0.2; // Weight for size match
                }
            }

            // Evaluate text patterns
            if (rule.TextPatterns.Count > 0)
            {
                totalCriteria++;
                foreach (var pattern in rule.TextPatterns)
                {
                    if (Regex.IsMatch(element.Text ?? string.Empty, pattern, RegexOptions.IgnoreCase))
                    {
                        matchedCriteria++;
                        confidence += 0.4; // Weight for text pattern match
                        break; // Only count first match
                    }
                }
            }

            // Evaluate visual characteristics
            if (rule.RequiresText.HasValue)
            {
                totalCriteria++;
                if (textFeatures.HasText == rule.RequiresText.Value)
                {
                    matchedCriteria++;
                    confidence += 0.1;
                }
            }

            // Calculate final confidence
            if (totalCriteria == 0)
                return 0.0;

            // Base confidence from criteria matching
            var baseConfidence = (double)matchedCriteria / totalCriteria;

            // Apply rule-specific boost
            confidence = Math.Min(1.0, confidence + (baseConfidence * 0.3));

            return confidence;
        }

        /// <summary>
        /// Initializes the default classification rules.
        /// </summary>
        /// <returns>List of classification rules.</returns>
        private List<ClassificationRule> InitializeClassificationRules()
        {
            return new List<ClassificationRule>
            {
                // Button rules
                new ClassificationRule
                {
                    ElementType = ElementType.Button,
                    Description = "Standard button (wide, clickable text)",
                    MinAspectRatio = 1.5,
                    MaxAspectRatio = 6.0,
                    MinArea = 1000,
                    MaxArea = 15000,
                    TextPatterns = new List<string> { @"^(OK|Cancel|Submit|Save|Delete|Edit|Add|Remove|Browse|Search|Go|Send|Apply|Reset|Clear|Yes|No|Close|Exit|Login|Logout|Sign In|Sign Up|Continue|Next|Previous|Back|Finish|Start|Stop|Pause|Play|Upload|Download)$" },
                    RequiresText = true
                },

                // TextBox rules
                new ClassificationRule
                {
                    ElementType = ElementType.TextBox,
                    Description = "Text input field (rectangular, often empty or placeholder)",
                    MinAspectRatio = 2.0,
                    MaxAspectRatio = 15.0,
                    MinArea = 800,
                    MaxArea = 20000,
                    TextPatterns = new List<string> { @"(Enter|Type|Input|Search|Filter|Username|Password|Email|Name|Address|Phone|Date)", @"^\s*$" }, // Empty or placeholder text
                    RequiresText = false
                },

                // Label rules
                new ClassificationRule
                {
                    ElementType = ElementType.Label,
                    Description = "Text label (descriptive text, often followed by colon)",
                    MinAspectRatio = 1.0,
                    MaxAspectRatio = 10.0,
                    MinArea = 400,
                    MaxArea = 8000,
                    TextPatterns = new List<string> { @"^[A-Za-z\s]+:?\s*$", @"(Name|Label|Title|Description|Status|Type|Category|Version|Date|Time|Size|Count|Total|Amount)" },
                    RequiresText = true
                },

                // Dropdown rules
                new ClassificationRule
                {
                    ElementType = ElementType.Dropdown,
                    Description = "Dropdown selector (medium size, selection text)",
                    MinAspectRatio = 3.0,
                    MaxAspectRatio = 12.0,
                    MinArea = 1200,
                    MaxArea = 12000,
                    TextPatterns = new List<string> { @"(Select|Choose|Pick|All|None|Option|Item)", @"--\s*(Select|Choose)\s*--" },
                    RequiresText = false
                },

                // Checkbox rules
                new ClassificationRule
                {
                    ElementType = ElementType.Checkbox,
                    Description = "Checkbox control (square, small)",
                    MinAspectRatio = 0.8,
                    MaxAspectRatio = 1.5,
                    MinArea = 100,
                    MaxArea = 800,
                    TextPatterns = new List<string> { @"(Enable|Disable|Check|Uncheck|Allow|Remember|Agree|Accept|Subscribe)" },
                    RequiresText = false
                }
            };
        }

        #region Helper Methods

        private bool IsValidRegion(Rectangle bounds, Bitmap image)
        {
            return bounds.X >= 0 && bounds.Y >= 0 &&
                   bounds.Right <= image.Width && bounds.Bottom <= image.Height &&
                   bounds.Width > 0 && bounds.Height > 0;
        }

        private Bitmap ExtractElementRegion(Bitmap source, Rectangle bounds)
        {
            var region = new Bitmap(bounds.Width, bounds.Height);
            using (var graphics = Graphics.FromImage(region))
            {
                graphics.DrawImage(source, 0, 0, bounds, GraphicsUnit.Pixel);
            }
            return region;
        }

        private SizeCategory ClassifySize(int area)
        {
            if (area < 1000) return SizeCategory.Small;
            if (area < 5000) return SizeCategory.Medium;
            if (area < 15000) return SizeCategory.Large;
            return SizeCategory.ExtraLarge;
        }

        private double CalculateEdgeDensity(Bitmap image)
        {
            // Simplified edge detection using color variance
            int edgePixels = 0;
            int totalPixels = image.Width * image.Height;

            for (int y = 1; y < image.Height - 1; y++)
            {
                for (int x = 1; x < image.Width - 1; x++)
                {
                    var center = image.GetPixel(x, y);
                    var right = image.GetPixel(x + 1, y);
                    var bottom = image.GetPixel(x, y + 1);

                    var edgeStrength = Math.Abs(center.GetBrightness() - right.GetBrightness()) +
                                     Math.Abs(center.GetBrightness() - bottom.GetBrightness());

                    if (edgeStrength > 0.1) // Threshold for edge detection
                        edgePixels++;
                }
            }

            return (double)edgePixels / totalPixels;
        }

        private double CalculateColorVariance(Bitmap image)
        {
            var colors = new List<Color>();
            var sampleRate = Math.Max(1, Math.Max(image.Width, image.Height) / 20); // Sample every N pixels

            for (int y = 0; y < image.Height; y += sampleRate)
            {
                for (int x = 0; x < image.Width; x += sampleRate)
                {
                    colors.Add(image.GetPixel(x, y));
                }
            }

            if (colors.Count == 0) return 0.0;

            var avgR = colors.Average(c => c.R);
            var avgG = colors.Average(c => c.G);
            var avgB = colors.Average(c => c.B);

            var variance = colors.Average(c =>
                Math.Pow(c.R - avgR, 2) + Math.Pow(c.G - avgG, 2) + Math.Pow(c.B - avgB, 2));

            return Math.Min(1.0, variance / (255 * 255 * 3)); // Normalize to 0-1
        }

        private bool DetectTextPresence(Bitmap image)
        {
            // Simple text detection based on horizontal edge patterns
            int horizontalEdges = 0;
            int totalChecks = 0;

            for (int y = 1; y < image.Height - 1; y++)
            {
                for (int x = 0; x < image.Width - 5; x += 3) // Sample every 3 pixels
                {
                    totalChecks++;
                    var leftBrightness = image.GetPixel(x, y).GetBrightness();
                    var rightBrightness = image.GetPixel(x + 3, y).GetBrightness();

                    if (Math.Abs(leftBrightness - rightBrightness) > 0.2)
                        horizontalEdges++;
                }
            }

            return totalChecks > 0 && (double)horizontalEdges / totalChecks > 0.1;
        }

        private bool DetectBorderPresence(Bitmap image)
        {
            // Check edges for consistent color (border detection)
            var edgeColors = new List<Color>();

            // Sample top and bottom edges
            for (int x = 0; x < image.Width; x += Math.Max(1, image.Width / 10))
            {
                edgeColors.Add(image.GetPixel(x, 0));
                edgeColors.Add(image.GetPixel(x, image.Height - 1));
            }

            // Sample left and right edges
            for (int y = 0; y < image.Height; y += Math.Max(1, image.Height / 10))
            {
                edgeColors.Add(image.GetPixel(0, y));
                edgeColors.Add(image.GetPixel(image.Width - 1, y));
            }

            if (edgeColors.Count == 0) return false;

            // Check for color consistency (indicating a border)
            var avgR = edgeColors.Average(c => c.R);
            var avgG = edgeColors.Average(c => c.G);
            var avgB = edgeColors.Average(c => c.B);

            var variance = edgeColors.Average(c =>
                Math.Pow(c.R - avgR, 2) + Math.Pow(c.G - avgG, 2) + Math.Pow(c.B - avgB, 2));

            return variance < 1000; // Low variance indicates consistent border color
        }

        private bool IsButtonText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var buttonPatterns = new[]
            {
                @"^(OK|Cancel|Submit|Save|Delete|Edit|Add|Remove|Browse|Search|Go|Send|Apply|Reset|Clear|Yes|No|Close|Exit)$",
                @"^(Login|Logout|Sign In|Sign Up|Continue|Next|Previous|Back|Finish|Start|Stop|Pause|Play)$",
                @"^(Upload|Download|Print|Export|Import|Copy|Cut|Paste|Undo|Redo|Refresh|Reload)$"
            };

            return buttonPatterns.Any(pattern => Regex.IsMatch(text.Trim(), pattern, RegexOptions.IgnoreCase));
        }

        private bool IsLabelText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            // Labels often end with colon or are descriptive
            return text.Trim().EndsWith(":") ||
                   Regex.IsMatch(text.Trim(), @"^[A-Za-z\s]+(Name|Label|Title|Description|Status|Type|Category|Version|Date|Time|Size|Count|Total|Amount).*$", RegexOptions.IgnoreCase);
        }

        private bool IsInputPlaceholder(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var placeholderPatterns = new[]
            {
                @"^(Enter|Type|Input|Search|Filter).*",
                @".*placeholder.*",
                @"^(Username|Password|Email|Name|Address|Phone|Date).*"
            };

            return placeholderPatterns.Any(pattern => Regex.IsMatch(text.Trim(), pattern, RegexOptions.IgnoreCase));
        }

        #endregion
    }

    /// <summary>
    /// Represents visual features of an element.
    /// </summary>
    public class VisualFeatures
    {
        public double AspectRatio { get; set; }
        public SizeCategory SizeCategory { get; set; }
        public double EdgeDensity { get; set; }
        public double ColorVariance { get; set; }
        public bool TextPresence { get; set; }
        public bool BorderPresence { get; set; }
        public string AnalysisError { get; set; }
    }

    /// <summary>
    /// Represents text features of an element.
    /// </summary>
    public class TextFeatures
    {
        public bool HasText { get; set; }
        public int TextLength { get; set; }
        public int WordCount { get; set; }
        public bool IsUpperCase { get; set; }
        public bool HasNumbers { get; set; }
        public bool HasSpecialChars { get; set; }
        public bool IsButtonText { get; set; }
        public bool IsLabelText { get; set; }
        public bool IsInputPlaceholder { get; set; }
    }

    /// <summary>
    /// Represents a classification rule for element types.
    /// </summary>
    public class ClassificationRule
    {
        public ElementType ElementType { get; set; }
        public string Description { get; set; }
        public double? MinAspectRatio { get; set; }
        public double? MaxAspectRatio { get; set; }
        public int? MinArea { get; set; }
        public int? MaxArea { get; set; }
        public List<string> TextPatterns { get; set; } = new List<string>();
        public bool? RequiresText { get; set; }
    }

    /// <summary>
    /// Size categories for UI elements.
    /// </summary>
    public enum SizeCategory
    {
        Small,
        Medium,
        Large,
        ExtraLarge
    }

    /// <summary>
    /// Implementation of IClassificationResult.
    /// </summary>
    public class ClassificationResult : IClassificationResult
    {
        public ClassificationResult(ElementType classifiedType, double confidence, string classifierName,
            IDictionary<string, object> properties = null, IEnumerable<(ElementType, double)> alternatives = null)
        {
            ResultId = Guid.NewGuid();
            ClassifiedType = classifiedType;
            Confidence = confidence;
            ClassifierName = classifierName;
            Timestamp = DateTime.UtcNow;
            Properties = new Dictionary<string, object>(properties ?? new Dictionary<string, object>());
            Alternatives = alternatives?.ToList() ?? new List<(ElementType, double)>();
            IsSuccessful = confidence > 0.0;
            Notes = new List<string>();
        }

        public Guid ResultId { get; }
        public ElementType ClassifiedType { get; }
        public double Confidence { get; }
        public string ClassifierName { get; }
        public DateTime Timestamp { get; }
        public IDictionary<string, object> Properties { get; }
        public IReadOnlyList<(ElementType type, double confidence)> Alternatives { get; }
        public bool IsSuccessful { get; }
        public IReadOnlyList<string> Notes { get; }

        public static ClassificationResult CreateFailure(string classifierName, string error)
        {
            return new ClassificationResult(ElementType.Unknown, 0.0, classifierName,
                new Dictionary<string, object> { ["Error"] = error });
        }
    }
}