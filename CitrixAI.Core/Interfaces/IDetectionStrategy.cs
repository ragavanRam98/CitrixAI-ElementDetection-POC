using System;
using System.Drawing;
using System.Threading.Tasks;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for element detection strategies.
    /// Implements Strategy pattern for different detection algorithms.
    /// </summary>
    public interface IDetectionStrategy
    {
        /// <summary>
        /// Gets the unique identifier for this detection strategy.
        /// </summary>
        string StrategyId { get; }

        /// <summary>
        /// Gets the human-readable name of this detection strategy.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the priority of this strategy (higher values = higher priority).
        /// Used by the orchestrator for strategy selection.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Determines if this strategy can handle the given detection context.
        /// </summary>
        /// <param name="context">The detection context containing image and metadata.</param>
        /// <returns>True if this strategy can process the context, false otherwise.</returns>
        bool CanHandle(IDetectionContext context);

        /// <summary>
        /// Performs element detection on the provided image.
        /// </summary>
        /// <param name="context">The detection context containing image and search criteria.</param>
        /// <returns>Detection result containing found elements and confidence scores.</returns>
        Task<IDetectionResult> DetectAsync(IDetectionContext context);

        /// <summary>
        /// Gets the estimated processing time for this strategy.
        /// Used for optimization and strategy selection.
        /// </summary>
        /// <param name="imageSize">The size of the image to process.</param>
        /// <returns>Estimated processing time in milliseconds.</returns>
        TimeSpan GetEstimatedProcessingTime(Size imageSize);

        /// <summary>
        /// Validates the strategy configuration and dependencies.
        /// </summary>
        /// <returns>True if the strategy is properly configured, false otherwise.</returns>
        bool IsConfigured();
    }
}