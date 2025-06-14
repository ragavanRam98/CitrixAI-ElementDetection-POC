using CitrixAI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace CitrixAI.Core.Caching
{
    /// <summary>
    /// Advanced detection cache with perceptual hashing for similarity-based matching.
    /// Implements IDetectionCache with intelligent similarity detection.
    /// </summary>
    public sealed class AdvancedDetectionCache : IDetectionCache
    {
        private const double DEFAULT_SIMILARITY_THRESHOLD = 0.85;
        private const int PERCEPTUAL_HASH_SIZE = 8;

        private readonly double _similarityThreshold;
        private readonly Dictionary<string, CacheEntry> _advancedEntries;
        private readonly object _advancedLock = new object();
        private readonly DetectionCache _baseCache;

        /// <summary>
        /// Initializes a new instance of the AdvancedDetectionCache class.
        /// </summary>
        /// <param name="maxEntries">Maximum number of cache entries.</param>
        /// <param name="similarityThreshold">Threshold for considering images similar (0.0 to 1.0).</param>
        public AdvancedDetectionCache(int maxEntries = 50, double similarityThreshold = DEFAULT_SIMILARITY_THRESHOLD)
        {
            if (similarityThreshold < 0.0 || similarityThreshold > 1.0)
                throw new ArgumentOutOfRangeException(nameof(similarityThreshold));

            _similarityThreshold = similarityThreshold;
            _advancedEntries = new Dictionary<string, CacheEntry>();
            _baseCache = new DetectionCache(maxEntries);
        }

        /// <inheritdoc />
        public int Count => _baseCache.Count;

        /// <summary>
        /// Gets cache statistics including similarity matching performance.
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            lock (_advancedLock)
            {
                var entries = _advancedEntries.Values.ToList();
                return new CacheStatistics
                {
                    TotalEntries = entries.Count,
                    TotalHits = entries.Sum(e => e.HitCount),
                    TotalMisses = entries.Sum(e => e.MissCount),
                    AverageAccessTime = entries.Any() ? entries.Average(e => e.AverageAccessTime) : 0,
                    SimilarityHits = entries.Sum(e => e.SimilarityHits),
                    ExactHits = entries.Sum(e => e.ExactHits),
                    LastAccessTime = entries.Any() ? entries.Max(e => e.LastAccessTime) : DateTime.MinValue
                };
            }
        }

        /// <summary>
        /// Attempts to retrieve cached result using similarity matching.
        /// </summary>
        /// <param name="imageHash">Hash of the source image.</param>
        /// <param name="result">The cached detection result if found.</param>
        /// <returns>True if similar result found, false otherwise.</returns>
        public bool TryGet(string imageHash, out IDetectionResult result)
        {
            // First try exact match from base cache
            if (_baseCache.TryGet(imageHash, out result))
            {
                UpdateHitStatistics(imageHash, true, false);
                return true;
            }

            // Try similarity matching with perceptual hash
            return TryGetSimilar(imageHash, out result);
        }

        /// <summary>
        /// Stores detection result with advanced metadata.
        /// </summary>
        /// <param name="imageHash">Hash of the source image.</param>
        /// <param name="result">The detection result to cache.</param>
        public void Store(string imageHash, IDetectionResult result)
        {
            // Store in base cache
            _baseCache.Store(imageHash, result);

            // Store advanced metadata
            lock (_advancedLock)
            {
                if (!_advancedEntries.ContainsKey(imageHash))
                {
                    _advancedEntries[imageHash] = new CacheEntry
                    {
                        Hash = imageHash,
                        Result = result,
                        CreationTime = DateTime.UtcNow,
                        LastAccessTime = DateTime.UtcNow,
                        HitCount = 0,
                        MissCount = 0,
                        ExactHits = 0,
                        SimilarityHits = 0
                    };
                }
            }
        }

        /// <summary>
        /// Attempts similarity-based cache lookup using perceptual hashing.
        /// </summary>
        /// <param name="imageHash">Hash to find similar matches for.</param>
        /// <param name="result">Similar cached result if found.</param>
        /// <returns>True if similar result found, false otherwise.</returns>
        private bool TryGetSimilar(string imageHash, out IDetectionResult result)
        {
            result = null;

            try
            {
                lock (_advancedLock)
                {
                    var bestMatch = FindBestSimilarMatch(imageHash);
                    if (bestMatch != null)
                    {
                        result = bestMatch.Result;
                        UpdateHitStatistics(bestMatch.Hash, false, true);
                        return true;
                    }
                }

                UpdateMissStatistics();
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Finds the best similar match based on perceptual hash similarity.
        /// </summary>
        /// <param name="targetHash">Target hash to match against.</param>
        /// <returns>Best matching cache entry or null if none found.</returns>
        private CacheEntry FindBestSimilarMatch(string targetHash)
        {
            CacheEntry bestMatch = null;
            double bestSimilarity = 0.0;

            foreach (var entry in _advancedEntries.Values)
            {
                var similarity = CalculateHashSimilarity(targetHash, entry.Hash);
                if (similarity >= _similarityThreshold && similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestMatch = entry;
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Calculates similarity between two image hashes using Hamming distance.
        /// </summary>
        /// <param name="hash1">First hash.</param>
        /// <param name="hash2">Second hash.</param>
        /// <returns>Similarity score between 0.0 and 1.0.</returns>
        private double CalculateHashSimilarity(string hash1, string hash2)
        {
            if (string.IsNullOrEmpty(hash1) || string.IsNullOrEmpty(hash2))
                return 0.0;

            if (hash1.Length != hash2.Length)
                return 0.0;

            int differences = 0;
            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                    differences++;
            }

            return 1.0 - ((double)differences / hash1.Length);
        }

        /// <summary>
        /// Generates perceptual hash using DCT-based algorithm.
        /// </summary>
        /// <param name="image">Source image to hash.</param>
        /// <returns>Perceptual hash string.</returns>
        public static string GetPerceptualHash(Bitmap image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            try
            {
                // Resize to 32x32 for DCT computation
                using (var resized = new Bitmap(32, 32))
                {
                    using (var graphics = Graphics.FromImage(resized))
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(image, 0, 0, 32, 32);
                    }

                    // Convert to grayscale and compute DCT
                    var grayValues = new double[32, 32];
                    for (int y = 0; y < 32; y++)
                    {
                        for (int x = 0; x < 32; x++)
                        {
                            var pixel = resized.GetPixel(x, y);
                            grayValues[y, x] = (0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                        }
                    }

                    // Compute simplified DCT on top-left 8x8 region
                    var dctValues = ComputeDCT(grayValues, PERCEPTUAL_HASH_SIZE);

                    // Calculate median for threshold
                    var sortedValues = dctValues.Cast<double>()
                        .Where(v => !double.IsNaN(v))
                        .OrderBy(v => v)
                        .ToArray();

                    if (sortedValues.Length == 0)
                        return "0000000000000000"; // Fallback hash

                    var median = sortedValues[sortedValues.Length / 2];

                    // Generate hash based on DCT values vs median
                    var hashBits = new bool[PERCEPTUAL_HASH_SIZE * PERCEPTUAL_HASH_SIZE];
                    int index = 0;
                    for (int y = 0; y < PERCEPTUAL_HASH_SIZE; y++)
                    {
                        for (int x = 0; x < PERCEPTUAL_HASH_SIZE; x++)
                        {
                            hashBits[index++] = dctValues[y, x] > median;
                        }
                    }

                    // Convert to hex string
                    return BitsToHexString(hashBits);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate perceptual hash: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Computes simplified DCT for perceptual hashing.
        /// </summary>
        /// <param name="input">Input grayscale values.</param>
        /// <param name="size">Size of DCT region to compute.</param>
        /// <returns>DCT coefficient matrix.</returns>
        private static double[,] ComputeDCT(double[,] input, int size)
        {
            var result = new double[size, size];

            for (int u = 0; u < size; u++)
            {
                for (int v = 0; v < size; v++)
                {
                    double sum = 0.0;
                    for (int x = 0; x < 32; x++)
                    {
                        for (int y = 0; y < 32; y++)
                        {
                            sum += input[x, y] *
                                   Math.Cos(((2.0 * x + 1.0) * u * Math.PI) / (2.0 * 32)) *
                                   Math.Cos(((2.0 * y + 1.0) * v * Math.PI) / (2.0 * 32));
                        }
                    }

                    double cu = (u == 0) ? 1.0 / Math.Sqrt(2.0) : 1.0;
                    double cv = (v == 0) ? 1.0 / Math.Sqrt(2.0) : 1.0;

                    result[u, v] = 0.25 * cu * cv * sum;
                }
            }

            return result;
        }

        /// <summary>
        /// Converts bit array to hexadecimal string.
        /// </summary>
        /// <param name="bits">Bit array to convert.</param>
        /// <returns>Hexadecimal string representation.</returns>
        private static string BitsToHexString(bool[] bits)
        {
            var result = new char[bits.Length / 4];
            for (int i = 0; i < result.Length; i++)
            {
                int value = 0;
                for (int j = 0; j < 4; j++)
                {
                    if (bits[i * 4 + j])
                        value |= (1 << (3 - j));
                }
                result[i] = value.ToString("X")[0];
            }
            return new string(result);
        }

        /// <summary>
        /// Updates hit statistics for cache entry.
        /// </summary>
        /// <param name="hash">Hash of accessed entry.</param>
        /// <param name="exactHit">Whether this was an exact hash match.</param>
        /// <param name="similarityHit">Whether this was a similarity match.</param>
        private void UpdateHitStatistics(string hash, bool exactHit, bool similarityHit)
        {
            lock (_advancedLock)
            {
                if (_advancedEntries.TryGetValue(hash, out var entry))
                {
                    entry.HitCount++;
                    entry.LastAccessTime = DateTime.UtcNow;
                    if (exactHit) entry.ExactHits++;
                    if (similarityHit) entry.SimilarityHits++;
                }
            }
        }

        /// <summary>
        /// Updates miss statistics.
        /// </summary>
        private void UpdateMissStatistics()
        {
            lock (_advancedLock)
            {
                // Update global miss counter if needed
                foreach (var entry in _advancedEntries.Values)
                {
                    entry.MissCount++;
                    break; // Just update one entry as representative
                }
            }
        }

        /// <summary>
        /// Clears advanced cache entries.
        /// </summary>
        public void Clear()
        {
            _baseCache.Clear();
            lock (_advancedLock)
            {
                _advancedEntries.Clear();
            }
        }

        /// <summary>
        /// Releases advanced cache resources.
        /// </summary>
        public void Dispose()
        {
            lock (_advancedLock)
            {
                _advancedEntries.Clear();
            }
            _baseCache?.Dispose();
        }
    }

    /// <summary>
    /// Represents an advanced cache entry with metadata.
    /// </summary>
    public sealed class CacheEntry
    {
        public string Hash { get; set; }
        public IDetectionResult Result { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public int ExactHits { get; set; }
        public int SimilarityHits { get; set; }
        public double AverageAccessTime { get; set; }
    }

    /// <summary>
    /// Represents cache performance statistics.
    /// </summary>
    public sealed class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int TotalHits { get; set; }
        public int TotalMisses { get; set; }
        public double HitRatio => TotalHits + TotalMisses > 0 ? (double)TotalHits / (TotalHits + TotalMisses) : 0.0;
        public int SimilarityHits { get; set; }
        public int ExactHits { get; set; }
        public double SimilarityRatio => TotalHits > 0 ? (double)SimilarityHits / TotalHits : 0.0;
        public double AverageAccessTime { get; set; }
        public DateTime LastAccessTime { get; set; }
    }
}