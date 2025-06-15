using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace CitrixAI.Vision.Processing
{
    /// <summary>
    /// Analyzes image quality and provides preprocessing recommendations.
    /// Evaluates various quality metrics to optimize detection performance.
    /// </summary>
    public sealed class ImageQualityAnalyzer : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Comprehensive image quality assessment result.
        /// </summary>
        public class QualityAssessment
        {
            /// <summary>
            /// Overall quality score from 0.0 (poor) to 1.0 (excellent).
            /// </summary>
            public double OverallScore { get; set; }

            /// <summary>
            /// Brightness level from 0.0 (dark) to 1.0 (bright).
            /// </summary>
            public double Brightness { get; set; }

            /// <summary>
            /// Contrast level from 0.0 (low) to 1.0 (high).
            /// </summary>
            public double Contrast { get; set; }

            /// <summary>
            /// Sharpness level from 0.0 (blurry) to 1.0 (sharp).
            /// </summary>
            public double Sharpness { get; set; }

            /// <summary>
            /// Noise level from 0.0 (clean) to 1.0 (noisy).
            /// </summary>
            public double NoiseLevel { get; set; }

            /// <summary>
            /// Edge density indicating detail richness.
            /// </summary>
            public double EdgeDensity { get; set; }

            /// <summary>
            /// Color distribution analysis results.
            /// </summary>
            public ColorDistribution ColorAnalysis { get; set; }

            /// <summary>
            /// Recommended preprocessing operations.
            /// </summary>
            public List<PreprocessingRecommendation> Recommendations { get; set; }

            public QualityAssessment()
            {
                Recommendations = new List<PreprocessingRecommendation>();
                ColorAnalysis = new ColorDistribution();
            }
        }

        /// <summary>
        /// Color distribution analysis for image assessment.
        /// </summary>
        public class ColorDistribution
        {
            /// <summary>
            /// Dominant color channels (BGR values).
            /// </summary>
            public (double Blue, double Green, double Red) DominantColor { get; set; }

            /// <summary>
            /// Color variance indicating diversity.
            /// </summary>
            public double ColorVariance { get; set; }

            /// <summary>
            /// Histogram entropy indicating color distribution richness.
            /// </summary>
            public double HistogramEntropy { get; set; }

            /// <summary>
            /// Whether the image is predominantly grayscale.
            /// </summary>
            public bool IsGrayscale { get; set; }
        }

        /// <summary>
        /// Preprocessing operation recommendation.
        /// </summary>
        public class PreprocessingRecommendation
        {
            /// <summary>
            /// Type of preprocessing operation.
            /// </summary>
            public PreprocessingType Operation { get; set; }

            /// <summary>
            /// Confidence in the recommendation (0.0 to 1.0).
            /// </summary>
            public double Confidence { get; set; }

            /// <summary>
            /// Detailed parameters for the operation.
            /// </summary>
            public Dictionary<string, object> Parameters { get; set; }

            /// <summary>
            /// Reason for the recommendation.
            /// </summary>
            public string Reason { get; set; }

            public PreprocessingRecommendation()
            {
                Parameters = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Types of preprocessing operations that can be recommended.
        /// </summary>
        public enum PreprocessingType
        {
            ContrastEnhancement,
            BrightnessAdjustment,
            NoiseReduction,
            Sharpening,
            ColorCorrection,
            EdgeEnhancement,
            AdaptiveThresholding,
            HistogramEqualization
        }

        /// <summary>
        /// Performs comprehensive quality assessment of an image.
        /// </summary>
        /// <param name="source">Image to analyze.</param>
        /// <returns>Detailed quality assessment with recommendations.</returns>
        public QualityAssessment AnalyzeQuality(Mat source)
        {
            if (source == null || source.IsDisposed)
                throw new ArgumentNullException(nameof(source));

            try
            {
                var assessment = new QualityAssessment();

                // Analyze individual quality metrics
                assessment.Brightness = CalculateBrightness(source);
                assessment.Contrast = CalculateContrast(source);
                assessment.Sharpness = CalculateSharpness(source);
                assessment.NoiseLevel = EstimateNoiseLevel(source);
                assessment.EdgeDensity = CalculateEdgeDensity(source);
                assessment.ColorAnalysis = AnalyzeColorDistribution(source);

                // Calculate overall quality score
                assessment.OverallScore = CalculateOverallScore(assessment);

                // Generate preprocessing recommendations
                assessment.Recommendations = GenerateRecommendations(assessment);

                return assessment;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to analyze image quality: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calculates the average brightness of an image.
        /// </summary>
        /// <param name="source">Source image.</param>
        /// <returns>Brightness value from 0.0 to 1.0.</returns>
        private double CalculateBrightness(Mat source)
        {
            using (var gray = new Mat())
            {
                if (source.Channels() == 3)
                {
                    Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    source.CopyTo(gray);
                }

                var mean = Cv2.Mean(gray);
                return mean.Val0 / 255.0;
            }
        }

        /// <summary>
        /// Calculates the contrast level of an image using standard deviation.
        /// </summary>
        /// <param name="source">Source image.</param>
        /// <returns>Contrast value from 0.0 to 1.0.</returns>
        private double CalculateContrast(Mat source)
        {
            using (var gray = new Mat())
            {
                if (source.Channels() == 3)
                {
                    Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    source.CopyTo(gray);
                }

                var mean = new Scalar();
                var stddev = new Scalar();
                Cv2.MeanStdDev(gray, out mean, out stddev);

                // Normalize standard deviation to 0-1 range
                return Math.Min(1.0, stddev.Val0 / 128.0);
            }
        }

        /// <summary>
        /// Calculates image sharpness using Laplacian variance.
        /// </summary>
        /// <param name="source">Source image.</param>
        /// <returns>Sharpness value from 0.0 to 1.0.</returns>
        private double CalculateSharpness(Mat source)
        {
            using (var gray = new Mat())
            using (var laplacian = new Mat())
            {
                if (source.Channels() == 3)
                {
                    Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    source.CopyTo(gray);
                }

                Cv2.Laplacian(gray, laplacian, MatType.CV_64F);

                var mean = new Scalar();
                var stddev = new Scalar();
                Cv2.MeanStdDev(laplacian, out mean, out stddev);

                // Normalize Laplacian variance to approximate sharpness
                var variance = stddev.Val0 * stddev.Val0;
                return Math.Min(1.0, variance / 10000.0);
            }
        }

        /// <summary>
        /// Estimates noise level in the image using high-frequency analysis.
        /// </summary>
        /// <param name="source">Source image.</param>
        /// <returns>Noise level from 0.0 to 1.0.</returns>
        private double EstimateNoiseLevel(Mat source)
        {
            using (var gray = new Mat())
            using (var gaussian = new Mat())
            using (var diff = new Mat())
            {
                if (source.Channels() == 3)
                {
                    Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    source.CopyTo(gray);
                }

                // Apply Gaussian blur and calculate difference
                Cv2.GaussianBlur(gray, gaussian, new OpenCvSharp.Size(5, 5), 1.0);
                Cv2.Absdiff(gray, gaussian, diff);

                var mean = Cv2.Mean(diff);
                // Normalize noise estimation
                return Math.Min(1.0, mean.Val0 / 50.0);
            }
        }

        /// <summary>
        /// Calculates edge density as an indicator of detail richness.
        /// </summary>
        /// <param name="source">Source image.</param>
        /// <returns>Edge density value from 0.0 to 1.0.</returns>
        private double CalculateEdgeDensity(Mat source)
        {
            using (var gray = new Mat())
            using (var edges = new Mat())
            {
                if (source.Channels() == 3)
                {
                    Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    source.CopyTo(gray);
                }

                Cv2.Canny(gray, edges, 50, 150);

                var totalPixels = source.Width * source.Height;
                var edgePixels = Cv2.CountNonZero(edges);

                return (double)edgePixels / totalPixels;
            }
        }

        /// <summary>
        /// Analyzes color distribution and characteristics.
        /// </summary>
        /// <param name="source">Source image.</param>
        /// <returns>Color distribution analysis.</returns>
        private ColorDistribution AnalyzeColorDistribution(Mat source)
        {
            var analysis = new ColorDistribution();

            try
            {
                if (source.Channels() == 1)
                {
                    analysis.IsGrayscale = true;
                    analysis.DominantColor = (0, 0, 0);
                    analysis.ColorVariance = 0.0;
                    analysis.HistogramEntropy = CalculateGrayscaleEntropy(source);
                }
                else
                {
                    // Calculate mean color values
                    var meanColor = Cv2.Mean(source);
                    analysis.DominantColor = (meanColor.Val0, meanColor.Val1, meanColor.Val2);

                    // Check if image is effectively grayscale
                    var colorDiff = Math.Max(
                        Math.Abs(meanColor.Val0 - meanColor.Val1),
                        Math.Max(
                            Math.Abs(meanColor.Val1 - meanColor.Val2),
                            Math.Abs(meanColor.Val0 - meanColor.Val2)));

                    analysis.IsGrayscale = colorDiff < 10.0;

                    // Calculate color variance
                    analysis.ColorVariance = CalculateColorVariance(source);

                    // Calculate histogram entropy
                    analysis.HistogramEntropy = CalculateColorHistogramEntropy(source);
                }

                return analysis;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to analyze color distribution: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calculates color variance across channels.
        /// </summary>
        /// <param name="source">Source image.</param>
        /// <returns>Color variance value.</returns>
        private double CalculateColorVariance(Mat source)
        {
            var channels = Cv2.Split(source);
            double totalVariance = 0.0;

            try
            {
                foreach (var channel in channels)
                {
                    var mean = new Scalar();
                    var stddev = new Scalar();
                    Cv2.MeanStdDev(channel, out mean, out stddev);
                    totalVariance += stddev.Val0 * stddev.Val0;
                }

                return totalVariance / channels.Length;
            }
            finally
            {
                foreach (var channel in channels)
                {
                    channel?.Dispose();
                }
            }
        }

        /// <summary>
        /// Calculates histogram entropy for grayscale images.
        /// </summary>
        /// <param name="source">Source grayscale image.</param>
        /// <returns>Histogram entropy value.</returns>
        private double CalculateGrayscaleEntropy(Mat source)
        {
            using (var hist = new Mat())
            {
                var histSize = new int[] { 256 };
                var ranges = new Rangef[] { new Rangef(0, 256) };
                var channels = new int[] { 0 };

                Cv2.CalcHist(new Mat[] { source }, channels, null, hist, 1, histSize, ranges);

                var totalPixels = source.Width * source.Height;
                double entropy = 0.0;

                for (int i = 0; i < 256; i++)
                {
                    var binValue = hist.At<float>(i);
                    if (binValue > 0)
                    {
                        var probability = binValue / totalPixels;
                        entropy -= probability * (Math.Log(probability) / Math.Log(2.0));
                    }
                }

                return entropy / 8.0; // Normalize to 0-1 range
            }
        }

        /// <summary>
        /// Calculates histogram entropy for color images.
        /// </summary>
        /// <param name="source">Source color image.</param>
        /// <returns>Average histogram entropy across color channels.</returns>
        private double CalculateColorHistogramEntropy(Mat source)
        {
            var channels = Cv2.Split(source);
            double totalEntropy = 0.0;

            try
            {
                foreach (var channel in channels)
                {
                    totalEntropy += CalculateGrayscaleEntropy(channel);
                }

                return totalEntropy / channels.Length;
            }
            finally
            {
                foreach (var channel in channels)
                {
                    channel?.Dispose();
                }
            }
        }

        /// <summary>
        /// Calculates overall quality score based on individual metrics.
        /// </summary>
        /// <param name="assessment">Quality assessment with individual metrics.</param>
        /// <returns>Overall quality score from 0.0 to 1.0.</returns>
        private double CalculateOverallScore(QualityAssessment assessment)
        {
            // Weighted combination of quality factors
            var brightnessScore = CalculateBrightnessScore(assessment.Brightness);
            var contrastScore = assessment.Contrast;
            var sharpnessScore = assessment.Sharpness;
            var noiseScore = 1.0 - assessment.NoiseLevel; // Invert noise (less noise = better)
            var edgeScore = Math.Min(1.0, assessment.EdgeDensity * 2.0); // Cap edge density influence

            // Weighted average (can be tuned based on UI detection requirements)
            var weights = new double[] { 0.2, 0.25, 0.25, 0.2, 0.1 };
            var scores = new double[] { brightnessScore, contrastScore, sharpnessScore, noiseScore, edgeScore };

            double weightedSum = 0.0;
            for (int i = 0; i < weights.Length; i++)
            {
                weightedSum += weights[i] * scores[i];
            }

            return Math.Max(0.0, Math.Min(1.0, weightedSum));
        }

        /// <summary>
        /// Calculates brightness quality score (optimal around 0.4-0.6).
        /// </summary>
        /// <param name="brightness">Brightness value.</param>
        /// <returns>Brightness quality score.</returns>
        private double CalculateBrightnessScore(double brightness)
        {
            // Optimal brightness range for UI detection
            const double optimalMin = 0.3;
            const double optimalMax = 0.7;

            if (brightness >= optimalMin && brightness <= optimalMax)
            {
                return 1.0;
            }
            else if (brightness < optimalMin)
            {
                return brightness / optimalMin;
            }
            else
            {
                return (1.0 - brightness) / (1.0 - optimalMax);
            }
        }

        /// <summary>
        /// Generates preprocessing recommendations based on quality assessment.
        /// </summary>
        /// <param name="assessment">Quality assessment results.</param>
        /// <returns>List of recommended preprocessing operations.</returns>
        private List<PreprocessingRecommendation> GenerateRecommendations(QualityAssessment assessment)
        {
            var recommendations = new List<PreprocessingRecommendation>();

            // Brightness adjustment recommendations
            if (assessment.Brightness < 0.3)
            {
                recommendations.Add(new PreprocessingRecommendation
                {
                    Operation = PreprocessingType.BrightnessAdjustment,
                    Confidence = 0.9,
                    Reason = "Image appears too dark for optimal UI element detection",
                    Parameters = new Dictionary<string, object>
                    {
                        ["BrightnessIncrease"] = (int)((0.4 - assessment.Brightness) * 255)
                    }
                });
            }
            else if (assessment.Brightness > 0.8)
            {
                recommendations.Add(new PreprocessingRecommendation
                {
                    Operation = PreprocessingType.BrightnessAdjustment,
                    Confidence = 0.8,
                    Reason = "Image appears overexposed, reducing brightness may help",
                    Parameters = new Dictionary<string, object>
                    {
                        ["BrightnessDecrease"] = (int)((assessment.Brightness - 0.6) * 255)
                    }
                });
            }

            // Contrast enhancement recommendations
            if (assessment.Contrast < 0.3)
            {
                recommendations.Add(new PreprocessingRecommendation
                {
                    Operation = PreprocessingType.ContrastEnhancement,
                    Confidence = 0.85,
                    Reason = "Low contrast detected, enhancement will improve element boundaries",
                    Parameters = new Dictionary<string, object>
                    {
                        ["ContrastMultiplier"] = 1.5 + (0.3 - assessment.Contrast)
                    }
                });
            }

            // Noise reduction recommendations
            if (assessment.NoiseLevel > 0.4)
            {
                recommendations.Add(new PreprocessingRecommendation
                {
                    Operation = PreprocessingType.NoiseReduction,
                    Confidence = 0.8,
                    Reason = "High noise level detected, reduction will improve detection accuracy",
                    Parameters = new Dictionary<string, object>
                    {
                        ["NoiseLevel"] = assessment.NoiseLevel,
                        ["FilterStrength"] = Math.Min(5, (int)(assessment.NoiseLevel * 10))
                    }
                });
            }

            // Sharpening recommendations
            if (assessment.Sharpness < 0.4)
            {
                recommendations.Add(new PreprocessingRecommendation
                {
                    Operation = PreprocessingType.Sharpening,
                    Confidence = 0.7,
                    Reason = "Image appears blurry, sharpening may improve edge detection",
                    Parameters = new Dictionary<string, object>
                    {
                        ["SharpeningStrength"] = (0.5 - assessment.Sharpness) * 2.0
                    }
                });
            }

            // Edge enhancement for low edge density
            if (assessment.EdgeDensity < 0.1)
            {
                recommendations.Add(new PreprocessingRecommendation
                {
                    Operation = PreprocessingType.EdgeEnhancement,
                    Confidence = 0.6,
                    Reason = "Low edge density detected, enhancement may reveal UI element boundaries",
                    Parameters = new Dictionary<string, object>
                    {
                        ["EdgeThreshold"] = 30.0,
                        ["EnhancementFactor"] = 1.5
                    }
                });
            }

            // Histogram equalization for poor overall contrast
            if (assessment.Contrast < 0.2 && assessment.ColorAnalysis.HistogramEntropy < 0.5)
            {
                recommendations.Add(new PreprocessingRecommendation
                {
                    Operation = PreprocessingType.HistogramEqualization,
                    Confidence = 0.7,
                    Reason = "Poor tonal distribution detected, histogram equalization may improve visibility",
                    Parameters = new Dictionary<string, object>
                    {
                        ["AdaptiveEqualization"] = true,
                        ["ClipLimit"] = 2.0
                    }
                });
            }

            // Sort recommendations by confidence
            recommendations.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));

            return recommendations;
        }

        /// <summary>
        /// Determines if an image requires preprocessing based on quality thresholds.
        /// </summary>
        /// <param name="assessment">Quality assessment results.</param>
        /// <returns>True if preprocessing is recommended.</returns>
        public bool RequiresPreprocessing(QualityAssessment assessment)
        {
            if (assessment == null)
                throw new ArgumentNullException(nameof(assessment));

            // Consider preprocessing if overall quality is below threshold
            // or if any critical metric is problematic
            return assessment.OverallScore < 0.6 ||
                   assessment.Brightness < 0.2 || assessment.Brightness > 0.9 ||
                   assessment.Contrast < 0.25 ||
                   assessment.NoiseLevel > 0.5 ||
                   assessment.Sharpness < 0.3;
        }

        /// <summary>
        /// Gets a quality category description for user feedback.
        /// </summary>
        /// <param name="overallScore">Overall quality score.</param>
        /// <returns>Human-readable quality description.</returns>
        public string GetQualityDescription(double overallScore)
        {
            if (overallScore >= 0.8)
                return "Excellent - Optimal for detection";
            else if (overallScore >= 0.6)
                return "Good - Suitable for detection";
            else if (overallScore >= 0.4)
                return "Fair - May benefit from preprocessing";
            else if (overallScore >= 0.2)
                return "Poor - Preprocessing recommended";
            else
                return "Very Poor - Significant preprocessing required";
        }

        /// <summary>
        /// Disposes resources used by the ImageQualityAnalyzer.
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