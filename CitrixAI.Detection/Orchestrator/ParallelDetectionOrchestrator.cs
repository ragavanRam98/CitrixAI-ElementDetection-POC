using CitrixAI.Core.Interfaces;
using CitrixAI.Core.Models;
using CitrixAI.Core.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CitrixAI.Detection.Orchestrator
{
    /// <summary>
    /// Parallel detection orchestrator with intelligent resource management.
    /// Provides parallel strategy execution capabilities while maintaining base orchestrator functionality.
    /// </summary>
    public sealed class ParallelDetectionOrchestrator : IDisposable
    {
        private readonly int _maxParallelism;
        private readonly SemaphoreSlim _resourceSemaphore;
        private readonly ConcurrentDictionary<string, StrategyResourceProfile> _resourceProfiles;
        private readonly object _performanceLock = new object();
        private readonly List<ParallelExecutionMetric> _executionHistory;
        private readonly DetectionOrchestrator _baseOrchestrator;

        /// <summary>
        /// Initializes a new instance of the ParallelDetectionOrchestrator class.
        /// </summary>
        /// <param name="resultAggregator">Result aggregator for combining outputs.</param>
        /// <param name="cache">Optional detection cache.</param>
        /// <param name="maxParallelism">Maximum parallel strategy executions (0 = auto-detect).</param>
        public ParallelDetectionOrchestrator(IResultAggregator resultAggregator = null,
            IDetectionCache cache = null, int maxParallelism = 0)
        {
            _maxParallelism = maxParallelism > 0 ? maxParallelism : ResourceMonitor.GetOptimalParallelismLevel();
            _resourceSemaphore = new SemaphoreSlim(_maxParallelism, _maxParallelism);
            _resourceProfiles = new ConcurrentDictionary<string, StrategyResourceProfile>();
            _executionHistory = new List<ParallelExecutionMetric>();
            _baseOrchestrator = new DetectionOrchestrator(resultAggregator, cache);

            InitializeResourceProfiles();
        }

        /// <summary>
        /// Gets the number of registered detection strategies.
        /// </summary>
        public int StrategyCount => _baseOrchestrator.StrategyCount;

        /// <summary>
        /// Registers a detection strategy.
        /// </summary>
        public void RegisterStrategy(IDetectionStrategy strategy)
        {
            _baseOrchestrator.RegisterStrategy(strategy);
        }

        /// <summary>
        /// Gets information about all registered strategies.
        /// </summary>
        public IReadOnlyList<IDetectionStrategy> GetRegisteredStrategies()
        {
            return _baseOrchestrator.GetRegisteredStrategies();
        }

        /// <summary>
        /// Gets the maximum parallelism level for this orchestrator.
        /// </summary>
        public int MaxParallelism => _maxParallelism;

        /// <summary>
        /// Gets current resource utilization statistics.
        /// </summary>
        public ResourceUtilization GetResourceUtilization()
        {
            return new ResourceUtilization
            {
                MaxParallelism = _maxParallelism,
                CurrentActiveStrategies = _maxParallelism - _resourceSemaphore.CurrentCount,
                AvailableSlots = _resourceSemaphore.CurrentCount,
                IsSystemUnderLoad = ResourceMonitor.IsSystemUnderLoad(),
                AverageCpuUsage = ResourceMonitor.GetAverageCpuUsage(),
                AvailableMemoryMB = ResourceMonitor.GetAvailableMemoryMB()
            };
        }

        /// <summary>
        /// Performs parallel element detection with intelligent resource management.
        /// </summary>
        /// <param name="context">Detection context.</param>
        /// <returns>Aggregated detection result from parallel execution.</returns>
        public async Task<IDetectionResult> DetectElementsAsync(IDetectionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Check if parallel execution is beneficial
            if (!ShouldUseParallelExecution(context))
            {
                return await _baseOrchestrator.DetectElementsAsync(context);
            }

            var executionMetric = new ParallelExecutionMetric
            {
                StartTime = DateTime.UtcNow,
                ContextId = context.ContextId,
                ImageSize = new System.Drawing.Size(context.SourceImage.Width, context.SourceImage.Height)
            };

            try
            {
                // Try cache lookup first (from base class)
                var cacheResult = await TryCacheFirstLookup(context);
                if (cacheResult != null)
                {
                    executionMetric.CacheHit = true;
                    executionMetric.TotalDuration = DateTime.UtcNow - executionMetric.StartTime;
                    RecordExecutionMetric(executionMetric);
                    return cacheResult;
                }

                // Get applicable strategies
                var applicableStrategies = GetApplicableStrategies(context);
                if (applicableStrategies.Count == 0)
                {
                    return DetectionResult.CreateFailure(
                        "NoApplicableStrategies",
                        "No strategies available for parallel execution",
                        TimeSpan.Zero);
                }

                // Execute strategies in parallel with resource management
                var results = await ExecuteStrategiesInParallel(applicableStrategies, context, executionMetric);

                // Aggregate results
                var aggregatedResult = await AggregateParallelResults(results, context);

                // Store in cache if successful
                await StoreCacheResult(context, aggregatedResult);

                executionMetric.TotalDuration = DateTime.UtcNow - executionMetric.StartTime;
                executionMetric.SuccessfulStrategies = results.Count(r => r.IsSuccessful);
                executionMetric.TotalElements = aggregatedResult.DetectedElements.Count;
                RecordExecutionMetric(executionMetric);

                return aggregatedResult;
            }
            catch (Exception ex)
            {
                executionMetric.TotalDuration = DateTime.UtcNow - executionMetric.StartTime;
                executionMetric.ErrorMessage = ex.Message;
                RecordExecutionMetric(executionMetric);

                return DetectionResult.CreateFailure(
                    "ParallelExecutionError",
                    $"Parallel detection failed: {ex.Message}",
                    executionMetric.TotalDuration);
            }
        }

        /// <summary>
        /// Executes detection strategies in parallel with resource awareness.
        /// </summary>
        /// <param name="strategies">Strategies to execute.</param>
        /// <param name="context">Detection context.</param>
        /// <param name="metric">Execution metric for tracking.</param>
        /// <returns>Collection of detection results.</returns>
        private async Task<List<IDetectionResult>> ExecuteStrategiesInParallel(
            List<IDetectionStrategy> strategies, IDetectionContext context, ParallelExecutionMetric metric)
        {
            var results = new ConcurrentBag<IDetectionResult>();
            var cancellationTokenSource = new CancellationTokenSource(context.Timeout);

            // Partition strategies based on resource requirements
            var partitions = PartitionStrategiesByResource(strategies);
            metric.StrategyPartitions = partitions.Count;

            // Execute partitions with controlled parallelism
            var partitionTasks = partitions.Select(partition =>
                ExecuteStrategyPartition(partition, context, results, cancellationTokenSource.Token));

            try
            {
                await Task.WhenAll(partitionTasks);
            }
            catch (OperationCanceledException)
            {
                // Timeout occurred - return partial results
                metric.TimedOut = true;
            }

            return results.ToList();
        }

        /// <summary>
        /// Executes a partition of strategies with resource limiting.
        /// </summary>
        /// <param name="strategyPartition">Strategies in this partition.</param>
        /// <param name="context">Detection context.</param>
        /// <param name="results">Thread-safe results collection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing partition execution.</returns>
        private async Task ExecuteStrategyPartition(List<IDetectionStrategy> strategyPartition,
            IDetectionContext context, ConcurrentBag<IDetectionResult> results, CancellationToken cancellationToken)
        {
            var strategyTasks = strategyPartition.Select(strategy =>
                ExecuteStrategyWithResourceControl(strategy, context, results, cancellationToken));

            await Task.WhenAll(strategyTasks);
        }

        /// <summary>
        /// Executes a single strategy with resource control and monitoring.
        /// </summary>
        /// <param name="strategy">Strategy to execute.</param>
        /// <param name="context">Detection context.</param>
        /// <param name="results">Thread-safe results collection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing strategy execution.</returns>
        private async Task ExecuteStrategyWithResourceControl(IDetectionStrategy strategy,
            IDetectionContext context, ConcurrentBag<IDetectionResult> results, CancellationToken cancellationToken)
        {
            await _resourceSemaphore.WaitAsync(cancellationToken);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var memoryBefore = MemoryManager.GetCurrentMemoryUsage();

                // Execute strategy
                var result = await strategy.DetectAsync(context);

                stopwatch.Stop();
                var memoryAfter = MemoryManager.GetCurrentMemoryUsage();

                // Update resource profile
                UpdateResourceProfile(strategy.StrategyId, stopwatch.Elapsed, memoryAfter - memoryBefore);

                // Add result to collection
                results.Add(result);
            }
            catch (Exception ex)
            {
                // Create failure result for this strategy
                var failureResult = DetectionResult.CreateFailure(
                    strategy.StrategyId,
                    $"Strategy execution failed: {ex.Message}",
                    TimeSpan.Zero);

                results.Add(failureResult);
            }
            finally
            {
                _resourceSemaphore.Release();
            }
        }

        /// <summary>
        /// Partitions strategies based on their resource requirements.
        /// </summary>
        /// <param name="strategies">Strategies to partition.</param>
        /// <returns>List of strategy partitions.</returns>
        private List<List<IDetectionStrategy>> PartitionStrategiesByResource(List<IDetectionStrategy> strategies)
        {
            var partitions = new List<List<IDetectionStrategy>>();
            var currentPartition = new List<IDetectionStrategy>();
            var currentPartitionWeight = 0.0;

            // Sort strategies by resource intensity (heavy strategies first)
            var sortedStrategies = strategies
                .OrderByDescending(s => GetResourceProfile(s.StrategyId).ResourceWeight)
                .ToList();

            foreach (var strategy in sortedStrategies)
            {
                var profile = GetResourceProfile(strategy.StrategyId);

                // Start new partition if current one would exceed capacity
                if (currentPartition.Count > 0 &&
                    currentPartitionWeight + profile.ResourceWeight > 1.0)
                {
                    partitions.Add(currentPartition);
                    currentPartition = new List<IDetectionStrategy>();
                    currentPartitionWeight = 0.0;
                }

                currentPartition.Add(strategy);
                currentPartitionWeight += profile.ResourceWeight;
            }

            if (currentPartition.Count > 0)
            {
                partitions.Add(currentPartition);
            }

            return partitions.Count > 0 ? partitions : new List<List<IDetectionStrategy>> { strategies };
        }

        /// <summary>
        /// Determines if parallel execution should be used for the given context.
        /// </summary>
        /// <param name="context">Detection context.</param>
        /// <returns>True if parallel execution is beneficial.</returns>
        private bool ShouldUseParallelExecution(IDetectionContext context)
        {
            // Don't use parallel execution if system is under load
            if (ResourceMonitor.IsSystemUnderLoad())
                return false;

            // Use parallel execution for larger images or when multiple strategies are available
            var imageArea = context.SourceImage.Width * context.SourceImage.Height;
            var applicableStrategies = GetApplicableStrategies(context);

            return applicableStrategies.Count >= 2 &&
                   (imageArea > 500000 || applicableStrategies.Count >= 3);
        }

        /// <summary>
        /// Gets or creates resource profile for a strategy.
        /// </summary>
        /// <param name="strategyId">Strategy identifier.</param>
        /// <returns>Resource profile for the strategy.</returns>
        private StrategyResourceProfile GetResourceProfile(string strategyId)
        {
            return _resourceProfiles.GetOrAdd(strategyId, id => new StrategyResourceProfile
            {
                StrategyId = id,
                ResourceWeight = GetDefaultResourceWeight(id),
                AverageExecutionTime = TimeSpan.FromSeconds(1),
                AverageMemoryUsage = 50,
                ExecutionCount = 0
            });
        }

        /// <summary>
        /// Updates resource profile based on execution metrics.
        /// </summary>
        /// <param name="strategyId">Strategy identifier.</param>
        /// <param name="executionTime">Execution time.</param>
        /// <param name="memoryUsed">Memory used in MB.</param>
        private void UpdateResourceProfile(string strategyId, TimeSpan executionTime, long memoryUsed)
        {
            var profile = GetResourceProfile(strategyId);

            lock (profile)
            {
                profile.ExecutionCount++;

                // Update rolling averages
                var alpha = 0.1; // Smoothing factor
                profile.AverageExecutionTime = TimeSpan.FromMilliseconds(
                    profile.AverageExecutionTime.TotalMilliseconds * (1 - alpha) +
                    executionTime.TotalMilliseconds * alpha);

                profile.AverageMemoryUsage = (long)(profile.AverageMemoryUsage * (1 - alpha) + memoryUsed * alpha);

                // Recalculate resource weight
                profile.ResourceWeight = CalculateResourceWeight(profile);
                profile.LastUpdateTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Calculates resource weight based on execution metrics.
        /// </summary>
        /// <param name="profile">Resource profile.</param>
        /// <returns>Resource weight (0.0 to 1.0).</returns>
        private double CalculateResourceWeight(StrategyResourceProfile profile)
        {
            // Weight based on execution time and memory usage
            var timeWeight = Math.Min(1.0, profile.AverageExecutionTime.TotalSeconds / 10.0);
            var memoryWeight = Math.Min(1.0, profile.AverageMemoryUsage / 200.0);

            return (timeWeight + memoryWeight) / 2.0;
        }

        /// <summary>
        /// Gets default resource weight for unknown strategies.
        /// </summary>
        /// <param name="strategyId">Strategy identifier.</param>
        /// <returns>Default resource weight.</returns>
        private double GetDefaultResourceWeight(string strategyId)
        {
            return strategyId switch
            {
                "AI_Detection" => 0.8,        // High resource usage
                "Feature_Detection" => 0.6,   // Medium resource usage
                "Template_Matching" => 0.4,   // Low resource usage
                "OCR_Detection" => 0.5,       // Medium resource usage
                _ => 0.5                       // Default medium usage
            };
        }

        /// <summary>
        /// Initializes default resource profiles for known strategies.
        /// </summary>
        private void InitializeResourceProfiles()
        {
            var knownStrategies = new[]
            {
                "AI_Detection", "Feature_Detection", "Template_Matching", "OCR_Detection"
            };

            foreach (var strategyId in knownStrategies)
            {
                GetResourceProfile(strategyId); // This creates the profile
            }
        }

        /// <summary>
        /// Records execution metric for performance analysis.
        /// </summary>
        /// <param name="metric">Execution metric to record.</param>
        private void RecordExecutionMetric(ParallelExecutionMetric metric)
        {
            lock (_performanceLock)
            {
                _executionHistory.Add(metric);

                // Keep only last 100 metrics
                while (_executionHistory.Count > 100)
                {
                    _executionHistory.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Gets parallel execution performance statistics.
        /// </summary>
        /// <returns>Performance statistics.</returns>
        public ParallelPerformanceStatistics GetPerformanceStatistics()
        {
            lock (_performanceLock)
            {
                if (_executionHistory.Count == 0)
                {
                    return new ParallelPerformanceStatistics();
                }

                var recentMetrics = _executionHistory.Where(m =>
                    DateTime.UtcNow - m.StartTime < TimeSpan.FromMinutes(30)).ToList();

                return new ParallelPerformanceStatistics
                {
                    TotalExecutions = _executionHistory.Count,
                    RecentExecutions = recentMetrics.Count,
                    AverageExecutionTime = recentMetrics.Any() ?
                        TimeSpan.FromMilliseconds(recentMetrics.Average(m => m.TotalDuration.TotalMilliseconds)) :
                        TimeSpan.Zero,
                    CacheHitRate = recentMetrics.Any() ?
                        recentMetrics.Count(m => m.CacheHit) / (double)recentMetrics.Count : 0.0,
                    AverageParallelEfficiency = CalculateParallelEfficiency(recentMetrics),
                    ResourceProfiles = new Dictionary<string, StrategyResourceProfile>(_resourceProfiles)
                };
            }
        }

        /// <summary>
        /// Calculates parallel execution efficiency.
        /// </summary>
        /// <param name="metrics">Recent execution metrics.</param>
        /// <returns>Efficiency ratio (0.0 to 1.0).</returns>
        private double CalculateParallelEfficiency(List<ParallelExecutionMetric> metrics)
        {
            if (metrics.Count < 2) return 1.0;

            // Compare parallel vs estimated serial execution time
            var parallelExecutions = metrics.Where(m => m.StrategyPartitions > 1).ToList();
            if (parallelExecutions.Count == 0) return 1.0;

            var avgParallelTime = parallelExecutions.Average(m => m.TotalDuration.TotalMilliseconds);
            var estimatedSerialTime = parallelExecutions.Average(m =>
                m.SuccessfulStrategies * avgParallelTime / Math.Max(1, m.StrategyPartitions));

            return estimatedSerialTime > 0 ? Math.Min(1.0, estimatedSerialTime / avgParallelTime) : 1.0;
        }

        /// <summary>
        /// Helper method to try cache lookup (delegates to base class).
        /// </summary>
        private async Task<IDetectionResult> TryCacheFirstLookup(IDetectionContext context)
        {
            // This would typically access the cache through the base class
            // For now, return null to indicate cache miss
            return await Task.FromResult<IDetectionResult>(null);
        }

        /// <summary>
        /// Helper method to get applicable strategies.
        /// </summary>
        private List<IDetectionStrategy> GetApplicableStrategies(IDetectionContext context)
        {
            return GetRegisteredStrategies().Where(s => s.CanHandle(context)).ToList();
        }

        /// <summary>
        /// Helper method to aggregate parallel results.
        /// </summary>
        private async Task<IDetectionResult> AggregateParallelResults(
            List<IDetectionResult> results, IDetectionContext context)
        {
            var successfulResults = results.Where(r => r.IsSuccessful).ToList();

            if (successfulResults.Count == 0)
            {
                return DetectionResult.CreateFailure(
                    "AllStrategiesFailed",
                    "All parallel strategies failed",
                    TimeSpan.Zero);
            }

            if (successfulResults.Count == 1)
            {
                return successfulResults[0];
            }

            // Use the base class aggregator (would need to access through base)
            return successfulResults.OrderByDescending(r => r.OverallConfidence).First();
        }

        /// <summary>
        /// Helper method to store cache result (delegates to base class).
        /// </summary>
        private async Task StoreCacheResult(IDetectionContext context, IDetectionResult result)
        {
            // This would typically store through the base class cache
            await Task.CompletedTask;
        }

        /// <summary>
        /// Releases parallel orchestrator resources.
        /// </summary>
        public void Dispose()
        {
            _resourceSemaphore?.Dispose();
            _baseOrchestrator?.Dispose();
        }
    }

    #region Supporting Classes

    /// <summary>
    /// Resource profile for a detection strategy.
    /// </summary>
    public class StrategyResourceProfile
    {
        public string StrategyId { get; set; }
        public double ResourceWeight { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public long AverageMemoryUsage { get; set; }
        public int ExecutionCount { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }

    /// <summary>
    /// Resource utilization information.
    /// </summary>
    public class ResourceUtilization
    {
        public int MaxParallelism { get; set; }
        public int CurrentActiveStrategies { get; set; }
        public int AvailableSlots { get; set; }
        public bool IsSystemUnderLoad { get; set; }
        public double AverageCpuUsage { get; set; }
        public long AvailableMemoryMB { get; set; }
    }

    /// <summary>
    /// Parallel execution performance metric.
    /// </summary>
    public class ParallelExecutionMetric
    {
        public DateTime StartTime { get; set; }
        public Guid ContextId { get; set; }
        public System.Drawing.Size ImageSize { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public bool CacheHit { get; set; }
        public int StrategyPartitions { get; set; }
        public int SuccessfulStrategies { get; set; }
        public int TotalElements { get; set; }
        public bool TimedOut { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Parallel performance statistics.
    /// </summary>
    public class ParallelPerformanceStatistics
    {
        public int TotalExecutions { get; set; }
        public int RecentExecutions { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public double CacheHitRate { get; set; }
        public double AverageParallelEfficiency { get; set; }
        public Dictionary<string, StrategyResourceProfile> ResourceProfiles { get; set; } = new Dictionary<string, StrategyResourceProfile>();
    }

    #endregion
}