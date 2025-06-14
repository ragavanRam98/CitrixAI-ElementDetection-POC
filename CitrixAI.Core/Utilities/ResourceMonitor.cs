using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CitrixAI.Core.Utilities
{
    /// <summary>
    /// Provides system resource monitoring and management capabilities.
    /// Supports intelligent resource allocation for parallel processing.
    /// </summary>
    public static class ResourceMonitor
    {
        private static readonly PerformanceCounter _cpuCounter;
        private static readonly object _lockObject = new object();
        private static DateTime _lastCpuUpdate = DateTime.MinValue;
        private static double _lastCpuReading = 0.0;
        private static readonly TimeSpan _cpuUpdateInterval = TimeSpan.FromSeconds(1);

        // System load thresholds
        private const double HIGH_CPU_THRESHOLD = 80.0;
        private const long LOW_MEMORY_THRESHOLD_MB = 500;
        private const double CPU_LOAD_WEIGHT = 0.7;
        private const double MEMORY_LOAD_WEIGHT = 0.3;

        static ResourceMonitor()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // First call returns 0, so we discard it
            }
            catch
            {
                // CPU counter might not be available in some environments
                _cpuCounter = null;
            }
        }

        /// <summary>
        /// Gets the optimal parallelism level based on system capabilities.
        /// </summary>
        /// <returns>Recommended number of parallel threads.</returns>
        public static int GetOptimalParallelismLevel()
        {
            var coreCount = Environment.ProcessorCount;
            var availableMemoryMB = GetAvailableMemoryMB();
            var currentCpuUsage = GetCurrentCpuUsage();

            // Base parallelism on core count
            var baseParallelism = Math.Max(1, coreCount);

            // Adjust based on system load
            if (currentCpuUsage > HIGH_CPU_THRESHOLD)
            {
                baseParallelism = Math.Max(1, baseParallelism / 2);
            }

            // Adjust based on available memory (each thread needs ~100MB)
            var memoryBasedLimit = Math.Max(1, (int)(availableMemoryMB / 100));

            // Use the more restrictive limit
            var optimalLevel = Math.Min(baseParallelism, memoryBasedLimit);

            // Cap at reasonable maximum
            return Math.Min(optimalLevel, 8);
        }

        /// <summary>
        /// Determines if the system is currently under high load.
        /// </summary>
        /// <returns>True if system is under load, false otherwise.</returns>
        public static bool IsSystemUnderLoad()
        {
            var cpuUsage = GetCurrentCpuUsage();
            var availableMemory = GetAvailableMemoryMB();
            var memoryPressure = MemoryManager.IsMemoryPressureHigh();

            // Calculate load score
            var cpuLoad = Math.Min(1.0, cpuUsage / 100.0);
            var memoryLoad = availableMemory < LOW_MEMORY_THRESHOLD_MB ? 1.0 : 0.0;

            var overallLoad = (cpuLoad * CPU_LOAD_WEIGHT) + (memoryLoad * MEMORY_LOAD_WEIGHT);

            return overallLoad > 0.8 || memoryPressure;
        }

        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        /// <returns>CPU usage percentage (0.0 to 100.0).</returns>
        public static double GetCurrentCpuUsage()
        {
            if (_cpuCounter == null)
                return EstimateCpuUsage();

            lock (_lockObject)
            {
                // Throttle CPU readings to avoid performance impact
                if (DateTime.UtcNow - _lastCpuUpdate < _cpuUpdateInterval)
                {
                    return _lastCpuReading;
                }

                try
                {
                    _lastCpuReading = _cpuCounter.NextValue();
                    _lastCpuUpdate = DateTime.UtcNow;
                    return _lastCpuReading;
                }
                catch
                {
                    return EstimateCpuUsage();
                }
            }
        }

        /// <summary>
        /// Gets the average CPU usage over a recent period.
        /// </summary>
        /// <returns>Average CPU usage percentage.</returns>
        public static double GetAverageCpuUsage()
        {
            // For this implementation, return current usage
            // In a production system, this would maintain a rolling average
            return GetCurrentCpuUsage();
        }

        /// <summary>
        /// Gets the available system memory in megabytes.
        /// </summary>
        /// <returns>Available memory in MB.</returns>
        public static long GetAvailableMemoryMB()
        {
            try
            {
                var memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                return (long)memoryCounter.NextValue();
            }
            catch
            {
                // Fallback estimation
                return EstimateAvailableMemory();
            }
        }

        /// <summary>
        /// Gets the number of available CPU cores.
        /// </summary>
        /// <returns>Number of CPU cores.</returns>
        public static int GetAvailableCpuCores()
        {
            return Environment.ProcessorCount;
        }

        /// <summary>
        /// Gets detailed system resource information.
        /// </summary>
        /// <returns>System resource information.</returns>
        public static SystemResourceInfo GetSystemResourceInfo()
        {
            return new SystemResourceInfo
            {
                CpuCores = GetAvailableCpuCores(),
                CurrentCpuUsage = GetCurrentCpuUsage(),
                AvailableMemoryMB = GetAvailableMemoryMB(),
                TotalMemoryMB = GetTotalSystemMemoryMB(),
                IsUnderLoad = IsSystemUnderLoad(),
                OptimalParallelism = GetOptimalParallelismLevel(),
                ProcessMemoryMB = MemoryManager.GetCurrentMemoryUsage(),
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Monitors resource usage for a specified duration.
        /// </summary>
        /// <param name="duration">Monitoring duration.</param>
        /// <param name="intervalMs">Sampling interval in milliseconds.</param>
        /// <returns>Resource monitoring data.</returns>
        public static ResourceMonitoringData MonitorResources(TimeSpan duration, int intervalMs = 1000)
        {
            var data = new ResourceMonitoringData
            {
                StartTime = DateTime.UtcNow,
                Duration = duration,
                SamplingInterval = TimeSpan.FromMilliseconds(intervalMs)
            };

            var endTime = DateTime.UtcNow.Add(duration);
            var samples = new System.Collections.Generic.List<ResourceSample>();

            while (DateTime.UtcNow < endTime)
            {
                samples.Add(new ResourceSample
                {
                    Timestamp = DateTime.UtcNow,
                    CpuUsage = GetCurrentCpuUsage(),
                    AvailableMemoryMB = GetAvailableMemoryMB(),
                    ProcessMemoryMB = MemoryManager.GetCurrentMemoryUsage()
                });

                Thread.Sleep(intervalMs);
            }

            data.Samples = samples;
            data.AverageCpuUsage = samples.Any() ? samples.Average(s => s.CpuUsage) : 0;
            data.PeakCpuUsage = samples.Any() ? samples.Max(s => s.CpuUsage) : 0;
            data.MinAvailableMemoryMB = samples.Any() ? samples.Min(s => s.AvailableMemoryMB) : 0;
            data.MaxProcessMemoryMB = samples.Any() ? samples.Max(s => s.ProcessMemoryMB) : 0;

            return data;
        }

        /// <summary>
        /// Waits for system resources to become available.
        /// </summary>
        /// <param name="maxWaitTime">Maximum time to wait.</param>
        /// <param name="checkInterval">Interval between resource checks.</param>
        /// <returns>True if resources became available, false if timed out.</returns>
        public static bool WaitForResources(TimeSpan maxWaitTime, TimeSpan? checkInterval = null)
        {
            var interval = checkInterval ?? TimeSpan.FromSeconds(1);
            var endTime = DateTime.UtcNow.Add(maxWaitTime);

            while (DateTime.UtcNow < endTime)
            {
                if (!IsSystemUnderLoad())
                    return true;

                Thread.Sleep(interval);
            }

            return false;
        }

        /// <summary>
        /// Estimates CPU usage when performance counters are not available.
        /// </summary>
        /// <returns>Estimated CPU usage percentage.</returns>
        private static double EstimateCpuUsage()
        {
            try
            {
                // Use Process.GetCurrentProcess() to estimate based on process threads
                using (var process = Process.GetCurrentProcess())
                {
                    var threadCount = process.Threads.Count;
                    var coreCount = Environment.ProcessorCount;

                    // Simple estimation based on thread activity
                    return Math.Min(100.0, (threadCount / (double)coreCount) * 25.0);
                }
            }
            catch
            {
                // Conservative estimate if all else fails
                return 30.0;
            }
        }

        /// <summary>
        /// Estimates available memory when performance counters are not available.
        /// </summary>
        /// <returns>Estimated available memory in MB.</returns>
        private static long EstimateAvailableMemory()
        {
            try
            {
                // Use GC information as a rough estimate
                var totalMemory = GC.GetTotalMemory(false);
                var estimatedTotal = totalMemory * 4; // Rough estimate
                return Math.Max(100, estimatedTotal / (1024 * 1024));
            }
            catch
            {
                // Conservative estimate
                return 1000; // 1GB
            }
        }

        /// <summary>
        /// Gets total system memory in megabytes.
        /// </summary>
        /// <returns>Total system memory in MB.</returns>
        private static long GetTotalSystemMemoryMB()
        {
            try
            {
                // Try WMI approach for .NET Framework
                using (var memoryCounter = new PerformanceCounter("Memory", "Available MBytes"))
                {
                    var available = memoryCounter.NextValue();
                    return (long)(available * 2); // Rough estimate: available * 2 = total
                }
            }
            catch
            {
                // Fallback estimation
                return GetAvailableMemoryMB() * 2; // Rough estimate
            }
        }

        /// <summary>
        /// Calculates resource efficiency score.
        /// </summary>
        /// <param name="targetCpuUsage">Target CPU usage percentage.</param>
        /// <param name="targetMemoryUsage">Target memory usage in MB.</param>
        /// <returns>Efficiency score (0.0 to 1.0).</returns>
        public static double CalculateResourceEfficiency(double targetCpuUsage = 70.0, long targetMemoryUsage = 200)
        {
            var currentCpu = GetCurrentCpuUsage();
            var currentMemory = MemoryManager.GetCurrentMemoryUsage();

            // Calculate efficiency based on how close we are to targets
            var cpuEfficiency = 1.0 - Math.Abs(currentCpu - targetCpuUsage) / 100.0;
            var memoryEfficiency = 1.0 - Math.Abs(currentMemory - targetMemoryUsage) / (double)targetMemoryUsage;

            cpuEfficiency = Math.Max(0.0, Math.Min(1.0, cpuEfficiency));
            memoryEfficiency = Math.Max(0.0, Math.Min(1.0, memoryEfficiency));

            return (cpuEfficiency + memoryEfficiency) / 2.0;
        }

        /// <summary>
        /// Gets resource recommendations for optimization.
        /// </summary>
        /// <returns>Resource optimization recommendations.</returns>
        public static ResourceRecommendations GetOptimizationRecommendations()
        {
            var recommendations = new ResourceRecommendations();
            var cpuUsage = GetCurrentCpuUsage();
            var availableMemory = GetAvailableMemoryMB();
            var processMemory = MemoryManager.GetCurrentMemoryUsage();

            // CPU recommendations
            if (cpuUsage > HIGH_CPU_THRESHOLD)
            {
                recommendations.CpuRecommendations.Add("Reduce parallel processing threads");
                recommendations.CpuRecommendations.Add("Consider caching frequently computed results");
                recommendations.Priority = RecommendationPriority.High;
            }
            else if (cpuUsage < 30.0)
            {
                recommendations.CpuRecommendations.Add("Consider increasing parallel processing");
                recommendations.CpuRecommendations.Add("Evaluate if more intensive algorithms could be used");
            }

            // Memory recommendations
            if (availableMemory < LOW_MEMORY_THRESHOLD_MB)
            {
                recommendations.MemoryRecommendations.Add("Clear unnecessary caches");
                recommendations.MemoryRecommendations.Add("Reduce image processing buffer sizes");
                recommendations.Priority = RecommendationPriority.High;
            }
            else if (processMemory > 500)
            {
                recommendations.MemoryRecommendations.Add("Monitor for memory leaks");
                recommendations.MemoryRecommendations.Add("Consider garbage collection optimization");
                if (recommendations.Priority < RecommendationPriority.Medium)
                    recommendations.Priority = RecommendationPriority.Medium;
            }

            // General recommendations
            if (IsSystemUnderLoad())
            {
                recommendations.GeneralRecommendations.Add("System is under load - consider reducing workload");
                recommendations.Priority = RecommendationPriority.High;
            }

            if (!recommendations.CpuRecommendations.Any() &&
                !recommendations.MemoryRecommendations.Any() &&
                !recommendations.GeneralRecommendations.Any())
            {
                recommendations.GeneralRecommendations.Add("System performance is optimal");
                recommendations.Priority = RecommendationPriority.Low;
            }

            return recommendations;
        }

        /// <summary>
        /// Disposes static resources.
        /// </summary>
        public static void Dispose()
        {
            try
            {
                _cpuCounter?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }

    #region Supporting Classes

    /// <summary>
    /// System resource information snapshot.
    /// </summary>
    public class SystemResourceInfo
    {
        public int CpuCores { get; set; }
        public double CurrentCpuUsage { get; set; }
        public long AvailableMemoryMB { get; set; }
        public long TotalMemoryMB { get; set; }
        public bool IsUnderLoad { get; set; }
        public int OptimalParallelism { get; set; }
        public long ProcessMemoryMB { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Resource monitoring data over time.
    /// </summary>
    public class ResourceMonitoringData
    {
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan SamplingInterval { get; set; }
        public System.Collections.Generic.List<ResourceSample> Samples { get; set; }
        public double AverageCpuUsage { get; set; }
        public double PeakCpuUsage { get; set; }
        public long MinAvailableMemoryMB { get; set; }
        public long MaxProcessMemoryMB { get; set; }
    }

    /// <summary>
    /// Single resource measurement sample.
    /// </summary>
    public class ResourceSample
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public long AvailableMemoryMB { get; set; }
        public long ProcessMemoryMB { get; set; }
    }

    /// <summary>
    /// Resource optimization recommendations.
    /// </summary>
    public class ResourceRecommendations
    {
        public System.Collections.Generic.List<string> CpuRecommendations { get; set; } = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> MemoryRecommendations { get; set; } = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> GeneralRecommendations { get; set; } = new System.Collections.Generic.List<string>();
        public RecommendationPriority Priority { get; set; } = RecommendationPriority.Low;
    }

    /// <summary>
    /// Priority levels for recommendations.
    /// </summary>
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}