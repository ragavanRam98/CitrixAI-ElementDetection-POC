using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using CitrixAI.Core.ML.Models;
using CitrixAI.Core.ML.Interfaces;

namespace CitrixAI.Core.ML.Training
{
    /// <summary>
    /// Provides data augmentation capabilities specifically designed for UI element training data.
    /// Generates variations of training samples to improve model robustness and generalization.
    /// </summary>
    public class DataAugmentation : IDisposable
    {
        private readonly AugmentationConfig _config;
        private readonly Random _random;
        private bool _disposed = false;

        public DataAugmentation(AugmentationConfig config = null)
        {
            _config = config ?? new AugmentationConfig();
            _random = new Random(_config.RandomSeed);
        }

        /// <summary>
        /// Generates multiple augmented versions of a training sample.
        /// Creates variations through rotation, scaling, noise, and lighting adjustments.
        /// </summary>
        public async Task<List<TrainingSample>> AugmentSampleAsync(TrainingSample originalSample, int targetCount = 10)
        {
            if (originalSample == null)
                throw new ArgumentNullException(nameof(originalSample));

            var augmentedSamples = new List<TrainingSample> { originalSample };

            try
            {
                using (var originalImage = new Bitmap(originalSample.ImagePath))
                {
                    for (int i = 1; i < targetCount; i++)
                    {
                        var augmentedImage = await ApplyRandomAugmentationAsync(originalImage);
                        var augmentedSample = CreateAugmentedSample(originalSample, augmentedImage, i);
                        augmentedSamples.Add(augmentedSample);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to augment sample {originalSample.SampleId}: {ex.Message}", ex);
            }

            return augmentedSamples;
        }

        /// <summary>
        /// Augments an entire dataset by generating variations for each sample.
        /// Maintains balanced distribution across different element types.
        /// </summary>
        public async Task<Dataset> AugmentDatasetAsync(Dataset originalDataset, int augmentationsPerSample = 5)
        {
            if (originalDataset == null)
                throw new ArgumentNullException(nameof(originalDataset));

            var augmentedDataset = new Dataset
            {
                DatasetId = $"{originalDataset.DatasetId}_augmented",
                Samples = new List<TrainingSample>(),
                Metadata = new DatasetMetadata
                {
                    CreatedDate = DateTime.UtcNow,
                    SourcePath = originalDataset.Metadata?.SourcePath,
                    Version = "1.0_augmented",
                    Description = $"Augmented version of {originalDataset.DatasetId} with {augmentationsPerSample}x variations"
                }
            };

            foreach (var originalSample in originalDataset.Samples)
            {
                try
                {
                    var augmentedSamples = await AugmentSampleAsync(originalSample, augmentationsPerSample + 1);
                    foreach (var sample in augmentedSamples)
                    {
                        augmentedDataset.Samples.Add(sample);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to augment sample {originalSample.SampleId}: {ex.Message}");
                    augmentedDataset.Samples.Add(originalSample);
                }
            }

            return augmentedDataset;
        }

        /// <summary>
        /// Applies a random combination of augmentation techniques to an image.
        /// Selects augmentations based on configuration probabilities.
        /// </summary>
        private async Task<Bitmap> ApplyRandomAugmentationAsync(Bitmap originalImage)
        {
            var augmentedImage = new Bitmap(originalImage);

            await Task.Run(() =>
            {
                if (_random.NextDouble() < _config.RotationProbability)
                {
                    var angle = (_random.NextDouble() - 0.5) * 2 * _config.MaxRotationDegrees;
                    augmentedImage = ApplyRotation(augmentedImage, angle);
                }

                if (_random.NextDouble() < _config.ScalingProbability)
                {
                    var scale = _config.MinScaleFactor +
                               (_random.NextDouble() * (_config.MaxScaleFactor - _config.MinScaleFactor));
                    augmentedImage = ApplyScaling(augmentedImage, scale);
                }

                if (_random.NextDouble() < _config.BrightnessProbability)
                {
                    var brightness = (_random.NextDouble() - 0.5) * 2 * _config.MaxBrightnessChange;
                    augmentedImage = ApplyBrightnessAdjustment(augmentedImage, brightness);
                }

                if (_random.NextDouble() < _config.ContrastProbability)
                {
                    var contrast = _config.MinContrastFactor +
                                  (_random.NextDouble() * (_config.MaxContrastFactor - _config.MinContrastFactor));
                    augmentedImage = ApplyContrastAdjustment(augmentedImage, contrast);
                }

                if (_random.NextDouble() < _config.NoiseProbability)
                {
                    var noiseLevel = _random.NextDouble() * _config.MaxNoiseLevel;
                    augmentedImage = ApplyNoise(augmentedImage, noiseLevel);
                }
            });

            return augmentedImage;
        }

        /// <summary>
        /// Applies rotation transformation while preserving image quality.
        /// Uses high-quality interpolation and maintains original dimensions.
        /// </summary>
        private Bitmap ApplyRotation(Bitmap source, double angleDegrees)
        {
            if (Math.Abs(angleDegrees) < 0.1) return new Bitmap(source);

            var rotated = new Bitmap(source.Width, source.Height);

            using (var graphics = Graphics.FromImage(rotated))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.TranslateTransform(source.Width / 2.0f, source.Height / 2.0f);
                graphics.RotateTransform((float)angleDegrees);
                graphics.TranslateTransform(-source.Width / 2.0f, -source.Height / 2.0f);

                graphics.DrawImage(source, new Point(0, 0));
            }

            return rotated;
        }

        /// <summary>
        /// Applies uniform scaling while maintaining aspect ratio.
        /// Centers the scaled image within the original dimensions.
        /// </summary>
        private Bitmap ApplyScaling(Bitmap source, double scaleFactor)
        {
            if (Math.Abs(scaleFactor - 1.0) < 0.01) return new Bitmap(source);

            var scaled = new Bitmap(source.Width, source.Height);

            var newWidth = (int)(source.Width * scaleFactor);
            var newHeight = (int)(source.Height * scaleFactor);

            var x = (source.Width - newWidth) / 2;
            var y = (source.Height - newHeight) / 2;

            using (var graphics = Graphics.FromImage(scaled))
            {
                graphics.Clear(Color.White);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                graphics.DrawImage(source, x, y, newWidth, newHeight);
            }

            return scaled;
        }

        /// <summary>
        /// Adjusts image brightness using color matrix transformation.
        /// Preserves color relationships while modifying luminosity.
        /// </summary>
        private Bitmap ApplyBrightnessAdjustment(Bitmap source, double brightnessChange)
        {
            if (Math.Abs(brightnessChange) < 0.01) return new Bitmap(source);

            var adjusted = new Bitmap(source.Width, source.Height);

            var colorMatrix = new ColorMatrix(new float[][]
            {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {(float)brightnessChange, (float)brightnessChange, (float)brightnessChange, 0, 1}
            });

            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);

            using (var graphics = Graphics.FromImage(adjusted))
            {
                graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
                                 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }

            return adjusted;
        }

        /// <summary>
        /// Adjusts image contrast using color matrix scaling.
        /// Enhances or reduces the difference between light and dark areas.
        /// </summary>
        private Bitmap ApplyContrastAdjustment(Bitmap source, double contrastFactor)
        {
            if (Math.Abs(contrastFactor - 1.0) < 0.01) return new Bitmap(source);

            var adjusted = new Bitmap(source.Width, source.Height);

            var factor = (float)contrastFactor;
            var translation = (1 - factor) / 2;

            var colorMatrix = new ColorMatrix(new float[][]
            {
                new float[] {factor, 0, 0, 0, 0},
                new float[] {0, factor, 0, 0, 0},
                new float[] {0, 0, factor, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {translation, translation, translation, 0, 1}
            });

            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);

            using (var graphics = Graphics.FromImage(adjusted))
            {
                graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
                                 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }

            return adjusted;
        }

        /// <summary>
        /// Adds controlled random noise to simulate real-world image variations.
        /// Uses safe managed code for cross-platform compatibility.
        /// </summary>
        private Bitmap ApplyNoise(Bitmap source, double noiseLevel)
        {
            if (noiseLevel < 0.01) return new Bitmap(source);

            var noisy = new Bitmap(source.Width, source.Height, source.PixelFormat);

            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    var originalColor = source.GetPixel(x, y);

                    var noise = GenerateGaussianNoise() * noiseLevel * 255;

                    var newR = Math.Max(0, Math.Min(255, originalColor.R + (int)noise));
                    var newG = Math.Max(0, Math.Min(255, originalColor.G + (int)noise));
                    var newB = Math.Max(0, Math.Min(255, originalColor.B + (int)noise));

                    var noisyColor = Color.FromArgb(originalColor.A, newR, newG, newB);
                    noisy.SetPixel(x, y, noisyColor);
                }
            }

            return noisy;
        }

        /// <summary>
        /// Generates Gaussian-distributed random noise using Box-Muller transform.
        /// Provides natural noise distribution for image augmentation.
        /// </summary>
        private double GenerateGaussianNoise()
        {
            var u1 = 1.0 - _random.NextDouble();
            var u2 = 1.0 - _random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        /// <summary>
        /// Creates a new training sample from an augmented image.
        /// Updates metadata and generates appropriate file paths.
        /// </summary>
        private TrainingSample CreateAugmentedSample(TrainingSample original, Bitmap augmentedImage, int augmentationIndex)
        {
            var augmentedPath = GenerateAugmentedImagePath(original.ImagePath, augmentationIndex);

            try
            {
                augmentedImage.Save(augmentedPath, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save augmented image: {ex.Message}", ex);
            }

            return new TrainingSample
            {
                SampleId = $"{original.SampleId}_aug_{augmentationIndex}",
                ImagePath = augmentedPath,
                Annotations = CloneAnnotations(original.Annotations),
                QualityScore = original.QualityScore * 0.95, // Slightly lower due to augmentation
                Metadata = new SampleMetadata
                {
                    OriginalPath = original.ImagePath,
                    CreatedDate = DateTime.UtcNow,
                    ProcessingHistory = new Dictionary<string, object>
                    {
                        { "augmentation_type", "random_combination" },
                        { "augmentation_index", augmentationIndex },
                        { "source_sample", original.SampleId }
                    }
                }
            };
        }

        /// <summary>
        /// Generates appropriate file path for augmented images.
        /// Maintains organization and prevents filename conflicts.
        /// </summary>
        private string GenerateAugmentedImagePath(string originalPath, int augmentationIndex)
        {
            var directory = System.IO.Path.GetDirectoryName(originalPath);
            var fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(originalPath);
            var extension = System.IO.Path.GetExtension(originalPath);

            return System.IO.Path.Combine(directory, $"{fileNameWithoutExtension}_aug_{augmentationIndex}{extension}");
        }

        /// <summary>
        /// Creates deep copies of annotations for augmented samples.
        /// Preserves all annotation data while maintaining independence.
        /// </summary>
        private List<ElementAnnotation> CloneAnnotations(IList<ElementAnnotation> originalAnnotations)
        {
            var clonedAnnotations = new List<ElementAnnotation>();

            foreach (var annotation in originalAnnotations)
            {
                clonedAnnotations.Add(new ElementAnnotation
                {
                    AnnotationId = $"{annotation.AnnotationId}_cloned",
                    BoundingBox = annotation.BoundingBox,
                    ElementType = annotation.ElementType,
                    Text = annotation.Text,
                    Confidence = annotation.Confidence,
                    SourceImage = annotation.SourceImage,
                    Properties = new Dictionary<string, object>(annotation.Properties)
                });
            }

            return clonedAnnotations;
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
    /// Configuration parameters for data augmentation operations.
    /// Controls the intensity and probability of different augmentation techniques.
    /// </summary>
    public class AugmentationConfig
    {
        public int RandomSeed { get; set; } = 42;

        public double RotationProbability { get; set; } = 0.7;
        public double MaxRotationDegrees { get; set; } = 15.0;

        public double ScalingProbability { get; set; } = 0.6;
        public double MinScaleFactor { get; set; } = 0.85;
        public double MaxScaleFactor { get; set; } = 1.15;

        public double BrightnessProbability { get; set; } = 0.5;
        public double MaxBrightnessChange { get; set; } = 0.2;

        public double ContrastProbability { get; set; } = 0.5;
        public double MinContrastFactor { get; set; } = 0.8;
        public double MaxContrastFactor { get; set; } = 1.2;

        public double NoiseProbability { get; set; } = 0.3;
        public double MaxNoiseLevel { get; set; } = 0.05;
    }
}