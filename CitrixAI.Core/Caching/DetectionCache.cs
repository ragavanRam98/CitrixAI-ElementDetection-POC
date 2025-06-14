using CitrixAI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace CitrixAI.Core.Caching
{
    /// <summary>
    /// Implements detection result caching with LRU eviction policy.
    /// Provides O(1) access time and automatic memory management.
    /// </summary>
    public sealed class DetectionCache : IDetectionCache, IDisposable
    {
        private const int DEFAULT_MAX_ENTRIES = 50;

        private readonly int _maxEntries;
        private readonly Dictionary<string, CacheNode> _cache;
        private readonly object _cacheLock = new object();
        private CacheNode _head;
        private CacheNode _tail;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the DetectionCache class.
        /// </summary>
        /// <param name="maxEntries">Maximum number of cache entries (default: 50).</param>
        public DetectionCache(int maxEntries = DEFAULT_MAX_ENTRIES)
        {
            if (maxEntries <= 0)
                throw new ArgumentException("Max entries must be greater than 0", nameof(maxEntries));

            _maxEntries = maxEntries;
            _cache = new Dictionary<string, CacheNode>(maxEntries);

            // Initialize LRU doubly-linked list
            _head = new CacheNode(null, null);
            _tail = new CacheNode(null, null);
            _head.Next = _tail;
            _tail.Previous = _head;
        }

        /// <inheritdoc />
        public int Count
        {
            get
            {
                lock (_cacheLock)
                {
                    return _cache.Count;
                }
            }
        }

        /// <inheritdoc />
        public bool TryGet(string imageHash, out IDetectionResult result)
        {
            if (string.IsNullOrWhiteSpace(imageHash))
            {
                result = null;
                return false;
            }

            lock (_cacheLock)
            {
                if (_cache.TryGetValue(imageHash, out var node))
                {
                    // Move to front (most recently used)
                    MoveToFront(node);
                    result = node.Result;
                    return true;
                }

                result = null;
                return false;
            }
        }

        /// <inheritdoc />
        public void Store(string imageHash, IDetectionResult result)
        {
            if (string.IsNullOrWhiteSpace(imageHash))
                throw new ArgumentException("Image hash cannot be null or empty", nameof(imageHash));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            lock (_cacheLock)
            {
                // Update existing entry
                if (_cache.TryGetValue(imageHash, out var existingNode))
                {
                    existingNode.Result = result;
                    MoveToFront(existingNode);
                    return;
                }

                // Add new entry
                var newNode = new CacheNode(imageHash, result);
                _cache[imageHash] = newNode;
                AddToFront(newNode);

                // Evict oldest if necessary
                if (_cache.Count > _maxEntries)
                {
                    EvictOldestEntry();
                }
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            lock (_cacheLock)
            {
                _cache.Clear();
                _head.Next = _tail;
                _tail.Previous = _head;
            }
        }

        /// <summary>
        /// Generates MD5 hash from bitmap for cache key generation.
        /// </summary>
        /// <param name="image">The source bitmap to hash.</param>
        /// <returns>MD5 hash string of the image.</returns>
        public static string GetSimpleHash(Bitmap image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            try
            {
                // Downscale to 32x32 grayscale for consistent hashing
                using (var scaledImage = new Bitmap(32, 32))
                {
                    using (var graphics = Graphics.FromImage(scaledImage))
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(image, 0, 0, 32, 32);
                    }

                    // Convert to grayscale and generate hash
                    using (var stream = new MemoryStream())
                    {
                        scaledImage.Save(stream, ImageFormat.Bmp);
                        var imageBytes = stream.ToArray();

                        using (var md5 = MD5.Create())
                        {
                            var hashBytes = md5.ComputeHash(imageBytes);
                            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate image hash: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Moves a cache node to the front of the LRU list.
        /// </summary>
        /// <param name="node">The node to move to front.</param>
        private void MoveToFront(CacheNode node)
        {
            RemoveNode(node);
            AddToFront(node);
        }

        /// <summary>
        /// Adds a cache node to the front of the LRU list.
        /// </summary>
        /// <param name="node">The node to add to front.</param>
        private void AddToFront(CacheNode node)
        {
            node.Previous = _head;
            node.Next = _head.Next;
            _head.Next.Previous = node;
            _head.Next = node;
        }

        /// <summary>
        /// Removes a cache node from the LRU list.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        private void RemoveNode(CacheNode node)
        {
            node.Previous.Next = node.Next;
            node.Next.Previous = node.Previous;
        }

        /// <summary>
        /// Evicts the oldest (least recently used) cache entry.
        /// </summary>
        private void EvictOldestEntry()
        {
            var oldestNode = _tail.Previous;
            if (oldestNode != _head && oldestNode.Hash != null)
            {
                RemoveNode(oldestNode);
                _cache.Remove(oldestNode.Hash);
            }
        }

        /// <summary>
        /// Releases resources used by the DetectionCache.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Clear();
                _disposed = true;
            }
        }

        /// <summary>
        /// Represents a node in the LRU doubly-linked list.
        /// </summary>
        private sealed class CacheNode
        {
            public string Hash { get; }
            public IDetectionResult Result { get; set; }
            public CacheNode Previous { get; set; }
            public CacheNode Next { get; set; }

            public CacheNode(string hash, IDetectionResult result)
            {
                Hash = hash;
                Result = result;
            }
        }
    }
}