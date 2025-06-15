using CitrixAI.Vision.Processing;
using OpenCvSharp;
using System;
using System.Drawing;
using System.IO;

namespace CitrixAI.Vision.Tests
{
    /// <summary>
    /// Integration test to verify Segment 1 implementation works correctly.
    /// Tests AdvancedImageProcessor and ImageQualityAnalyzer functionality.
    /// </summary>
    public static class Segment1IntegrationTest
    {
        /// <summary>
        /// Runs comprehensive tests for Segment 1 advanced image processing.
        /// </summary>
        /// <param name="logger">Optional callback for logging test progress and results.</param>
        /// <returns>True if all tests pass, false if any failures detected.</returns>
        public static bool RunTests(Action<string> logger = null)
        {
            logger?.Invoke("Starting Segment 1 Integration Tests...");

            try
            {
                // Test 1: AdvancedImageProcessor instantiation
                if (!TestAdvancedImageProcessorCreation(logger))
                {
                    logger?.Invoke("❌ AdvancedImageProcessor creation test failed");
                    return false;
                }
                logger?.Invoke("✅ AdvancedImageProcessor creation test passed");

                // Test 2: ImageQualityAnalyzer instantiation
                if (!TestImageQualityAnalyzerCreation(logger))
                {
                    logger?.Invoke("❌ ImageQualityAnalyzer creation test failed");
                    return false;
                }
                logger?.Invoke("✅ ImageQualityAnalyzer creation test passed");

                // Test 3: Create test image and process it
                if (!TestImageProcessingPipeline(logger))
                {
                    logger?.Invoke("❌ Image processing pipeline test failed");
                    return false;
                }
                logger?.Invoke("✅ Image processing pipeline test passed");

                // Test 4: Quality analysis functionality
                if (!TestQualityAnalysisPipeline(logger))
                {
                    logger?.Invoke("❌ Quality analysis pipeline test failed");
                    return false;
                }
                logger?.Invoke("✅ Quality analysis pipeline test passed");

                // Test 5: Multi-scale processing
                if (!TestMultiScaleProcessing(logger))
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
        /// Tests AdvancedImageProcessor creation and basic functionality.
        /// </summary>
        private static bool TestAdvancedImageProcessorCreation(Action<string> logger = null)
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
                        return true; // Successfully created both default and configured processors
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
        private static bool TestImageQualityAnalyzerCreation(Action<string> logger = null)
        {
            try
            {
                using (var analyzer = new ImageQualityAnalyzer())
                {
                    logger?.Invoke("  - ImageQualityAnalyzer created successfully");
                    return true; // Successfully created analyzer
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
        private static bool TestImageProcessingPipeline(Action<string> logger = null)
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

                    return true;
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
        private static bool TestQualityAnalysisPipeline(Action<string> logger = null)
        {
            try
            {
                using (var testImage = CreateTestImage())
                using (var analyzer = new ImageQualityAnalyzer())
                {
                    // Perform quality assessment
                    var assessment = analyzer.AnalyzeQuality(testImage);

                    // Validate assessment results
                    if (assessment == null)
                        return false;

                    // Check that all metrics are within expected ranges
                    if (assessment.OverallScore < 0.0 || assessment.OverallScore > 1.0)
                        return false;

                    if (assessment.Brightness < 0.0 || assessment.Brightness > 1.0)
                        return false;

                    if (assessment.Contrast < 0.0 || assessment.Contrast > 1.0)
                        return false;

                    if (assessment.Sharpness < 0.0 || assessment.Sharpness > 1.0)
                        return false;

                    if (assessment.NoiseLevel < 0.0 || assessment.NoiseLevel > 1.0)
                        return false;

                    // Check that recommendations are generated
                    if (assessment.Recommendations == null)
                        return false;

                    // Test quality description functionality
                    var description = analyzer.GetQualityDescription(assessment.OverallScore);
                    if (string.IsNullOrEmpty(description))
                        return false;

                    // Test preprocessing requirement check
                    var requiresPreprocessing = analyzer.RequiresPreprocessing(assessment);
                    // This should return a boolean without error

                    logger?.Invoke($"  - Quality Assessment Results:");
                    logger?.Invoke($"    Overall Score: {assessment.OverallScore:F3}");
                    logger?.Invoke($"    Brightness: {assessment.Brightness:F3}");
                    logger?.Invoke($"    Contrast: {assessment.Contrast:F3}");
                    logger?.Invoke($"    Sharpness: {assessment.Sharpness:F3}");
                    logger?.Invoke($"    Noise Level: {assessment.NoiseLevel:F3}");
                    logger?.Invoke($"    Edge Density: {assessment.EdgeDensity:F3}");
                    logger?.Invoke($"    Description: {description}");
                    logger?.Invoke($"    Requires Preprocessing: {requiresPreprocessing}");
                    logger?.Invoke($"    Recommendations Count: {assessment.Recommendations.Count}");

                    return true;
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
        private static bool TestMultiScaleProcessing(Action<string> logger = null)
        {
            try
            {
                using (var testImage = CreateTestImage())
                using (var processor = new AdvancedImageProcessor())
                {
                    // Create multi-scale pyramid
                    var pyramid = processor.CreateMultiScalePyramid(testImage);

                    try
                    {
                        // Validate pyramid creation
                        if (pyramid == null || pyramid.Count == 0)
                            return false;

                        // Check that different scales are present
                        if (!pyramid.ContainsKey(1.0)) // Should always contain original scale
                            return false;

                        logger?.Invoke($"  - Multi-scale pyramid created with {pyramid.Count} scales");

                        // Validate that scaled images have appropriate sizes
                        foreach (var kvp in pyramid)
                        {
                            var scaleFactor = kvp.Key;
                            var scaledImage = kvp.Value;

                            if (scaledImage == null || scaledImage.IsDisposed)
                                return false;

                            // Check that the size matches the scale factor (approximately)
                            var expectedWidth = (int)(testImage.Width * scaleFactor);
                            var expectedHeight = (int)(testImage.Height * scaleFactor);

                            // Allow for small rounding differences
                            if (Math.Abs(scaledImage.Width - expectedWidth) > 2 ||
                                Math.Abs(scaledImage.Height - expectedHeight) > 2)
                            {
                                logger?.Invoke($"  - Scale factor {scaleFactor}: Expected {expectedWidth}x{expectedHeight}, got {scaledImage.Width}x{scaledImage.Height}");
                                return false;
                            }

                            logger?.Invoke($"  - Scale {scaleFactor:F1}x: {scaledImage.Width}x{scaledImage.Height} ✓");
                        }

                        return true;
                    }
                    finally
                    {
                        // Clean up pyramid images
                        foreach (var image in pyramid.Values)
                        {
                            image?.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Invoke($"  - Multi-scale processing failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a test image with various patterns for testing purposes.
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
    }
}