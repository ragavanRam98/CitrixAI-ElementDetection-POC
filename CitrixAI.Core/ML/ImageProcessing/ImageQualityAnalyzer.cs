using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace CitrixAI.Core.ML.ImageProcessing
{
    /// <summary>
    /// Analyzes image quality using multiple computer vision metrics.
    /// Provides comprehensive quality assessment for training data curation.
    /// </summary>
    public class ImageQualityAnalyzer : IDisposable
    {
        private bool _disposed = false;

        /// <summary>
        /// Analyzes the quality of an image using multiple metrics.
        /// Returns comprehensive quality assessment with recommendations.
        /// </summary>
        public async Task<QualityAnalysisResult> AnalyzeQualityAsync(Bitmap image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            return await Task.Run(() =>
            {
                using (var mat = BitmapConverter.ToMat(image))
                using (var grayMat = new Mat())
                {
                    Cv2.CvtColor(mat, grayMat, ColorConversionCodes.BGR2GRAY);

                    var result = new QualityAnalysisResult
                    {
                        ImageSize = new System.Drawing.Size(image.Width, image.Height),
                        AnalyzedAt = DateTime.UtcNow
                    };

                    // Calculate individual quality metrics
                    result.SharpnessScore = CalculateSharpness(grayMat);
                    result.ContrastScore = CalculateContrast(grayMat);
                    result.BrightnessScore = CalculateBrightness(grayMat);
                    result.NoiseLevel = CalculateNoiseLevel(grayMat);

                    // Calculate overall quality score
                    result.OverallScore = CalculateOverallScore(result);

                    // Generate improvement recommendations
                    result.Recommendations = GenerateRecommendations(result);

                    return result;
                }
            });
        }

        /// <summary>
        /// Legacy method for backward compatibility with existing tests.
        /// Returns simple quality score without detailed analysis.
        /// </summary>
        public async Task<double> AnalyzeQuality(Bitmap image)
        {
            var result = await AnalyzeQualityAsync(image);
            return result.OverallScore;
        }

        /// <summary>
        /// Legacy method for backward compatibility with existing tests.
        /// Returns human-readable quality description.
        /// </summary>
        public string GetQualityDescription(double qualityScore)
        {
            if (qualityScore >= 0.8)
                return "Excellent";
            else if (qualityScore >= 0.6)
                return "Good";
            else if (qualityScore >= 0.4)
                return "Fair";
            else
                return "Poor";
        }

        /// <summary>
        /// Legacy method for backward compatibility with existing tests.
        /// Determines if image requires preprocessing based on quality score.
        /// </summary>
        public bool RequiresPreprocessing(double qualityScore)
        {
            return qualityScore < 0.6;
        }

        /// <summary>
        /// Calculates image sharpness using Laplacian variance.
        /// Higher values indicate sharper images suitable for training.
        /// </summary>
        private double CalculateSharpness(Mat grayImage)
        {
            using (var laplacian = new Mat())
            {
                Cv2.Laplacian(grayImage, laplacian, MatType.CV_64F);

                Cv2.MeanStdDev(laplacian, out var mean, out var stddev);
                var variance = stddev.Val0 * stddev.Val0;

                // Normalize to 0-1 range (typical range 0-3000, good images > 100)
                return Math.Min(1.0, variance / 1000.0);
            }
        }

        /// <summary>
        /// Calculates image contrast using standard deviation of pixel intensities.
        /// Higher contrast improves feature detection accuracy.
        /// </summary>
        private double CalculateContrast(Mat grayImage)
        {
            Cv2.MeanStdDev(grayImage, out var mean, out var stddev);

            // Normalize standard deviation to 0-1 range
            // Good contrast typically has stddev > 30
            return Math.Min(1.0, stddev.Val0 / 80.0);
        }

        /// <summary>
        /// Evaluates brightness distribution to avoid over/under-exposed images.
        /// Optimal brightness improves element detection reliability.
        /// </summary>
        private double CalculateBrightness(Mat grayImage)
        {
            Cv2.MeanStdDev(grayImage, out var mean, out var stddev);

            var avgBrightness = mean.Val0;

            // Optimal brightness is around 100-160 for 8-bit images
            var optimalRange = Math.Abs(avgBrightness - 130);
            var score = Math.Max(0, 1.0 - (optimalRange / 100.0));

            return Math.Min(1.0, Math.Max(0.0, score));
        }

        /// <summary>
        /// Estimates noise level using high-frequency component analysis.
        /// Lower noise levels improve training data quality.
        /// </summary>
        private double CalculateNoiseLevel(Mat grayImage)
        {
            using (var blurred = new Mat())
            using (var difference = new Mat())
            {
                // Apply Gaussian blur and calculate difference
                Cv2.GaussianBlur(grayImage, blurred, new OpenCvSharp.Size(5, 5), 0);
                Cv2.Subtract(grayImage, blurred, difference);

                Cv2.MeanStdDev(difference, out var mean, out var stddev);

                // Lower noise = higher score (inverted)
                var noiseLevel = stddev.Val0 / 50.0; // Normalize
                return Math.Max(0.0, 1.0 - Math.Min(1.0, noiseLevel));
            }
        }

        /// <summary>
        /// Combines individual metrics into overall quality score.
        /// Uses weighted average optimized for UI element detection.
        /// </summary>
        private double CalculateOverallScore(QualityAnalysisResult result)
        {
            // Weights optimized for UI element detection
            const double sharpnessWeight = 0.35;
            const double contrastWeight = 0.30;
            const double brightnessWeight = 0.20;
            const double noiseWeight = 0.15;

            var weightedScore =
                (result.SharpnessScore * sharpnessWeight) +
                (result.ContrastScore * contrastWeight) +
                (result.BrightnessScore * brightnessWeight) +
                (result.NoiseLevel * noiseWeight);

            return Math.Min(1.0, Math.Max(0.0, weightedScore));
        }

        /// <summary>
        /// Generates actionable recommendations based on quality analysis.
        /// Provides specific suggestions for image improvement.
        /// </summary>
        private List<string> GenerateRecommendations(QualityAnalysisResult result)
        {
            var recommendations = new List<string>();

            if (result.SharpnessScore < 0.3)
            {
                recommendations.Add("Image appears blurry. Consider using higher resolution or better focus.");
            }

            if (result.ContrastScore < 0.4)
            {
                recommendations.Add("Low contrast detected. Consider enhancing contrast or improving lighting.");
            }

            if (result.BrightnessScore < 0.5)
            {
                recommendations.Add("Brightness may be suboptimal. Adjust exposure or lighting conditions.");
            }

            if (result.NoiseLevel < 0.6)
            {
                recommendations.Add("High noise level detected. Consider noise reduction preprocessing.");
            }

            if (result.OverallScore < 0.5)
            {
                recommendations.Add("Overall quality is below recommended threshold for training data.");
            }

            if (recommendations.Count == 0)
            {
                recommendations.Add("Image quality is suitable for training purposes.");
            }

            return recommendations;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Comprehensive result of image quality analysis.
    /// Contains individual metrics and overall assessment.
    /// </summary>
    public class QualityAnalysisResult
    {
        public double OverallScore { get; set; }
        public double SharpnessScore { get; set; }
        public double ContrastScore { get; set; }
        public double BrightnessScore { get; set; }
        public double NoiseLevel { get; set; }
        public System.Drawing.Size ImageSize { get; set; }
        public DateTime AnalyzedAt { get; set; }
        public List<string> Recommendations { get; set; }

        public QualityAnalysisResult()
        {
            Recommendations = new List<string>();
        }
    }
}