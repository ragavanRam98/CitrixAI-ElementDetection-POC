using System;
using CitrixAI.Core.Interfaces;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for detection result caching operations.
    /// Provides simple cache interface following Interface Segregation Principle.
    /// </summary>
    public interface IDetectionCache : IDisposable
    {
        /// <summary>
        /// Attempts to retrieve a cached detection result by image hash.
        /// </summary>
        /// <param name="imageHash">The hash of the source image.</param>
        /// <param name="result">The cached detection result if found.</param>
        /// <returns>True if result was found in cache, false otherwise.</returns>
        bool TryGet(string imageHash, out IDetectionResult result);

        /// <summary>
        /// Stores a detection result in the cache with the given image hash.
        /// </summary>
        /// <param name="imageHash">The hash of the source image.</param>
        /// <param name="result">The detection result to cache.</param>
        void Store(string imageHash, IDetectionResult result);

        /// <summary>
        /// Clears all cached results.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the current number of cached results.
        /// </summary>
        int Count { get; }
    }
}