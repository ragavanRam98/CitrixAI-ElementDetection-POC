using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for EleviantRPA plugin integration.
    /// Provides a standardized interface for the AI detection system.
    /// </summary>
    public interface IPluginContract
    {
        /// <summary>
        /// Gets the plugin metadata information.
        /// </summary>
        IPluginMetadata Metadata { get; }

        /// <summary>
        /// Initializes the plugin with the provided configuration.
        /// </summary>
        /// <param name="configuration">Plugin configuration parameters.</param>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync(IDictionary<string, object> configuration);

        /// <summary>
        /// Performs element detection using the AI-powered system.
        /// </summary>
        /// <param name="context">Detection context with image and search criteria.</param>
        /// <returns>Detection result with found elements.</returns>
        Task<IDetectionResult> DetectElementsAsync(IDetectionContext context);

        /// <summary>
        /// Performs template-based element detection (backward compatibility).
        /// </summary>
        /// <param name="sourceImage">The image to search in.</param>
        /// <param name="templateImage">The template image to find.</param>
        /// <param name="threshold">Confidence threshold (0.0 to 1.0).</param>
        /// <returns>Detection result with matching locations.</returns>
        Task<IDetectionResult> FindTemplateAsync(System.Drawing.Bitmap sourceImage, System.Drawing.Bitmap templateImage, double threshold = 0.8);

        /// <summary>
        /// Shuts down the plugin and releases resources.
        /// </summary>
        /// <returns>Task representing the shutdown operation.</returns>
        Task ShutdownAsync();

        /// <summary>
        /// Gets the current health status of the plugin.
        /// </summary>
        /// <returns>Health status information.</returns>
        IPluginHealthStatus GetHealthStatus();

        /// <summary>
        /// Gets performance metrics for the plugin.
        /// </summary>
        /// <returns>Performance metrics data.</returns>
        IPluginPerformanceMetrics GetPerformanceMetrics();
    }
}