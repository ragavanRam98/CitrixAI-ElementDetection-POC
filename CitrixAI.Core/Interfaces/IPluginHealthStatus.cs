using System;
using System.Collections.Generic;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Represents the health status of a plugin.
    /// Provides information about plugin operational state and issues.
    /// </summary>
    public interface IPluginHealthStatus
    {
        /// <summary>
        /// Gets the overall health status of the plugin.
        /// </summary>
        PluginStatus Status { get; }

        /// <summary>
        /// Gets the timestamp of the last health check.
        /// </summary>
        DateTime LastCheckTime { get; }

        /// <summary>
        /// Gets the uptime of the plugin.
        /// </summary>
        TimeSpan Uptime { get; }

        /// <summary>
        /// Gets any error messages or issues.
        /// </summary>
        IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Gets warning messages.
        /// </summary>
        IReadOnlyList<string> Warnings { get; }

        /// <summary>
        /// Gets informational messages.
        /// </summary>
        IReadOnlyList<string> Information { get; }

        /// <summary>
        /// Gets detailed health metrics.
        /// </summary>
        IDictionary<string, object> HealthMetrics { get; }

        /// <summary>
        /// Gets a value indicating whether the plugin is responding.
        /// </summary>
        bool IsResponding { get; }

        /// <summary>
        /// Gets the memory usage of the plugin in bytes.
        /// </summary>
        long MemoryUsage { get; }

        /// <summary>
        /// Gets the CPU usage percentage (0.0 to 100.0).
        /// </summary>
        double CpuUsage { get; }
    }

    /// <summary>
    /// Defines the possible plugin status values.
    /// </summary>
    public enum PluginStatus
    {
        /// <summary>
        /// Plugin status is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Plugin is healthy and operating normally.
        /// </summary>
        Healthy = 1,

        /// <summary>
        /// Plugin is operational but has warnings.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Plugin has errors but is still functional.
        /// </summary>
        Error = 3,

        /// <summary>
        /// Plugin is not responding or has failed.
        /// </summary>
        Critical = 4,

        /// <summary>
        /// Plugin is disabled or not running.
        /// </summary>
        Disabled = 5
    }
}