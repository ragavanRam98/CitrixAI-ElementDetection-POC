using System.Collections.Generic;
using System.Threading.Tasks;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for aggregating detection results from multiple strategies.
    /// Implements consensus algorithms and conflict resolution.
    /// </summary>
    public interface IResultAggregator
    {
        /// <summary>
        /// Gets the name of this aggregation strategy.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the minimum number of results required for aggregation.
        /// </summary>
        int MinimumResults { get; }

        /// <summary>
        /// Aggregates multiple detection results into a single consolidated result.
        /// </summary>
        /// <param name="results">The collection of detection results to aggregate.</param>
        /// <param name="context">The original detection context.</param>
        /// <returns>Aggregated detection result with consensus data.</returns>
        Task<IDetectionResult> AggregateAsync(IEnumerable<IDetectionResult> results, IDetectionContext context);

        /// <summary>
        /// Resolves conflicts between overlapping elements from different strategies.
        /// </summary>
        /// <param name="conflictingElements">Elements that overlap or conflict.</param>
        /// <returns>Resolved element or null if conflict cannot be resolved.</returns>
        IElementInfo ResolveConflict(IEnumerable<IElementInfo> conflictingElements);

        /// <summary>
        /// Calculates consensus confidence based on multiple strategy results.
        /// </summary>
        /// <param name="confidenceScores">Confidence scores from different strategies.</param>
        /// <param name="strategyWeights">Weights for each strategy.</param>
        /// <returns>Consensus confidence score (0.0 to 1.0).</returns>
        double CalculateConsensusConfidence(IEnumerable<double> confidenceScores, IDictionary<string, double> strategyWeights);

        /// <summary>
        /// Validates the aggregator configuration.
        /// </summary>
        /// <returns>True if the aggregator is properly configured, false otherwise.</returns>
        bool IsConfigured();
    }
}