using CitrixAI.Vision.Processing;
using CitrixAI.Core.ML.ImageProcessing;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace CitrixAI.Vision.Tests
{
    /// <summary>
    /// Integration test to verify Segment 1 implementation works correctly.
    /// Tests AdvancedImageProcessor (Vision) and ImageQualityAnalyzer (Core) functionality.
    /// </summary>
    public static class Segment1IntegrationTest
    {
        /// <summary>
        /// Runs comprehensive tests for Segment 1 advanced image processing.
        /// </summary>
        /// <param name="logger">Optional callback for logging test progress and results.</param>
        /// <returns>True if all tests pass, false if any failures detected.</returns>
        public static async Task<bool> RunTestsAsync(Action<string> logger = null)
        {
            logger?.Invoke("Starting Segment 1 Integration Tests...");

            try
            {
                // Test 1: AdvancedImageProcessor instantiation
                if (!await TestAdvancedImageProcessorCreationAsync(logger))
                {
                    logger?.Invoke("❌ AdvancedImageProcessor creation test failed");
                    return false;
                }
                logger?.Invoke("✅ AdvancedImageProcessor creation test passed");

                // Test 2: ImageQualityAnalyzer instantiation
                if (!await TestImageQualityAnalyzerCreationAsync(logger))
                {
                    logger?.Invoke("❌ ImageQualityAnalyzer creation test failed");
                    return false;
                }
                logger?.Invoke("✅ ImageQualityAnalyzer creation test passed");

                // Test 3: Create test image and process it
                if (!await TestImageProcessingPipelineAsync(logger))
                {
                    logger?.Invoke("❌ Image processing pipeline test failed");
                    return false;
                }
                logger?.Invoke("✅ Image processing pipeline test passed");

                // Test 4: Quality analysis functionality
                if (!await TestQualityAnalysisPipelineAsync(logger))
                {
                    logger?.Invoke("❌ Quality analysis pipeline test failed");
                    return false;
                }
                logger?.Invoke("✅ Quality analysis pipeline test passed");

                // Test 5: Multi-scale processing
                if (!await TestMultiScaleProcessingAsync(logger))
                {
                    logger?.Invoke("❌ Multi-scale processing test failed");
                    return false;
                }
                logger?.Invoke("✅ Multi-scale processing test passed");

                logger?.Invoke("🎯 All Segment 1 tests passed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                logger?.Invoke($"❌ Segment 1 tests failed with exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Legacy synchronous wrapper for backward compatibility with existing UI code.
        /// </summary>
        public static bool RunTests(Action<string> logger = null)
        {
            try
            {
                return RunTestsAsync(logger).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger?.Invoke($"❌ Segment 1 tests failed with exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tests AdvancedImageProcessor creation and basic functionality.
        /// </summary>
        private static async Task<bool> TestAdvancedImageProcessorCreationAsync(Action<string> logger = null)
        {
            try
            {
                using (var processor = new AdvancedImageProcessor())
                {
                    // Test with custom configurations
                    var multiScaleConfig = new AdvancedImageProcessor.MultiScaleConfig
                    {
                        ScaleFactors = new double[] { 0.5, 1.0, 1.5 },
                        MinElementSize = 20,
                        MaxElementSize = 400
                    };

                    var noiseConfig = new AdvancedImageProcessor.NoiseReductionConfig
                    {
                        GaussianKernelSize = 3,
                        EnableBilateralFilter = true
                    };

                    using (var configuredProcessor = new AdvancedImageProcessor(multiScaleConfig, noiseConfig))
                    {
                        logger?.Invoke("  - Default and configured processors created successfully");
                        return await Task.FromResult(true);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Invoke($"  - AdvancedImageProcessor creation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tests ImageQualityAnalyzer creation and basic functionality.
        /// </summary>
        private static async Task<bool> TestImageQualityAnalyzerCreationAsync(Action<string> logger = null)
        {
            try
            {
                using (var analyzer = new ImageQualityAnalyzer())
                {
                    logger?.Invoke("  - ImageQualityAnalyzer created successfully");
                    return await Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                logger?.Invoke($"  - ImageQualityAnalyzer creation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a test image and runs it through the advanced processing pipeline.
        /// </summary>
        private static async Task<bool> TestImageProcessingPipelineAsync(Action<string> logger = null)
        {
            try
            {
                // Create a simple test image (200x200 with some patterns)
                using (var testImage = CreateTestImage())
                using (var processor = new AdvancedImageProcessor())
                {
                    logger?.Invoke("  - Created 200x200 test image with patterns");

                    // Test noise reduction
                    using (var noiseReduced = processor.ReduceNoise(testImage))
                    {
                        if (noiseReduced == null || noiseReduced.IsDisposed)
                            return false;
                        logger?.Invoke("  - Noise reduction: PASSED");
                    }

                    // Test contrast enhancement
                    using (var enhanced = processor.EnhanceContrast(testImage))
                    {
                        if (enhanced == null || enhanced.IsDisposed)
                            return false;
                        logger?.Invoke("  - Contrast enhancement: PASSED");
                    }

                    // Test edge detection optimization
                    using (var edgeOptimized = processor.OptimizeEdgeDetection(testImage))
                    {
                        if (edgeOptimized == null || edgeOptimized.IsDisposed)
                            return false;
                        logger?.Invoke("  - Edge detection optimization: PASSED");
                    }

                    // Test comprehensive preprocessing pipeline
                    using (var preprocessed = processor.PreprocessForDetection(testImage))
                    {
                        if (preprocessed == null || preprocessed.IsDisposed)
                            return false;
                        logger?.Invoke("  - Comprehensive preprocessing pipeline: PASSED");
                    }

                    return await Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                logger?.Invoke($"  - Image processing pipeline failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tests the quality analysis functionality with a test image.
        /// </summary>
        private static async Task<bool> TestQualityAnalysisPipelineAsync(Action<string> logger = null)
        {
            try
            {
                using (var testImageMat = CreateTestImage())
                using (var analyzer = new ImageQualityAnalyzer())
                {
                    // Convert OpenCV Mat to System.Drawing.Bitmap for quality analysis
                    using (var testImageBitmap = BitmapConverter.ToBitmap(testImageMat))
                    {
                        // Perform quality assessment using the new async API
                        var assessment = await analyzer.AnalyzeQualityAsync(testImageBitmap);

                        // Validate assessment results
                        if (assessment == null)
                            return false;

                        // Check that all metrics are within expected ranges
                        if (assessment.OverallScore < 0.0 || assessment.OverallScore > 1.0)
                            return false;

                        if (assessment.BrightnessScore < 0.0 || assessment.BrightnessScore > 1.0)
                            return false;

                        if (assessment.ContrastScore < 0.0 || assessment.ContrastScore > 1.0)
                            return false;

                        if (assessment.SharpnessScore < 0.0 || assessment.SharpnessScore > 1.0)
                            return false;

                        if (assessment.NoiseLevel < 0.0 || assessment.NoiseLevel > 1.0)
                            return false;

                        // Check that recommendations are generated
                        if (assessment.Recommendations == null)
                            return false;

                        // Test legacy compatibility methods
                        var legacyScore = await analyzer.AnalyzeQuality(testImageBitmap);
                        if (legacyScore < 0.0 || legacyScore > 1.0)
                            return false;

                        var description = analyzer.GetQualityDescription(assessment.OverallScore);
                        if (string.IsNullOrEmpty(description))
                            return false;

                        var requiresPreprocessing = analyzer.RequiresPreprocessing(assessment.OverallScore);
                        // This should return a boolean without error

                        logger?.Invoke($"  - Quality Assessment Results:");
                        logger?.Invoke($"    Overall Score: {assessment.OverallScore:F3}");
                        logger?.Invoke($"    Brightness: {assessment.BrightnessScore:F3}");
                        logger?.Invoke($"    Contrast: {assessment.ContrastScore:F3}");
                        logger?.Invoke($"    Sharpness: {assessment.SharpnessScore:F3}");
                        logger?.Invoke($"    Noise Level: {assessment.NoiseLevel:F3}");
                        logger?.Invoke($"    Description: {description}");
                        logger?.Invoke($"    Requires Preprocessing: {requiresPreprocessing}");
                        logger?.Invoke($"    Recommendations Count: {assessment.Recommendations.Count}");
                        logger?.Invoke($"    Legacy Score Match: {Math.Abs(legacyScore - assessment.OverallScore) < 0.001}");

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Invoke($"  - Quality analysis pipeline failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tests multi-scale pyramid creation functionality.
        /// </summary>
        private static async Task<bool> TestMultiScaleProcessingAsync(Action<string> logger = null)
        {
            try
            {
                using (var testImage = CreateTestImage())
                using (var processor = new AdvancedImageProcessor())
                {
                    // Test multi-scale pyramid generation
                    var pyramid = processor.CreateMultiScalePyramid(testImage);

                    if (pyramid == null || pyramid.Count == 0)
                        return false;

                    logger?.Invoke($"  - Multi-scale pyramid created with {pyramid.Count} levels");

                    // Test that pyramid contains different scales
                    var originalSize = testImage.Size();
                    bool foundDifferentScale = false;

                    try
                    {
                        foreach (var kvp in pyramid)
                        {
                            var scaleFactor = kvp.Key;
                            var scaledMat = kvp.Value;

                            if (scaledMat.Size() != originalSize)
                            {
                                foundDifferentScale = true;
                            }

                            logger?.Invoke($"  - Scale {scaleFactor:F1}x: {scaledMat.Width}x{scaledMat.Height}");
                        }

                        if (!foundDifferentScale)
                        {
                            logger?.Invoke("  - Multi-scale processing failed: All scales are same size");
                            return false;
                        }
                    }
                    finally
                    {
                        // Clean up pyramid mats
                        foreach (var kvp in pyramid)
                        {
                            kvp.Value?.Dispose();
                        }
                    }

                    logger?.Invoke("  - Multi-scale processing: PASSED");
                    return await Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                logger?.Invoke($"  - Multi-scale processing failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a test image using OpenCV Mat with various patterns for testing.
        /// </summary>
        /// <returns>Test image Mat.</returns>
        private static Mat CreateTestImage()
        {
            // Create a 200x200 color image with some patterns
            var testImage = new Mat(200, 200, MatType.CV_8UC3, Scalar.White);

            // Add some rectangles for edge detection testing
            Cv2.Rectangle(testImage, new OpenCvSharp.Point(50, 50), new OpenCvSharp.Point(100, 100), Scalar.Blue, -1);
            Cv2.Rectangle(testImage, new OpenCvSharp.Point(120, 120), new OpenCvSharp.Point(170, 170), Scalar.Red, -1);

            // Add some circles for shape testing
            Cv2.Circle(testImage, new OpenCvSharp.Point(75, 150), 20, Scalar.Green, -1);

            // Add some lines for edge testing
            Cv2.Line(testImage, new OpenCvSharp.Point(10, 10), new OpenCvSharp.Point(190, 190), Scalar.Black, 2);

            // Add some text for OCR testing preparation
            Cv2.PutText(testImage, "TEST", new OpenCvSharp.Point(10, 30), HersheyFonts.HersheySimplex, 1, Scalar.Black, 2);

            return testImage;
        }

        /// <summary>
        /// Creates a test bitmap for direct bitmap testing scenarios.
        /// </summary>
        private static Bitmap CreateTestBitmap()
        {
            using (var mat = CreateTestImage())
            {
                return BitmapConverter.ToBitmap(mat);
            }
        }
    }
}