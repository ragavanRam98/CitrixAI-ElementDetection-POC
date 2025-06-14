using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace CitrixAI.Vision.OpenCV
{
    /// <summary>
    /// Provides image processing capabilities using OpenCV.
    /// Implements common image processing operations for element detection.
    /// </summary>
    public sealed class ImageProcessor : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Converts a System.Drawing.Bitmap to OpenCV Mat.
        /// </summary>
        /// <param name="bitmap">The bitmap to convert.</param>
        /// <returns>OpenCV Mat representation of the bitmap.</returns>
        public Mat BitmapToMat(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            try
            {
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Bmp);
                    var buffer = stream.ToArray();
                    return Mat.FromImageData(buffer, ImreadModes.Color);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert bitmap to Mat: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts an OpenCV Mat to System.Drawing.Bitmap.
        /// </summary>
        /// <param name="mat">The Mat to convert.</param
        /// <returns>Bitmap representation of the Mat.</returns>
        public Bitmap MatToBitmap(Mat mat)
        {
            if (mat == null || mat.IsDisposed)
                throw new ArgumentNullException(nameof(mat));

            try
            {
                var buffer = mat.ToBytes();
                using (var stream = new MemoryStream(buffer))
                {
                    return new Bitmap(stream);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert Mat to bitmap: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Enhances image quality for better element detection.
        /// </summary>
        /// <param name="source">The source image to enhance.</param>
        /// <returns>Enhanced image.</returns>
        public Mat EnhanceImageQuality(Mat source)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            var enhanced = source.Clone();

            try
            {
                // Convert to grayscale for processing
                using (var gray = new Mat())
                {
                    Cv2.CvtColor(enhanced, gray, ColorConversionCodes.BGR2GRAY);

                    // Apply CLAHE (Contrast Limited Adaptive Histogram Equalization)
                    using (var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8)))
                    using (var enhanced_gray = new Mat())
                    {
                        clahe.Apply(gray, enhanced_gray);

                        // Convert back to color
                        Cv2.CvtColor(enhanced_gray, enhanced, ColorConversionCodes.GRAY2BGR);
                    }

                    // Apply Gaussian blur to reduce noise
                    using (var blurred = new Mat())
                    {
                        Cv2.GaussianBlur(enhanced, blurred, new OpenCvSharp.Size(3, 3), 0);
                        blurred.CopyTo(enhanced);
                    }

                    // Sharpen the image
                    using (var kernel = Mat.FromArray(new float[,] {
               { 0, -1, 0 },
               { -1, 5, -1 },
               { 0, -1, 0 }
           }))
                    using (var sharpened = new Mat())
                    {
                        Cv2.Filter2D(enhanced, sharpened, -1, kernel);
                        sharpened.CopyTo(enhanced);
                    }
                }

                return enhanced;
            }
            catch (Exception ex)
            {
                enhanced?.Dispose();
                throw new InvalidOperationException($"Failed to enhance image quality: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Preprocesses image for OCR to improve text recognition accuracy.
        /// </summary>
        /// <param name="source">The source image to preprocess.</param>
        /// <returns>Preprocessed image optimized for OCR.</returns>
        public Mat PreprocessForOCR(Mat source)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            Mat processed = null;

            try
            {
                // Convert to grayscale
                if (source.Channels() > 1)
                {
                    processed = new Mat();
                    Cv2.CvtColor(source, processed, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    processed = source.Clone();
                }

                // Apply median blur to reduce noise
                using (var blurred = new Mat())
                {
                    Cv2.MedianBlur(processed, blurred, 3);
                    blurred.CopyTo(processed);
                }

                // Apply adaptive threshold for better text contrast
                using (var binary = new Mat())
                {
                    Cv2.AdaptiveThreshold(processed, binary, 255, AdaptiveThresholdTypes.GaussianC,
                        ThresholdTypes.Binary, 11, 2);

                    // Morphological operations to clean up text
                    using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2)))
                    using (var morphed = new Mat())
                    {
                        Cv2.MorphologyEx(binary, morphed, MorphTypes.Close, kernel);
                        morphed.CopyTo(processed);
                    }
                }

                // Scale image if too small (OCR works better on larger text)
                if (processed.Width < 300 || processed.Height < 200)
                {
                    var scaleFactor = Math.Max(300.0 / processed.Width, 200.0 / processed.Height);
                    var newSize = new OpenCvSharp.Size((int)(processed.Width * scaleFactor),
                        (int)(processed.Height * scaleFactor));

                    using (var scaled = new Mat())
                    {
                        Cv2.Resize(processed, scaled, newSize, interpolation: InterpolationFlags.Cubic);
                        var temp = scaled.Clone();
                        processed.Dispose();
                        processed = temp;
                    }
                }

                return processed;
            }
            catch (Exception ex)
            {
                processed?.Dispose();
                throw new InvalidOperationException($"Failed to preprocess image for OCR: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Detects edges in the image using Canny edge detection.
        /// </summary>
        /// <param name="source">The source image.</param>
        /// <param name="lowThreshold">Lower threshold for edge detection.</param>
        /// <param name="highThreshold">Higher threshold for edge detection.</param>
        /// <returns>Edge-detected image.</returns>
        public Mat DetectEdges(Mat source, double lowThreshold = 50, double highThreshold = 150)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            var edges = new Mat();

            try
            {
                // Convert to grayscale if needed
                using (var gray = source.Channels() > 1 ? new Mat() : source.Clone())
                {
                    if (source.Channels() > 1)
                    {
                        Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                    }

                    // Apply Gaussian blur to reduce noise
                    using (var blurred = new Mat())
                    {
                        Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 0);

                        // Apply Canny edge detection
                        Cv2.Canny(blurred, edges, lowThreshold, highThreshold);
                    }
                }

                return edges;
            }
            catch (Exception ex)
            {
                edges?.Dispose();
                throw new InvalidOperationException($"Failed to detect edges: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Finds contours in the image.
        /// </summary>
        /// <param name="source">The source image (should be binary).</param>
        /// <param name="mode">Contour retrieval mode.</param>
        /// <param name="method">Contour approximation method.</param>
        /// <returns>Array of detected contours.</returns>
        public OpenCvSharp.Point[][] FindContours(Mat source, RetrievalModes mode = RetrievalModes.External,
            ContourApproximationModes method = ContourApproximationModes.ApproxSimple)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            try
            {
                Cv2.FindContours(source, out var contours, out var hierarchy, mode, method);
                return contours;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to find contours: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calculates the quality score of an image based on various factors.
        /// </summary>
        /// <param name="source">The source image to analyze.</param>
        /// <returns>Quality score between 0.0 and 1.0.</returns>
        public double CalculateImageQuality(Mat source)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            try
            {
                double qualityScore = 0.0;
                int factors = 0;

                // Factor 1: Contrast (using standard deviation)
                Mat gray;
                if (source.Channels() > 1)
                {
                    gray = new Mat();
                    Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    gray = source.Clone();
                }

                using (gray)
                {
                    Cv2.MeanStdDev(gray, out var mean, out var stddev);
                    var contrastScore = Math.Min(1.0, stddev.Val0 / 64.0); // Normalize by expected max stddev
                    qualityScore += contrastScore;
                    factors++;

                    // Factor 2: Sharpness (using Laplacian variance)
                    using (var laplacian = new Mat())
                    {
                        Cv2.Laplacian(gray, laplacian, MatType.CV_64F);
                        Cv2.MeanStdDev(laplacian, out var lapMean, out var lapStddev);
                        var sharpnessScore = Math.Min(1.0, lapStddev.Val0 / 1000.0); // Normalize
                        qualityScore += sharpnessScore;
                        factors++;
                    }

                    // Factor 3: Brightness (should be in reasonable range)
                    var brightnessScore = 1.0 - Math.Abs(mean.Val0 - 128.0) / 128.0; // Optimal brightness around 128
                    qualityScore += brightnessScore;
                    factors++;
                }

                // Factor 4: Resolution adequacy
                var resolutionScore = Math.Min(1.0, (source.Width * source.Height) / (800.0 * 600.0));
                qualityScore += resolutionScore;
                factors++;

                return factors > 0 ? qualityScore / factors : 0.0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to calculate image quality: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extracts a region of interest from the image.
        /// </summary>
        /// <param name="source">The source image.</param>
        /// <param name="roi">The region of interest rectangle.</param>
        /// <returns>Extracted region as a new Mat.</returns>
        public Mat ExtractRegion(Mat source, Rect roi)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            if (roi.X < 0 || roi.Y < 0 || roi.X + roi.Width > source.Width || roi.Y + roi.Height > source.Height)
                throw new ArgumentOutOfRangeException(nameof(roi), "ROI is outside image bounds.");

            try
            {
                return new Mat(source, roi);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract region: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Resizes an image while maintaining aspect ratio.
        /// </summary>
        /// <param name="source">The source image.</param>
        /// <param name="maxWidth">Maximum width.</param>
        /// <param name="maxHeight">Maximum height.</param>
        /// <returns>Resized image.</returns>
        public Mat ResizeProportional(Mat source, int maxWidth, int maxHeight)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            if (maxWidth <= 0 || maxHeight <= 0)
                throw new ArgumentException("Maximum dimensions must be positive.");

            try
            {
                var aspectRatio = (double)source.Width / source.Height;
                var targetWidth = maxWidth;
                var targetHeight = (int)(maxWidth / aspectRatio);

                if (targetHeight > maxHeight)
                {
                    targetHeight = maxHeight;
                    targetWidth = (int)(maxHeight * aspectRatio);
                }

                var resized = new Mat();
                Cv2.Resize(source, resized, new OpenCvSharp.Size(targetWidth, targetHeight),
                    interpolation: InterpolationFlags.Cubic);

                return resized;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to resize image: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Releases resources used by the ImageProcessor.
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