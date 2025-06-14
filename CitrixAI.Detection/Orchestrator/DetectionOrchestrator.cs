using CitrixAI.Core.Interfaces;
using CitrixAI.Core.Models;
using CitrixAI.Core.Caching;
using CitrixAI.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CitrixAI.Detection.Orchestrator
{
    /// <summary>
    /// Orchestrates multiple detection strategies with intelligent caching and memory monitoring.
    /// Implements the Strategy pattern and coordinates parallel detection execution.
    /// </summary>
    public sealed class DetectionOrchestrator : IDisposable
    {
        private readonly List<IDetectionStrategy> _strategies;
        private readonly IResultAggregator _resultAggregator;
        private readonly IDetectionCache _detectionCache;
        private readonly object _strategiesLock = new object();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the DetectionOrchestrator class.
        /// </summary>
        /// <param name="resultAggregator">The result aggregator for combining detection results.</param>
        /// <param name="cache">Optional cache for detection results.</param>
        public DetectionOrchestrator(IResultAggregator resultAggregator = null, IDetectionCache cache = null)
        {
            _strategies = new List<IDetectionStrategy>();
            _resultAggregator = resultAggregator ?? new WeightedConsensusAggregator();
            _detectionCache = cache;
        }

        /// <summary>
        /// Gets the number of registered detection strategies.
        /// </summary>
        public int StrategyCount
        {
            get
            {
                lock (_strategiesLock)
                {
                    return _strategies.Count;
                }
            }
        }

        /// <summary>
        /// Gets cache statistics if cache is available.
        /// </summary>
        public int? CacheCount => _detectionCache?.Count;

        /// <summary>
        /// Registers a detection strategy with the orchestrator.
        /// </summary>
        /// <param name="strategy">The detection strategy to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when strategy is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when strategy is not properly configured.</exception>
        public void RegisterStrategy(IDetectionStrategy strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            if (!strategy.IsConfigured())
                throw new InvalidOperationException($"Strategy '{strategy.Name}' is not properly configured.");

            lock (_strategiesLock)
            {
                // Remove existing strategy with same ID if present
                _strategies.RemoveAll(s => s.StrategyId == strategy.StrategyId);

                // Add new strategy and sort by priority
                _strategies.Add(strategy);
                _strategies.Sort((s1, s2) => s2.Priority.CompareTo(s1.Priority));
            }

            LogMessage($"Strategy registered: {strategy.Name} (Priority: {strategy.Priority})");
        }

        /// <summary>
        /// Unregisters a detection strategy from the orchestrator.
        /// </summary>
        /// <param name="strategyId">The ID of the strategy to unregister.</param>
        /// <returns>True if the strategy was found and removed, false otherwise.</returns>
        public bool UnregisterStrategy(string strategyId)
        {
            if (string.IsNullOrWhiteSpace(strategyId))
                return false;

            lock (_strategiesLock)
            {
                var removedCount = _strategies.RemoveAll(s => s.StrategyId == strategyId);
                if (removedCount > 0)
                {
                    LogMessage($"Strategy unregistered: {strategyId}");
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Performs element detection with intelligent caching and memory monitoring.
        /// </summary>
        /// <param name="context">The detection context containing image and search criteria.</param>
        /// <returns>Cached or newly computed detection result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no strategies are registered.</exception>
        public async Task<IDetectionResult> DetectElementsAsync(IDetectionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            List<IDetectionStrategy> applicableStrategies;
            lock (_strategiesLock)
            {
                if (_strategies.Count == 0)
                    throw new InvalidOperationException("No detection strategies are registered.");

                applicableStrategies = _strategies.Where(s => s.CanHandle(context)).ToList();
            }

            if (applicableStrategies.Count == 0)
            {
                return DetectionResult.CreateFailure(
                    "NoApplicableStrategies",
                    "No registered strategies can handle the provided detection context.",
                    TimeSpan.Zero);
            }

            // Try cache lookup first
            string imageHash = null;
            if (_detectionCache != null)
            {
                try
                {
                    imageHash = DetectionCache.GetSimpleHash(context.SourceImage);
                    if (_detectionCache.TryGet(imageHash, out var cachedResult))
                    {
                        LogCacheHit(imageHash);
                        return cachedResult;
                    }
                    LogCacheMiss(imageHash);
                }
                catch (Exception ex)
                {
                    LogCacheError($"Cache lookup failed: {ex.Message}");
                }
            }

            // Execute detection with memory tracking
            IDetectionResult detectionResult;
            long memoryUsed;

            var memoryBefore = MemoryManager.GetCurrentMemoryUsage();
            LogMemoryUsage($"Memory before detection: {memoryBefore} MB");

            try
            {
                detectionResult = await ExecuteDetectionStrategies(applicableStrategies, context);
            }
            finally
            {
                var memoryAfter = MemoryManager.GetCurrentMemoryUsage();
                memoryUsed = memoryAfter - memoryBefore;
                LogMemoryUsage($"Memory after detection: {memoryAfter} MB (difference: {memoryUsed:+#;-#;0} MB)");
            }

            // Store successful results in cache
            if (_detectionCache != null && imageHash != null && detectionResult.IsSuccessful)
            {
                try
                {
                    _detectionCache.Store(imageHash, detectionResult);
                    LogCacheStore(imageHash, detectionResult.DetectedElements.Count);
                }
                catch (Exception ex)
                {
                    LogCacheError($"Cache storage failed: {ex.Message}");
                }
            }

            return detectionResult;
        }

        /// <summary>
        /// Executes detection strategies with existing orchestration logic.
        /// </summary>
        /// <param name="strategies">List of applicable strategies to execute.</param>
        /// <param name="context">The detection context.</param>
        /// <returns>Aggregated detection result.</returns>
        private async Task<IDetectionResult> ExecuteDetectionStrategies(List<IDetectionStrategy> strategies, IDetectionContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var detectionTasks = new List<Task<IDetectionResult>>();

            try
            {
                // Execute strategies in parallel
                foreach (var strategy in strategies)
                {
                    detectionTasks.Add(ExecuteStrategyWithTimeout(strategy, context));
                }

                // Wait for all strategies to complete or timeout
                var results = await Task.WhenAll(detectionTasks);
                stopwatch.Stop();

                // Filter out failed results
                var successfulResults = results.Where(r => r.IsSuccessful).ToList();

                if (successfulResults.Count == 0)
                {
                    var failedResults = results.Where(r => !r.IsSuccessful).ToList();
                    var combinedErrors = string.Join("; ", failedResults.SelectMany(r => r.Warnings));
                    return DetectionResult.CreateFailure(
                        "AllStrategiesFailed",
                        $"All detection strategies failed: {combinedErrors}",
                        stopwatch.Elapsed);
                }

                // Aggregate successful results
                if (successfulResults.Count == 1)
                {
                    return successfulResults[0];
                }

                return await _resultAggregator.AggregateAsync(successfulResults, context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return DetectionResult.CreateFailure(
                    "OrchestrationError",
                    $"Error during detection orchestration: {ex.Message}",
                    stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Executes a detection strategy with timeout protection.
        /// </summary>
        /// <param name="strategy">The strategy to execute.</param>
        /// <param name="context">The detection context.</param>
        /// <returns>Detection result or timeout result.</returns>
        private async Task<IDetectionResult> ExecuteStrategyWithTimeout(IDetectionStrategy strategy, IDetectionContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var detectionTask = strategy.DetectAsync(context);
                var timeoutTask = Task.Delay(context.Timeout);

                var completedTask = await Task.WhenAny(detectionTask, timeoutTask);
                stopwatch.Stop();

                if (completedTask == timeoutTask)
                {
                    return DetectionResult.CreateFailure(
                        strategy.StrategyId,
                        $"Strategy '{strategy.Name}' timed out after {context.Timeout.TotalSeconds} seconds.",
                        stopwatch.Elapsed);
                }

                return await detectionTask;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return DetectionResult.CreateFailure(
                    strategy.StrategyId,
                    $"Strategy '{strategy.Name}' failed with error: {ex.Message}",
                    stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Clears the detection cache if available.
        /// </summary>
        public void ClearCache()
        {
            _detectionCache?.Clear();
            LogMessage("Detection cache cleared");
        }

        /// <summary>
        /// Gets information about all registered strategies.
        /// </summary>
        /// <returns>Collection of strategy information.</returns>
        public IReadOnlyList<IDetectionStrategy> GetRegisteredStrategies()
        {
            lock (_strategiesLock)
            {
                return _strategies.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the optimal strategy for the given context.
        /// </summary>
        /// <param name="context">The detection context.</param>
        /// <returns>The most suitable strategy or null if none can handle the context.</returns>
        public IDetectionStrategy GetOptimalStrategy(IDetectionContext context)
        {
            if (context == null)
                return null;

            lock (_strategiesLock)
            {
                return _strategies.FirstOrDefault(s => s.CanHandle(context));
            }
        }

        /// <summary>
        /// Validates all registered strategies.
        /// </summary>
        /// <returns>Dictionary of strategy validation results.</returns>
        public IDictionary<string, bool> ValidateStrategies()
        {
            lock (_strategiesLock)
            {
                return _strategies.ToDictionary(s => s.StrategyId, s => s.IsConfigured());
            }
        }

        #region Cache and Memory Logging Methods

        private void LogCacheHit(string imageHash)
        {
            LogMessage($"Cache HIT for hash {imageHash.Substring(0, 8)}... (Cache size: {_detectionCache.Count})");
        }

        private void LogCacheMiss(string imageHash)
        {
            LogMessage($"Cache MISS for hash {imageHash.Substring(0, 8)}... (Cache size: {_detectionCache.Count})");
        }

        private void LogCacheStore(string imageHash, int elementCount)
        {
            LogMessage($"Cache STORE for hash {imageHash.Substring(0, 8)}... with {elementCount} elements (Cache size: {_detectionCache.Count})");
        }

        private void LogCacheError(string error)
        {
            LogMessage($"Cache ERROR: {error}");
        }

        private void LogMemoryUsage(string message)
        {
            LogMessage($"MEMORY: {message}");
        }

        private void LogMessage(string message)
        {
            // This integrates with the ViewModel logging system
            Debug.WriteLine($"[DetectionOrchestrator] {message}");
        }

        #endregion

        /// <summary>
        /// Releases resources used by the DetectionOrchestrator.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_strategiesLock)
                {
                    foreach (var strategy in _strategies.OfType<IDisposable>())
                    {
                        try
                        {
                            strategy.Dispose();
                        }
                        catch
                        {
                            // Ignore disposal errors
                        }
                    }
                    _strategies.Clear();
                }

                if (_resultAggregator is IDisposable disposableAggregator)
                {
                    disposableAggregator.Dispose();
                }

                _detectionCache?.Dispose();
                _disposed = true;
            }
        }
    }
}