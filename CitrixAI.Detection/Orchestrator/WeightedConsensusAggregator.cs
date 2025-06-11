using CitrixAI.Core.Interfaces;
using CitrixAI.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace CitrixAI.Detection.Orchestrator
{
    /// <summary>
    /// Aggregates detection results using weighted consensus algorithms.
    /// Implements IResultAggregator with sophisticated conflict resolution.
    /// </summary>
    public sealed class WeightedConsensusAggregator : IResultAggregator
    {
        private readonly Dictionary<string, double> _strategyWeights;
        private readonly double _overlapThreshold;
        private readonly double _minimumConsensus;

        /// <summary>
        /// Initializes a new instance of the WeightedConsensusAggregator class.
        /// </summary>
        /// <param name="strategyWeights">Weights for different strategies.</param>
        /// <param name="overlapThreshold">Threshold for considering elements as overlapping (0.0 to 1.0).</param>
        /// <param name="minimumConsensus">Minimum consensus required for accepting results (0.0 to 1.0).</param>
        public WeightedConsensusAggregator(
            IDictionary<string, double> strategyWeights = null,
            double overlapThreshold = 0.5,
            double minimumConsensus = 0.6)
        {
            if (overlapThreshold < 0.0 || overlapThreshold > 1.0)
                throw new ArgumentOutOfRangeException(nameof(overlapThreshold));

            if (minimumConsensus < 0.0 || minimumConsensus > 1.0)
                throw new ArgumentOutOfRangeException(nameof(minimumConsensus));

            _strategyWeights = new Dictionary<string, double>(strategyWeights ?? GetDefaultWeights());
            _overlapThreshold = overlapThreshold;
            _minimumConsensus = minimumConsensus;
            Name = "Weighted Consensus Aggregator";
            MinimumResults = 1;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public int MinimumResults { get; }

        /// <inheritdoc />
        public async Task<IDetectionResult> AggregateAsync(IEnumerable<IDetectionResult> results, IDetectionContext context)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var resultList = results.Where(r => r.IsSuccessful).ToList();
            if (resultList.Count < MinimumResults)
            {
                return DetectionResult.CreateFailure(
                    "AggregationFailed",
                    $"Insufficient successful results for aggregation. Required: {MinimumResults}, Available: {resultList.Count}",
                    TimeSpan.Zero);
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Collect all detected elements from all strategies
                var allElements = new List<(IElementInfo element, string strategyId)>();
                foreach (var result in resultList)
                {
                    foreach (var element in result.DetectedElements)
                    {
                        allElements.Add((element, result.StrategyId));
                    }
                }

                // Group overlapping elements
                var elementGroups = GroupOverlappingElements(allElements);

                // Resolve conflicts and create consensus elements
                var consensusElements = new List<IElementInfo>();
                var aggregationWarnings = new List<string>();

                foreach (var group in elementGroups)
                {
                    var consensusElement = await ResolveElementGroup(group, aggregationWarnings);
                    if (consensusElement != null)
                    {
                        consensusElements.Add(consensusElement);
                    }
                }

                // Calculate overall confidence
                var overallConfidence = CalculateOverallConfidence(resultList, consensusElements.Count);

                // Calculate total processing time
                var totalProcessingTime = TimeSpan.FromMilliseconds(
                    resultList.Sum(r => r.ProcessingTime.TotalMilliseconds));

                // Create aggregated metadata
                var aggregatedMetadata = CreateAggregatedMetadata(resultList, elementGroups.Count);

                // Combine warnings from all results
                var allWarnings = resultList.SelectMany(r => r.Warnings)
                    .Concat(aggregationWarnings)
                    .Distinct()
                    .ToList();

                stopwatch.Stop();

                return new DetectionResult(
                    "WeightedConsensusAggregator",
                    consensusElements,
                    overallConfidence,
                    totalProcessingTime,
                    aggregatedMetadata,
                    allWarnings,
                    resultList.Average(r => r.ImageQuality));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return DetectionResult.CreateFailure(
                    "WeightedConsensusAggregator",
                    $"Error during result aggregation: {ex.Message}",
                    stopwatch.Elapsed);
            }
        }

        /// <inheritdoc />
        public IElementInfo ResolveConflict(IEnumerable<IElementInfo> conflictingElements)
        {
            var elements = conflictingElements?.ToList();
            if (elements == null || elements.Count == 0)
                return null;

            if (elements.Count == 1)
                return elements[0];

            // Find element with highest confidence
            var bestElement = elements.OrderByDescending(e => e.Confidence).First();

            // Calculate average position and size
            var avgX = (int)elements.Average(e => e.BoundingBox.X);
            var avgY = (int)elements.Average(e => e.BoundingBox.Y);
            var avgWidth = (int)elements.Average(e => e.BoundingBox.Width);
            var avgHeight = (int)elements.Average(e => e.BoundingBox.Height);

            var consensusBounds = new Rectangle(avgX, avgY, avgWidth, avgHeight);

            // Calculate weighted confidence
            var weightedConfidence = CalculateWeightedConfidence(elements);

            // Combine text from all elements
            var combinedText = string.Join(" ", elements.Where(e => !string.IsNullOrWhiteSpace(e.Text))
                .Select(e => e.Text.Trim()).Distinct());

            // Create consensus element
            return new ElementInfo(
                consensusBounds,
                bestElement.ElementType,
                weightedConfidence,
                combinedText,
                CombineProperties(elements),
                bestElement.Features,
                elements.Average(e => e.TypeConfidence));
        }

        /// <inheritdoc />
        public double CalculateConsensusConfidence(IEnumerable<double> confidenceScores, IDictionary<string, double> strategyWeights)
        {
            var scores = confidenceScores?.ToList();
            if (scores == null || scores.Count == 0)
                return 0.0;

            if (strategyWeights == null || strategyWeights.Count == 0)
                return scores.Average();

            double weightedSum = 0.0;
            double totalWeight = 0.0;

            for (int i = 0; i < scores.Count; i++)
            {
                var weight = strategyWeights.Values.Skip(i).FirstOrDefault();
                if (weight <= 0) weight = 1.0;

                weightedSum += scores[i] * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? weightedSum / totalWeight : scores.Average();
        }

        /// <inheritdoc />
        public bool IsConfigured()
        {
            return _strategyWeights != null && _overlapThreshold >= 0.0 && _minimumConsensus >= 0.0;
        }

        /// <summary>
        /// Groups overlapping elements together for conflict resolution.
        /// </summary>
        /// <param name="elements">All detected elements with their strategy IDs.</param>
        /// <returns>Groups of overlapping elements.</returns>
        private List<List<(IElementInfo element, string strategyId)>> GroupOverlappingElements(
            List<(IElementInfo element, string strategyId)> elements)
        {
            var groups = new List<List<(IElementInfo element, string strategyId)>>();
            var processed = new HashSet<IElementInfo>();

            foreach (var (element, strategyId) in elements)
            {
                if (processed.Contains(element))
                    continue;

                var group = new List<(IElementInfo element, string strategyId)> { (element, strategyId) };
                processed.Add(element);

                // Find overlapping elements
                foreach (var (otherElement, otherStrategyId) in elements)
                {
                    if (processed.Contains(otherElement))
                        continue;

                    if (CalculateOverlap(element.BoundingBox, otherElement.BoundingBox) >= _overlapThreshold)
                    {
                        group.Add((otherElement, otherStrategyId));
                        processed.Add(otherElement);
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        /// <summary>
        /// Resolves a group of overlapping elements into a single consensus element.
        /// </summary>
        /// <param name="elementGroup">Group of overlapping elements.</param>
        /// <param name="warnings">List to add warnings to.</param>
        /// <returns>Consensus element or null if no consensus can be reached.</returns>
        private async Task<IElementInfo> ResolveElementGroup(
            List<(IElementInfo element, string strategyId)> elementGroup,
            List<string> warnings)
        {
            if (elementGroup.Count == 1)
                return elementGroup[0].element;

            // Calculate consensus score for this group
            var consensusScore = CalculateGroupConsensus(elementGroup);
            if (consensusScore < _minimumConsensus)
            {
                warnings.Add($"Element group rejected due to low consensus: {consensusScore:F2} < {_minimumConsensus:F2}");
                return null;
            }

            // Resolve conflicts using weighted voting
            var elements = elementGroup.Select(eg => eg.element).ToList();
            return ResolveConflict(elements);
        }

        /// <summary>
        /// Calculates the consensus score for a group of elements.
        /// </summary>
        /// <param name="elementGroup">Group of elements to evaluate.</param>
        /// <returns>Consensus score (0.0 to 1.0).</returns>
        private double CalculateGroupConsensus(List<(IElementInfo element, string strategyId)> elementGroup)
        {
            if (elementGroup.Count <= 1)
                return 1.0;

            // Factor 1: Agreement on element type
            var elementTypes = elementGroup.Select(eg => eg.element.ElementType).ToList();
            var typeAgreement = (double)elementTypes.GroupBy(t => t).Max(g => g.Count()) / elementTypes.Count;

            // Factor 2: Spatial agreement (overlap)
            var spatialAgreements = new List<double>();
            for (int i = 0; i < elementGroup.Count; i++)
            {
                for (int j = i + 1; j < elementGroup.Count; j++)
                {
                    var overlap = CalculateOverlap(
                        elementGroup[i].element.BoundingBox,
                        elementGroup[j].element.BoundingBox);
                    spatialAgreements.Add(overlap);
                }
            }
            var spatialAgreement = spatialAgreements.Count > 0 ? spatialAgreements.Average() : 1.0;

            // Factor 3: Confidence agreement
            var confidences = elementGroup.Select(eg => eg.element.Confidence).ToList();
            var avgConfidence = confidences.Average();
            var confidenceVariance = confidences.Sum(c => Math.Pow(c - avgConfidence, 2)) / confidences.Count;
            var confidenceAgreement = Math.Max(0.0, 1.0 - Math.Sqrt(confidenceVariance));

            // Weighted combination
            return (typeAgreement * 0.4) + (spatialAgreement * 0.4) + (confidenceAgreement * 0.2);
        }

        /// <summary>
        /// Calculates the overlap ratio between two rectangles.
        /// </summary>
        /// <param name="rect1">First rectangle.</param>
        /// <param name="rect2">Second rectangle.</param>
        /// <returns>Overlap ratio (0.0 to 1.0).</returns>
        private double CalculateOverlap(Rectangle rect1, Rectangle rect2)
        {
            var intersect = Rectangle.Intersect(rect1, rect2);
            if (intersect.IsEmpty)
                return 0.0;

            var intersectArea = intersect.Width * intersect.Height;
            var unionArea = (rect1.Width * rect1.Height) + (rect2.Width * rect2.Height) - intersectArea;

            return unionArea > 0 ? (double)intersectArea / unionArea : 0.0;
        }

        /// <summary>
        /// Calculates weighted confidence based on strategy weights.
        /// </summary>
        /// <param name="elements">Elements to calculate confidence for.</param>
        /// <returns>Weighted confidence score.</returns>
        private double CalculateWeightedConfidence(List<IElementInfo> elements)
        {
            if (elements.Count == 1)
                return elements[0].Confidence;

            double weightedSum = 0.0;
            double totalWeight = 0.0;

            foreach (var element in elements)
            {
                // For this implementation, assume equal weights if strategy info is not available
                double weight = 1.0;
                weightedSum += element.Confidence * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? weightedSum / totalWeight : elements.Average(e => e.Confidence);
        }

        /// <summary>
        /// Calculates overall confidence for the aggregated result.
        /// </summary>
        /// <param name="results">Individual detection results.</param>
        /// <param name="consensusElementCount">Number of consensus elements.</param>
        /// <returns>Overall confidence score.</returns>
        private double CalculateOverallConfidence(List<IDetectionResult> results, int consensusElementCount)
        {
            if (results.Count == 0)
                return 0.0;

            // Base confidence from individual results
            var baseConfidence = results.Average(r => r.OverallConfidence);

            // Confidence boost from consensus
            var consensusBoost = Math.Min(0.2, consensusElementCount * 0.02);

            // Strategy diversity bonus
            var uniqueStrategies = results.Select(r => r.StrategyId).Distinct().Count();
            var diversityBonus = Math.Min(0.1, (uniqueStrategies - 1) * 0.05);

            return Math.Min(1.0, baseConfidence + consensusBoost + diversityBonus);
        }

        /// <summary>
        /// Creates aggregated metadata from individual results.
        /// </summary>
        /// <param name="results">Individual detection results.</param>
        /// <param name="elementGroupCount">Number of element groups processed.</param>
        /// <returns>Aggregated metadata dictionary.</returns>
        private Dictionary<string, object> CreateAggregatedMetadata(List<IDetectionResult> results, int elementGroupCount)
        {
            var metadata = new Dictionary<string, object>
            {
                ["AggregationMethod"] = Name,
                ["SourceResultCount"] = results.Count,
                ["ElementGroupCount"] = elementGroupCount,
                ["OverlapThreshold"] = _overlapThreshold,
                ["MinimumConsensus"] = _minimumConsensus,
                ["StrategyWeights"] = new Dictionary<string, double>(_strategyWeights)
            };

            // Add strategy-specific metadata
            foreach (var result in results)
            {
                metadata[$"Strategy_{result.StrategyId}_Elements"] = result.DetectedElements.Count;
                metadata[$"Strategy_{result.StrategyId}_Confidence"] = result.OverallConfidence;
                metadata[$"Strategy_{result.StrategyId}_ProcessingTime"] = result.ProcessingTime.TotalMilliseconds;
            }

            return metadata;
        }

        /// <summary>
        /// Combines properties from multiple elements.
        /// </summary>
        /// <param name="elements">Elements to combine properties from.</param>
        /// <returns>Combined properties dictionary.</returns>
        private Dictionary<string, object> CombineProperties(List<IElementInfo> elements)
        {
            var combinedProperties = new Dictionary<string, object>();

            foreach (var element in elements)
            {
                foreach (var property in element.Properties)
                {
                    if (!combinedProperties.ContainsKey(property.Key))
                    {
                        combinedProperties[property.Key] = property.Value;
                    }
                    else if (combinedProperties[property.Key] != property.Value)
                    {
                        // Handle conflicting property values
                        combinedProperties[$"{property.Key}_Conflict"] = new[] { combinedProperties[property.Key], property.Value };
                    }
                }
            }

            return combinedProperties;
        }

        /// <summary>
        /// Gets default strategy weights.
        /// </summary>
        /// <returns>Default strategy weights dictionary.</returns>
        private static Dictionary<string, double> GetDefaultWeights()
        {
            return new Dictionary<string, double>
            {
                ["AI_Detection"] = 1.0,
                ["Feature_Detection"] = 0.8,
                ["Template_Matching"] = 0.6,
                ["OCR_Detection"] = 0.7
            };
        }
    }
}