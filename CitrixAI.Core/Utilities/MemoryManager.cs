using System;
using System.Diagnostics;

namespace CitrixAI.Core.Utilities
{
    /// <summary>
    /// Provides memory management utilities and monitoring capabilities.
    /// Centralized memory operations following Single Responsibility Principle.
    /// </summary>
    public static class MemoryManager
    {
        private const long MEMORY_PRESSURE_THRESHOLD_MB = 500;
        private const long BYTES_PER_MB = 1024 * 1024;

        /// <summary>
        /// Gets the current memory usage of the process in megabytes.
        /// </summary>
        /// <returns>Memory usage in MB.</returns>
        public static long GetCurrentMemoryUsage()
        {
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    return process.WorkingSet64 / BYTES_PER_MB;
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Forces garbage collection and waits for finalization.
        /// Use sparingly and only for testing or critical memory situations.
        /// </summary>
        public static void ForceGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// Logs memory usage with operation context.
        /// </summary>
        /// <param name="operation">Description of the operation being performed.</param>
        /// <param name="logAction">Action to perform logging (e.g., Console.WriteLine).</param>
        public static void LogMemoryUsage(string operation, Action<string> logAction = null)
        {
            var memoryUsage = GetCurrentMemoryUsage();
            var message = $"Memory usage during {operation}: {memoryUsage} MB";

            logAction?.Invoke(message);
        }

        /// <summary>
        /// Determines if the system is under memory pressure.
        /// </summary>
        /// <returns>True if memory usage exceeds threshold, false otherwise.</returns>
        public static bool IsMemoryPressureHigh()
        {
            return GetCurrentMemoryUsage() > MEMORY_PRESSURE_THRESHOLD_MB;
        }

        /// <summary>
        /// Executes an operation with before/after memory logging.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">Name of the operation for logging.</param>
        /// <param name="logAction">Action to perform logging.</param>
        /// <returns>Memory usage difference in MB.</returns>
        public static long ExecuteWithMemoryTracking(Action operation, string operationName, Action<string> logAction = null)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var memoryBefore = GetCurrentMemoryUsage();
            logAction?.Invoke($"Memory before {operationName}: {memoryBefore} MB");

            try
            {
                operation();
            }
            finally
            {
                var memoryAfter = GetCurrentMemoryUsage();
                var memoryDifference = memoryAfter - memoryBefore;

                logAction?.Invoke($"Memory after {operationName}: {memoryAfter} MB (difference: {memoryDifference:+#;-#;0} MB)");
            }

            return GetCurrentMemoryUsage() - memoryBefore;
        }

        /// <summary>
        /// Executes an operation with before/after memory logging and returns result.
        /// </summary>
        /// <typeparam name="T">Return type of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">Name of the operation for logging.</param>
        /// <param name="logAction">Action to perform logging.</param>
        /// <returns>Tuple containing operation result and memory difference.</returns>
        public static (T result, long memoryDifference) ExecuteWithMemoryTracking<T>(Func<T> operation, string operationName, Action<string> logAction = null)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var memoryBefore = GetCurrentMemoryUsage();
            logAction?.Invoke($"Memory before {operationName}: {memoryBefore} MB");

            T result;
            try
            {
                result = operation();
            }
            finally
            {
                var memoryAfter = GetCurrentMemoryUsage();
                var memoryDifference = memoryAfter - memoryBefore;

                logAction?.Invoke($"Memory after {operationName}: {memoryAfter} MB (difference: {memoryDifference:+#;-#;0} MB)");
            }

            return (result, GetCurrentMemoryUsage() - memoryBefore);
        }
    }
}