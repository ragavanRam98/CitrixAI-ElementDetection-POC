using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CitrixAI.Vision.Processing
{
    /// <summary>
    /// Advanced image processing pipeline for enhanced element detection.
    /// Provides multi-scale processing, noise reduction, and quality enhancement.
    /// </summary>
    public sealed class AdvancedImageProcessor : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Configuration for multi-scale processing.
        /// </summary>
        public class MultiScaleConfig
        {
            public double[] ScaleFactors { get; set; } = { 0.5, 0.75, 1.0, 1.25, 1.5 };
            public int MinElementSize { get; set; } = 10;
            public int MaxElementSize { get; set; } = 500;
            public bool EnableAdaptiveScaling { get; set; } = true;
        }

        /// <summary>
        /// Configuration for noise reduction processing.
        /// </summary>
        public class NoiseReductionConfig
        {
            public int GaussianKernelSize { get; set; } = 5;
            public double GaussianSigma { get; set; } = 1.0;
            public int MedianFilterSize { get; set; } = 3;
            public bool EnableBilateralFilter { get; set; } = true;
            public double BilateralSigmaColor { get; set; } = 75.0;
            public double BilateralSigmaSpace { get; set; } = 75.0;
        }

        /// <summary>
        /// Configuration for edge detection optimization.
        /// </summary>
        public class EdgeDetectionConfig
        {
            public double CannyThreshold1 { get; set; } = 50.0;
            public double CannyThreshold2 { get; set; } = 150.0;
            public int CannyApertureSize { get; set; } = 3;
            public bool EnableMorphologyOperations { get; set; } = true;
            public int MorphologyKernelSize { get; set; } = 3;
        }

        private readonly MultiScaleConfig _multiScaleConfig;
        private readonly NoiseReductionConfig _noiseReductionConfig;
        private readonly EdgeDetectionConfig _edgeDetectionConfig;

        /// <summary>
        /// Initializes a new instance of the AdvancedImageProcessor class.
        /// </summary>
        public AdvancedImageProcessor(
            MultiScaleConfig multiScaleConfig = null,
            NoiseReductionConfig noiseReductionConfig = null,
            EdgeDetectionConfig edgeDetectionConfig = null)
        {
            _multiScaleConfig = multiScaleConfig ?? new MultiScaleConfig();
            _noiseReductionConfig = noiseReductionConfig ?? new NoiseReductionConfig();
            _edgeDetectionConfig = edgeDetectionConfig ?? new EdgeDetectionConfig();
        }

        /// <summary>
        /// Creates multiple scaled versions of an image for multi-scale detection.
        /// </summary>
        /// <param name="source">Source image to scale.</param>
        /// <returns>Dictionary of scale factors to scaled images.</returns>
        public Dictionary<double, Mat> CreateMultiScalePyramid(Mat source)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            var pyramid = new Dictionary<double, Mat>();

            try
            {
                foreach (var scaleFactor in _multiScaleConfig.ScaleFactors)
                {
                    var scaledSize = new OpenCvSharp.Size(
                        (int)(source.Width * scaleFactor),
                        (int)(source.Height * scaleFactor));

                    // Skip scales that would create images too small or too large
                    if (scaledSize.Width < _multiScaleConfig.MinElementSize ||
                        scaledSize.Height < _multiScaleConfig.MinElementSize ||
                        scaledSize.Width > source.Width * 2 ||
                        scaledSize.Height > source.Height * 2)
                    {
                        continue;
                    }

                    var scaledImage = new Mat();
                    Cv2.Resize(source, scaledImage, scaledSize, interpolation: InterpolationFlags.Cubic);
                    pyramid[scaleFactor] = scaledImage;
                }

                return pyramid;
            }
            catch (Exception ex)
            {
                // Clean up any created images on error
                foreach (var image in pyramid.Values)
                {
                    image?.Dispose();
                }
                throw new InvalidOperationException($"Failed to create multi-scale pyramid: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Applies advanced noise reduction to improve image quality.
        /// </summary>
        /// <param name="source">Source image to process.</param>
        /// <returns>Noise-reduced image.</returns>
        public Mat ReduceNoise(Mat source)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            try
            {
                var processed = new Mat();
                source.CopyTo(processed);

                // Apply Gaussian blur for basic noise reduction
                if (_noiseReductionConfig.GaussianKernelSize > 1)
                {
                    using (var gaussianResult = new Mat())
                    {
                        Cv2.GaussianBlur(processed, gaussianResult,
                            new OpenCvSharp.Size(_noiseReductionConfig.GaussianKernelSize, _noiseReductionConfig.GaussianKernelSize),
                            _noiseReductionConfig.GaussianSigma);
                        gaussianResult.CopyTo(processed);
                    }
                }

                // Apply median filter for salt-and-pepper noise
                if (_noiseReductionConfig.MedianFilterSize > 1)
                {
                    using (var medianResult = new Mat())
                    {
                        Cv2.MedianBlur(processed, medianResult, _noiseReductionConfig.MedianFilterSize);
                        medianResult.CopyTo(processed);
                    }
                }

                // Apply bilateral filter for edge-preserving noise reduction
                if (_noiseReductionConfig.EnableBilateralFilter)
                {
                    using (var bilateralResult = new Mat())
                    {
                        Cv2.BilateralFilter(processed, bilateralResult, -1,
                            _noiseReductionConfig.BilateralSigmaColor,
                            _noiseReductionConfig.BilateralSigmaSpace);
                        bilateralResult.CopyTo(processed);
                    }
                }

                return processed;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to reduce noise: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Enhances image contrast and brightness for better detection.
        /// </summary>
        /// <param name="source">Source image to enhance.</param>
        /// <param name="alpha">Contrast multiplier (1.0 = no change).</param>
        /// <param name="beta">Brightness offset (0 = no change).</param>
        /// <returns>Enhanced image.</returns>
        public Mat EnhanceContrast(Mat source, double alpha = 1.5, int beta = 30)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            try
            {
                var enhanced = new Mat();
                source.ConvertTo(enhanced, MatType.CV_8UC3, alpha, beta);

                // Apply CLAHE (Contrast Limited Adaptive Histogram Equalization) for local enhancement
                if (source.Channels() == 1)
                {
                    using (var clahe = Cv2.CreateCLAHE(2.0, new OpenCvSharp.Size(8, 8)))
                    {
                        clahe.Apply(enhanced, enhanced);
                    }
                }
                else if (source.Channels() == 3)
                {
                    // Convert to LAB color space for better CLAHE application
                    using (var lab = new Mat())
                    using (var clahe = Cv2.CreateCLAHE(2.0, new OpenCvSharp.Size(8, 8)))
                    {
                        Cv2.CvtColor(enhanced, lab, ColorConversionCodes.BGR2Lab);
                        var labChannels = Cv2.Split(lab);

                        try
                        {
                            clahe.Apply(labChannels[0], labChannels[0]);
                            Cv2.Merge(labChannels, lab);
                            Cv2.CvtColor(lab, enhanced, ColorConversionCodes.Lab2BGR);
                        }
                        finally
                        {
                            foreach (var channel in labChannels)
                            {
                                channel?.Dispose();
                            }
                        }
                    }
                }

                return enhanced;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to enhance contrast: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Optimizes edge detection for UI element boundaries.
        /// </summary>
        /// <param name="source">Source image to process.</param>
        /// <returns>Processed image with enhanced edges.</returns>
        public Mat OptimizeEdgeDetection(Mat source)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            try
            {
                using (var gray = new Mat())
                using (var edges = new Mat())
                {
                    // Convert to grayscale if needed
                    if (source.Channels() == 3)
                    {
                        Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                    }
                    else
                    {
                        source.CopyTo(gray);
                    }

                    // Apply Canny edge detection
                    Cv2.Canny(gray, edges,
                        _edgeDetectionConfig.CannyThreshold1,
                        _edgeDetectionConfig.CannyThreshold2,
                        _edgeDetectionConfig.CannyApertureSize);

                    // Apply morphological operations to connect broken edges
                    if (_edgeDetectionConfig.EnableMorphologyOperations)
                    {
                        using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect,
                            new OpenCvSharp.Size(_edgeDetectionConfig.MorphologyKernelSize, _edgeDetectionConfig.MorphologyKernelSize)))
                        using (var morphResult = new Mat())
                        {
                            Cv2.MorphologyEx(edges, morphResult, MorphTypes.Close, kernel);
                            morphResult.CopyTo(edges);
                        }
                    }

                    // Convert back to 3-channel for consistency
                    var result = new Mat();
                    Cv2.CvtColor(edges, result, ColorConversionCodes.GRAY2BGR);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to optimize edge detection: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Applies image segmentation to isolate UI regions.
        /// </summary>
        /// <param name="source">Source image to segment.</param>
        /// <returns>Segmented image with isolated regions.</returns>
        public Mat PerformImageSegmentation(Mat source)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            try
            {
                using (var gray = new Mat())
                using (var binary = new Mat())
                {
                    // Convert to grayscale
                    if (source.Channels() == 3)
                    {
                        Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                    }
                    else
                    {
                        source.CopyTo(gray);
                    }

                    // Apply adaptive thresholding for better segmentation
                    Cv2.AdaptiveThreshold(gray, binary, 255,
                        AdaptiveThresholdTypes.GaussianC,
                        ThresholdTypes.Binary, 11, 2);

                    // Find contours for region identification
                    OpenCvSharp.Point[][] contours;
                    HierarchyIndex[] hierarchy;

                    Cv2.FindContours(binary, out contours, out hierarchy,
                        RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    // Create result image with contours
                    var result = new Mat();
                    source.CopyTo(result);

                    // Draw significant contours (filter by area)
                    for (int i = 0; i < contours.Length; i++)
                    {
                        var area = Cv2.ContourArea(contours[i]);
                        if (area > 100 && area < source.Width * source.Height * 0.5)
                        {
                            Cv2.DrawContours(result, contours, i, Scalar.Green, 2);
                        }
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to perform image segmentation: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Applies a comprehensive preprocessing pipeline optimized for UI element detection.
        /// </summary>
        /// <param name="source">Source image to preprocess.</param>
        /// <param name="enableNoiseReduction">Whether to apply noise reduction.</param>
        /// <param name="enableContrastEnhancement">Whether to enhance contrast.</param>
        /// <param name="enableEdgeOptimization">Whether to optimize edges.</param>
        /// <returns>Preprocessed image ready for detection.</returns>
        public Mat PreprocessForDetection(Mat source,
            bool enableNoiseReduction = true,
            bool enableContrastEnhancement = true,
            bool enableEdgeOptimization = false)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            try
            {
                var processed = new Mat();
                source.CopyTo(processed);

                // Step 1: Noise reduction
                if (enableNoiseReduction)
                {
                    using (var noiseReduced = ReduceNoise(processed))
                    {
                        noiseReduced.CopyTo(processed);
                    }
                }

                // Step 2: Contrast enhancement
                if (enableContrastEnhancement)
                {
                    using (var enhanced = EnhanceContrast(processed))
                    {
                        enhanced.CopyTo(processed);
                    }
                }

                // Step 3: Edge optimization (optional for detection)
                if (enableEdgeOptimization)
                {
                    using (var edgeOptimized = OptimizeEdgeDetection(processed))
                    {
                        edgeOptimized.CopyTo(processed);
                    }
                }

                return processed;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to preprocess image for detection: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disposes resources used by the AdvancedImageProcessor.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}