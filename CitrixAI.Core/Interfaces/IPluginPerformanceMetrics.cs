using System;
using System.Collections.Generic;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Provides performance metrics for a plugin.
    /// Contains timing, throughput, and resource utilization data.
    /// </summary>
    public interface IPluginPerformanceMetrics
    {
        /// <summary>
        /// Gets the total number of operations performed.
        /// </summary>
        long TotalOperations { get; }

        /// <summary>
        /// Gets the number of successful operations.
        /// </summary>
        long SuccessfulOperations { get; }

        /// <summary>
        /// Gets the number of failed operations.
        /// </summary>
        long FailedOperations { get; }

        /// <summary>
        /// Gets the success rate as a percentage (0.0 to 100.0).
        /// </summary>
        double SuccessRate { get; }

        /// <summary>
        /// Gets the average operation duration.
        /// </summary>
        TimeSpan AverageOperationTime { get; }

        /// <summary>
        /// Gets the minimum operation duration recorded.
        /// </summary>
        TimeSpan MinOperationTime { get; }

        /// <summary>
        /// Gets the maximum operation duration recorded.
        /// </summary>
        TimeSpan MaxOperationTime { get; }

        /// <summary>
        /// Gets the operations per second throughput.
        /// </summary>
        double OperationsPerSecond { get; }

        /// <summary>
        /// Gets the current memory usage in bytes.
        /// </summary>
        long CurrentMemoryUsage { get; }

        /// <summary>
        /// Gets the peak memory usage in bytes.
        /// </summary>
        long PeakMemoryUsage { get; }

        /// <summary>
        /// Gets the current CPU usage percentage (0.0 to 100.0).
        /// </summary>
        double CurrentCpuUsage { get; }

        /// <summary>
        /// Gets the average CPU usage percentage (0.0 to 100.0).
        /// </summary>
        double AverageCpuUsage { get; }

        /// <summary>
        /// Gets additional custom performance metrics.
        /// </summary>
        IDictionary<string, object> CustomMetrics { get; }

        /// <summary>
        /// Gets the time when metrics collection started.
        /// </summary>
        DateTime MetricsStartTime { get; }

        /// <summary>
        /// Gets the last time metrics were updated.
        /// </summary>
        DateTime LastUpdateTime { get; }
    }
}