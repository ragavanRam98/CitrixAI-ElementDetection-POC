using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CitrixAI.Core.Interfaces;
using CitrixAI.Core.Models;

namespace CitrixAI.Detection.ContextAnalysis
{
    /// <summary>
    /// Advanced context-aware detection engine that analyzes spatial relationships
    /// and UI patterns to improve detection accuracy and reduce false positives
    /// </summary>
    public class ContextualDetector : IDisposable
    {
        private readonly UIPatternRecognizer _patternRecognizer;
        private readonly Dictionary<string, SpatialRelationship> _spatialRules;
        private readonly ContextualSettings _settings;
        private bool _disposed = false;

        public ContextualDetector()
        {
            _patternRecognizer = new UIPatternRecognizer();
            _spatialRules = InitializeSpatialRules();
            _settings = new ContextualSettings();
        }

        /// <summary>
        /// Analyzes detection results within their spatial context to improve accuracy
        /// </summary>
        public async Task<IList<IElementInfo>> AnalyzeContextAsync(
    IList<IElementInfo> elements,
    IDetectionContext context,
    Bitmap sourceImage)
        {
            try
            {
                Console.WriteLine($"=== CONTEXTUAL ANALYSIS START ===");
                Console.WriteLine($"Input elements: {elements?.Count ?? 0}");

                if (elements == null || !elements.Any())
                {
                    Console.WriteLine("No elements to analyze");
                    return elements ?? new List<IElementInfo>();
                }

                // Step 1: Identify UI patterns in the image
                Console.WriteLine("Step 1: Recognizing UI patterns...");
                var patterns = await _patternRecognizer.RecognizePatternsAsync(elements, sourceImage);
                Console.WriteLine($"Found {patterns.Count} UI patterns");

                // Step 2: Analyze spatial relationships between elements
                Console.WriteLine("Step 2: Analyzing spatial relationships...");
                var enhancedElements = AnalyzeSpatialRelationships(elements, patterns);
                Console.WriteLine($"Spatial analysis complete: {enhancedElements.Count} elements");

                // Step 3: Apply context-based confidence adjustments
                Console.WriteLine("Step 3: Applying confidence adjustments...");
                var adjustedElements = ApplyContextualConfidenceAdjustments(enhancedElements, patterns);
                Console.WriteLine($"Confidence adjustments complete: {adjustedElements.Count} elements");

                // Step 4: Filter out likely false positives based on context
                Console.WriteLine("Step 4: Filtering false positives...");
                var filteredElements = FilterContextualFalsePositives(adjustedElements, patterns);
                Console.WriteLine($"=== CONTEXTUAL ANALYSIS COMPLETE: {filteredElements.Count} elements ===");

                return filteredElements;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Context analysis failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Return original elements as fallback
                return elements ?? new List<IElementInfo>();
            }
        }

        /// <summary>
        /// Analyzes spatial relationships between detected elements
        /// </summary>
        private IList<IElementInfo> AnalyzeSpatialRelationships(
            IList<IElementInfo> elements,
            IList<UIPattern> patterns)
        {
            var enhancedElements = new List<IElementInfo>();

            foreach (var element in elements)
            {
                var enhancedElement = element;
                var confidence = element.Confidence;

                // Find nearby elements for relationship analysis
                var nearbyElements = FindNearbyElements(element, elements);

                // Apply spatial relationship rules
                foreach (var nearby in nearbyElements)
                {
                    var relationship = DetermineRelationship(element, nearby);
                    if (_spatialRules.ContainsKey(relationship.Type))
                    {
                        var rule = _spatialRules[relationship.Type];
                        confidence = ApplyRelationshipAdjustment(confidence, rule, relationship);
                    }
                }

                // Check if element fits expected patterns
                foreach (var pattern in patterns)
                {
                    if (IsElementInPattern(element, pattern))
                    {
                        confidence = ApplyPatternConfidenceBoost(confidence, pattern);
                    }
                }

                // Create enhanced element with adjusted confidence
                if (Math.Abs(confidence - element.Confidence) > 0.01)
                {
                    enhancedElement = CreateEnhancedElement(element, confidence, "Contextual analysis");
                }

                enhancedElements.Add(enhancedElement);
            }

            return enhancedElements;
        }

        /// <summary>
        /// Finds elements within proximity threshold of the target element
        /// </summary>
        private IList<IElementInfo> FindNearbyElements(IElementInfo target, IList<IElementInfo> allElements)
        {
            var nearbyElements = new List<IElementInfo>();
            var targetCenter = GetElementCenter(target.BoundingBox);

            foreach (var element in allElements)
            {
                if (element == target) continue;

                var elementCenter = GetElementCenter(element.BoundingBox);
                var distance = CalculateDistance(targetCenter, elementCenter);

                if (distance <= _settings.ProximityThreshold)
                {
                    nearbyElements.Add(element);
                }
            }

            return nearbyElements;
        }

        /// <summary>
        /// Determines the spatial relationship between two elements
        /// </summary>
        private SpatialRelationship DetermineRelationship(IElementInfo element1, IElementInfo element2)
        {
            var rect1 = element1.BoundingBox;
            var rect2 = element2.BoundingBox;

            var center1 = GetElementCenter(rect1);
            var center2 = GetElementCenter(rect2);

            var distance = CalculateDistance(center1, center2);
            var horizontalAlignment = Math.Abs(center1.Y - center2.Y) < _settings.AlignmentTolerance;
            var verticalAlignment = Math.Abs(center1.X - center2.X) < _settings.AlignmentTolerance;

            string relationshipType;

            if (horizontalAlignment && center2.X > center1.X)
                relationshipType = $"{element1.ElementType}-RightOf-{element2.ElementType}";
            else if (horizontalAlignment && center2.X < center1.X)
                relationshipType = $"{element1.ElementType}-LeftOf-{element2.ElementType}";
            else if (verticalAlignment && center2.Y > center1.Y)
                relationshipType = $"{element1.ElementType}-Below-{element2.ElementType}";
            else if (verticalAlignment && center2.Y < center1.Y)
                relationshipType = $"{element1.ElementType}-Above-{element2.ElementType}";
            else
                relationshipType = $"{element1.ElementType}-Near-{element2.ElementType}";

            return new SpatialRelationship
            {
                Type = relationshipType,
                Distance = distance,
                IsAligned = horizontalAlignment || verticalAlignment,
                Element1 = element1,
                Element2 = element2
            };
        }

        /// <summary>
        /// Applies confidence adjustments based on contextual analysis
        /// </summary>
        private IList<IElementInfo> ApplyContextualConfidenceAdjustments(
            IList<IElementInfo> elements,
            IList<UIPattern> patterns)
        {
            var adjustedElements = new List<IElementInfo>();

            foreach (var element in elements)
            {
                var confidence = element.Confidence;

                // Boost confidence for elements that fit common UI patterns
                if (IsInExpectedPattern(element, patterns))
                {
                    confidence = Math.Min(1.0, confidence * _settings.PatternBoostFactor);
                }

                // Reduce confidence for isolated elements in unexpected locations
                if (IsIsolatedElement(element, elements))
                {
                    confidence *= _settings.IsolationPenaltyFactor;
                }

                // Boost confidence for elements with strong spatial relationships
                if (HasStrongSpatialSupport(element, elements))
                {
                    confidence = Math.Min(1.0, confidence * _settings.SpatialSupportBoostFactor);
                }

                var adjustedElement = Math.Abs(confidence - element.Confidence) > 0.01
                    ? CreateEnhancedElement(element, confidence, "Contextual confidence adjustment")
                    : element;

                adjustedElements.Add(adjustedElement);
            }

            return adjustedElements;
        }

        /// <summary>
        /// Filters out elements likely to be false positives based on context
        /// </summary>
        private IList<IElementInfo> FilterContextualFalsePositives(
    IList<IElementInfo> elements,
    IList<UIPattern> patterns)
        {
            var filteredElements = new List<IElementInfo>();

            foreach (var element in elements)
            {
                // More aggressive filtering - higher minimum confidence
                if (element.Confidence < 0.4) // Increased from 0.3
                    continue;

                // Filter out unreasonably small elements
                if (element.BoundingBox.Width < 5 || element.BoundingBox.Height < 5)
                    continue;

                // Filter out unreasonably large elements (taking up >50% of any dimension)
                var boundingBox = element.BoundingBox;
                if (boundingBox.Width > 1000 || boundingBox.Height > 600) // Assuming max reasonable UI size
                    continue;

                // Skip elements that contradict expected patterns
                if (ContradictsKnownPatterns(element, patterns))
                    continue;

                // Skip elements with impossible spatial relationships
                if (HasImpossibleSpatialRelationships(element, elements))
                    continue;

                filteredElements.Add(element);
            }

            return filteredElements;
        }

        /// <summary>
        /// Initializes spatial relationship rules for different element types
        /// </summary>
        private Dictionary<string, SpatialRelationship> InitializeSpatialRules()
        {
            return new Dictionary<string, SpatialRelationship>
            {
                // Common UI patterns
                ["Label-LeftOf-TextBox"] = new SpatialRelationship { ConfidenceMultiplier = 1.3 },
                ["Label-Above-TextBox"] = new SpatialRelationship { ConfidenceMultiplier = 1.2 },
                ["Button-RightOf-TextBox"] = new SpatialRelationship { ConfidenceMultiplier = 1.2 },
                ["Button-Below-TextBox"] = new SpatialRelationship { ConfidenceMultiplier = 1.1 },
                ["Checkbox-LeftOf-Label"] = new SpatialRelationship { ConfidenceMultiplier = 1.4 },
                ["RadioButton-LeftOf-Label"] = new SpatialRelationship { ConfidenceMultiplier = 1.4 },

                // Form patterns
                ["TextBox-Above-TextBox"] = new SpatialRelationship { ConfidenceMultiplier = 1.1 },
                ["Button-RightOf-Button"] = new SpatialRelationship { ConfidenceMultiplier = 1.1 },

                // Dialog patterns
                ["Button-Below-Panel"] = new SpatialRelationship { ConfidenceMultiplier = 1.2 },
                ["TitleBar-Above-Panel"] = new SpatialRelationship { ConfidenceMultiplier = 1.3 }
            };
        }

        // Helper methods
        private Point GetElementCenter(Rectangle rect)
        {
            return new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        }

        private double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private double ApplyRelationshipAdjustment(double confidence, SpatialRelationship rule, SpatialRelationship actual)
        {
            return Math.Min(1.0, confidence * rule.ConfidenceMultiplier);
        }

        private double ApplyPatternConfidenceBoost(double confidence, UIPattern pattern)
        {
            return Math.Min(1.0, confidence * pattern.ConfidenceBoost);
        }

        private bool IsElementInPattern(IElementInfo element, UIPattern pattern)
        {
            return pattern.Elements.Any(e =>
                Math.Abs(e.BoundingBox.X - element.BoundingBox.X) < 10 &&
                Math.Abs(e.BoundingBox.Y - element.BoundingBox.Y) < 10);
        }

        private bool IsInExpectedPattern(IElementInfo element, IList<UIPattern> patterns)
        {
            return patterns.Any(p => IsElementInPattern(element, p) && p.Confidence > 0.7);
        }

        private bool IsIsolatedElement(IElementInfo element, IList<IElementInfo> allElements)
        {
            var nearbyElements = FindNearbyElements(element, allElements);
            return nearbyElements.Count < _settings.MinimumNeighbors;
        }

        private bool HasStrongSpatialSupport(IElementInfo element, IList<IElementInfo> allElements)
        {
            var nearbyElements = FindNearbyElements(element, allElements);
            var supportingRelationships = 0;

            foreach (var nearby in nearbyElements)
            {
                var relationship = DetermineRelationship(element, nearby);
                if (_spatialRules.ContainsKey(relationship.Type))
                {
                    supportingRelationships++;
                }
            }

            return supportingRelationships >= _settings.MinimumSpatialSupport;
        }

        private bool ContradictsKnownPatterns(IElementInfo element, IList<UIPattern> patterns)
        {
            // Implementation for pattern contradiction detection
            return false; // Simplified for now
        }

        private bool HasImpossibleSpatialRelationships(IElementInfo element, IList<IElementInfo> allElements)
        {
            // Implementation for impossible relationship detection
            return false; // Simplified for now
        }

        private IElementInfo CreateEnhancedElement(IElementInfo original, double newConfidence, string enhancement)
        {
            var properties = new Dictionary<string, object>(original.Properties ?? new Dictionary<string, object>())
            {
                ["EnhancementApplied"] = enhancement,
                ["OriginalConfidence"] = original.Confidence,
                ["ContextuallyAdjusted"] = true
            };

            return new ElementInfo(
                original.BoundingBox,
                original.ElementType,
                newConfidence,
                original.Text,
                properties
            );
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _patternRecognizer?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Configuration settings for contextual detection
    /// </summary>
    public class ContextualSettings
    {
        public double ProximityThreshold { get; set; } = 100.0; // pixels
        public double AlignmentTolerance { get; set; } = 15.0; // pixels
        public double PatternBoostFactor { get; set; } = 1.2;
        public double IsolationPenaltyFactor { get; set; } = 0.8;
        public double SpatialSupportBoostFactor { get; set; } = 1.15;
        public double MinimumContextualConfidence { get; set; } = 0.3;
        public int MinimumNeighbors { get; set; } = 1;
        public int MinimumSpatialSupport { get; set; } = 2;
    }

    /// <summary>
    /// Represents a spatial relationship between UI elements
    /// </summary>
    public class SpatialRelationship
    {
        public string Type { get; set; }
        public double Distance { get; set; }
        public bool IsAligned { get; set; }
        public double ConfidenceMultiplier { get; set; } = 1.0;
        public IElementInfo Element1 { get; set; }
        public IElementInfo Element2 { get; set; }
    }
}