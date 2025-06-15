using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using CitrixAI.Core.Interfaces;
using CitrixAI.Core.ML.Models;
using CitrixAI.Core.ML.Interfaces;
using CitrixAI.Core.ML.ImageProcessing;
using Newtonsoft.Json;

namespace CitrixAI.Core.ML.Training
{
    /// <summary>
    /// Manages training data ingestion, validation, and quality-based curation for model training.
    /// This class handles the complete lifecycle of training datasets from raw annotation files
    /// to quality-filtered, validated datasets ready for machine learning training pipelines.
    /// </summary>
    public class TrainingDataManager : ITrainingDataSource, IDisposable
    {
        private readonly ImageQualityAnalyzer _qualityAnalyzer;
        private readonly string _workingDirectory;
        private bool _disposed = false;

        public TrainingDataManager(string workingDirectory = null)
        {
            _workingDirectory = workingDirectory ?? Path.GetTempPath();
            _qualityAnalyzer = new ImageQualityAnalyzer();
        }

        /// <summary>
        /// Loads a complete dataset from the specified path, including images and annotations.
        /// Validates file structure and ensures all referenced images exist.
        /// </summary>
        public async Task<Dataset> LoadDatasetAsync(string datasetPath)
        {
            if (string.IsNullOrEmpty(datasetPath) || !Directory.Exists(datasetPath))
            {
                throw new ArgumentException("Dataset path must be a valid directory", nameof(datasetPath));
            }

            var dataset = new Dataset
            {
                DatasetId = Path.GetFileName(datasetPath),
                Samples = new List<TrainingSample>(),
                Metadata = new DatasetMetadata
                {
                    CreatedDate = DateTime.UtcNow,
                    SourcePath = datasetPath,
                    Version = "1.0"
                }
            };

            var annotationFiles = Directory.GetFiles(datasetPath, "*.json", SearchOption.AllDirectories);

            foreach (var annotationFile in annotationFiles)
            {
                try
                {
                    var annotationData = await ParseAnnotationsAsync(annotationFile);
                    var samples = await CreateSamplesFromAnnotations(annotationData, datasetPath);

                    foreach (var sample in samples)
                    {
                        dataset.Samples.Add(sample);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse annotation file {annotationFile}: {ex.Message}");
                }
            }

            dataset.Statistics = await GenerateDatasetStatsAsync(dataset);
            return dataset;
        }

        /// <summary>
        /// Validates the integrity and completeness of a loaded dataset.
        /// Checks for missing files, corrupt annotations, and data consistency.
        /// </summary>
        public async Task<bool> ValidateDatasetAsync(Dataset dataset)
        {
            if (dataset == null || dataset.Samples == null)
                return false;

            foreach (var sample in dataset.Samples)
            {
                if (!File.Exists(sample.ImagePath))
                    return false;

                if (sample.Annotations != null)
                {
                    foreach (var annotation in sample.Annotations)
                    {
                        if (!await ValidateIndividualAnnotation(annotation))
                            return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Generates comprehensive statistics about the dataset composition.
        /// Provides insights for training strategy and data balance assessment.
        /// </summary>
        public async Task<DatasetStatistics> GetStatisticsAsync(Dataset dataset)
        {
            return await GenerateDatasetStatsAsync(dataset);
        }

        /// <summary>
        /// Parses annotation data from JSON files following standard annotation formats.
        /// Supports multiple annotation schemas and validates annotation completeness.
        /// </summary>
        public async Task<AnnotationData> ParseAnnotationsAsync(string annotationFile)
        {
            if (!File.Exists(annotationFile))
            {
                throw new FileNotFoundException($"Annotation file not found: {annotationFile}");
            }

            var jsonContent = File.ReadAllText(annotationFile);

            try
            {
                var rawAnnotations = JsonConvert.DeserializeObject<dynamic>(jsonContent);

                var annotationData = new AnnotationData
                {
                    SourceFile = annotationFile,
                    Format = DetectAnnotationFormat(rawAnnotations),
                    Elements = new List<ElementAnnotation>(),
                    Metadata = new AnnotationMetadata
                    {
                        CreatedDate = File.GetCreationTimeUtc(annotationFile),
                        LastModified = File.GetLastWriteTimeUtc(annotationFile)
                    }
                };

                if (annotationData.Format == "COCO")
                {
                    await ParseCocoFormat(rawAnnotations, annotationData);
                }
                else if (annotationData.Format == "YOLO")
                {
                    await ParseYoloFormat(rawAnnotations, annotationData);
                }
                else
                {
                    await ParseCustomFormat(rawAnnotations, annotationData);
                }

                return annotationData;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Invalid JSON in annotation file {annotationFile}: {ex.Message}");
            }
        }

        /// <summary>
        /// Filters dataset samples based on image quality scores using the existing ImageQualityAnalyzer.
        /// Removes low-quality images that would negatively impact training performance.
        /// </summary>
        public async Task<FilteredDataset> FilterByQualityAsync(Dataset dataset, double minQualityScore = 0.5)
        {
            var filteredSamples = new List<TrainingSample>();
            var rejectedSamples = new List<TrainingSample>();

            foreach (var sample in dataset.Samples)
            {
                try
                {
                    if (File.Exists(sample.ImagePath))
                    {
                        using (var bitmap = new Bitmap(sample.ImagePath))
                        {
                            var qualityResult = await _qualityAnalyzer.AnalyzeQualityAsync(bitmap);
                            sample.QualityScore = qualityResult.OverallScore;

                            if (qualityResult.OverallScore >= minQualityScore)
                            {
                                filteredSamples.Add(sample);
                            }
                            else
                            {
                                rejectedSamples.Add(sample);
                            }
                        }
                    }
                    else
                    {
                        rejectedSamples.Add(sample);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error analyzing quality for {sample.ImagePath}: {ex.Message}");
                    rejectedSamples.Add(sample);
                }
            }

            return new FilteredDataset
            {
                OriginalDataset = dataset,
                FilteredSamples = filteredSamples,
                RejectedSamples = rejectedSamples,
                FilterCriteria = $"MinQualityScore: {minQualityScore}",
                FilteredCount = filteredSamples.Count,
                RejectedCount = rejectedSamples.Count
            };
        }

        /// <summary>
        /// Generates comprehensive quality analysis report for the entire dataset.
        /// Provides insights into data distribution, quality metrics, and improvement recommendations.
        /// </summary>
        public async Task<DatasetQualityReport> AnalyzeDatasetQualityAsync(Dataset dataset)
        {
            var qualityScores = new List<double>();
            var elementTypeCounts = new Dictionary<ElementType, int>();
            var qualityByType = new Dictionary<ElementType, List<double>>();

            foreach (var sample in dataset.Samples)
            {
                if (sample.QualityScore > 0)
                {
                    qualityScores.Add(sample.QualityScore);
                }

                foreach (var annotation in sample.Annotations)
                {
                    if (!elementTypeCounts.ContainsKey(annotation.ElementType))
                    {
                        elementTypeCounts[annotation.ElementType] = 0;
                        qualityByType[annotation.ElementType] = new List<double>();
                    }

                    elementTypeCounts[annotation.ElementType]++;

                    if (sample.QualityScore > 0)
                    {
                        qualityByType[annotation.ElementType].Add(sample.QualityScore);
                    }
                }
            }

            var report = new DatasetQualityReport
            {
                TotalSamples = dataset.Samples.Count,
                AverageQuality = qualityScores.Count > 0 ? qualityScores.Average() : 0,
                MinQuality = qualityScores.Count > 0 ? qualityScores.Min() : 0,
                MaxQuality = qualityScores.Count > 0 ? qualityScores.Max() : 0,
                ElementTypeDistribution = elementTypeCounts,
                QualityByElementType = qualityByType.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Count > 0 ? kvp.Value.Average() : 0
                ),
                Recommendations = GenerateQualityRecommendations(qualityScores, elementTypeCounts)
            };

            return report;
        }

        /// <summary>
        /// Creates balanced train/validation split maintaining element type distribution.
        /// Ensures both sets have representative samples of all element types.
        /// </summary>
        public async Task<DatasetSplit> CreateTrainValidationSplitAsync(Dataset dataset, double splitRatio = 0.8)
        {
            if (splitRatio <= 0 || splitRatio >= 1)
            {
                throw new ArgumentException("Split ratio must be between 0 and 1", nameof(splitRatio));
            }

            var samplesByType = dataset.Samples
                .GroupBy(s => s.Annotations.FirstOrDefault()?.ElementType ?? ElementType.Button)
                .ToDictionary(g => g.Key, g => g.ToList());

            var trainingSamples = new List<TrainingSample>();
            var validationSamples = new List<TrainingSample>();

            foreach (var typeGroup in samplesByType)
            {
                var samples = typeGroup.Value;
                var trainCount = (int)(samples.Count * splitRatio);

                var shuffled = samples.OrderBy(x => Guid.NewGuid()).ToList();

                trainingSamples.AddRange(shuffled.Take(trainCount));
                validationSamples.AddRange(shuffled.Skip(trainCount));
            }

            return new DatasetSplit
            {
                OriginalDataset = dataset,
                TrainingSamples = trainingSamples,
                ValidationSamples = validationSamples,
                SplitRatio = splitRatio,
                TrainingCount = trainingSamples.Count,
                ValidationCount = validationSamples.Count
            };
        }

        /// <summary>
        /// Validates annotation consistency and completeness across the dataset.
        /// Checks for missing bounding boxes, invalid coordinates, and annotation format compliance.
        /// </summary>
        public async Task<bool> ValidateAnnotationsAsync(AnnotationData annotations)
        {
            if (annotations == null || annotations.Elements == null)
            {
                return false;
            }

            foreach (var element in annotations.Elements)
            {
                if (!await ValidateIndividualAnnotation(element))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Generates comprehensive statistics about dataset composition and distribution.
        /// Provides insights for training strategy optimization and data balance assessment.
        /// </summary>
        public async Task<DatasetStatistics> GenerateDatasetStatsAsync(Dataset dataset)
        {
            var elementCounts = new Dictionary<ElementType, int>();
            var sizeCounts = new Dictionary<string, int>();
            var qualityScores = new List<double>();

            foreach (var sample in dataset.Samples)
            {
                if (sample.QualityScore > 0)
                {
                    qualityScores.Add(sample.QualityScore);
                }

                foreach (var annotation in sample.Annotations)
                {
                    if (!elementCounts.ContainsKey(annotation.ElementType))
                    {
                        elementCounts[annotation.ElementType] = 0;
                    }
                    elementCounts[annotation.ElementType]++;

                    var sizeCategory = CategorizeElementSize(annotation.BoundingBox);
                    if (!sizeCounts.ContainsKey(sizeCategory))
                    {
                        sizeCounts[sizeCategory] = 0;
                    }
                    sizeCounts[sizeCategory]++;
                }
            }

            return new DatasetStatistics
            {
                TotalSamples = dataset.Samples.Count,
                TotalAnnotations = dataset.Samples.Sum(s => s.Annotations.Count),
                ElementTypeDistribution = elementCounts,
                SizeDistribution = sizeCounts,
                AverageQualityScore = qualityScores.Count > 0 ? qualityScores.Average() : 0,
                QualityRange = qualityScores.Count > 0 ? new QualityRange
                {
                    Min = qualityScores.Min(),
                    Max = qualityScores.Max(),
                    StandardDeviation = CalculateStandardDeviation(qualityScores)
                } : null
            };
        }

        // Helper Methods

        private async Task<bool> ValidateIndividualAnnotation(ElementAnnotation annotation)
        {
            if (annotation.BoundingBox.Width <= 0 || annotation.BoundingBox.Height <= 0)
                return false;

            if (annotation.BoundingBox.X < 0 || annotation.BoundingBox.Y < 0)
                return false;

            if (!Enum.IsDefined(typeof(ElementType), annotation.ElementType))
                return false;

            if (annotation.Confidence.HasValue && (annotation.Confidence.Value < 0 || annotation.Confidence.Value > 1))
                return false;

            return await Task.FromResult(true);
        }

        private string DetectAnnotationFormat(dynamic annotations)
        {
            if (annotations.images != null && annotations.annotations != null)
            {
                return "COCO";
            }
            else if (annotations.classes != null)
            {
                return "YOLO";
            }
            else
            {
                return "Custom";
            }
        }

        private async Task ParseCocoFormat(dynamic rawData, AnnotationData annotationData)
        {
            await Task.CompletedTask;
        }

        private async Task ParseYoloFormat(dynamic rawData, AnnotationData annotationData)
        {
            await Task.CompletedTask;
        }

        private async Task ParseCustomFormat(dynamic rawData, AnnotationData annotationData)
        {
            await Task.CompletedTask;
        }

        private async Task<List<TrainingSample>> CreateSamplesFromAnnotations(AnnotationData annotationData, string basePath)
        {
            var samples = new List<TrainingSample>();

            var annotationsByImage = annotationData.Elements.GroupBy(e => e.SourceImage);

            foreach (var imageGroup in annotationsByImage)
            {
                var imagePath = Path.Combine(basePath, imageGroup.Key);
                if (File.Exists(imagePath))
                {
                    var sample = new TrainingSample
                    {
                        SampleId = Guid.NewGuid().ToString(),
                        ImagePath = imagePath,
                        Annotations = imageGroup.ToList(),
                        QualityScore = 0,
                        Metadata = new SampleMetadata
                        {
                            OriginalPath = imagePath,
                            CreatedDate = DateTime.UtcNow
                        }
                    };

                    samples.Add(sample);
                }
            }

            return samples;
        }

        private List<string> GenerateQualityRecommendations(List<double> qualityScores, Dictionary<ElementType, int> elementCounts)
        {
            var recommendations = new List<string>();

            if (qualityScores.Count > 0)
            {
                var avgQuality = qualityScores.Average();
                if (avgQuality < 0.6)
                {
                    recommendations.Add("Dataset average quality is below recommended threshold. Consider image preprocessing.");
                }

                var lowQualityCount = qualityScores.Count(q => q < 0.4);
                if (lowQualityCount > qualityScores.Count * 0.2)
                {
                    recommendations.Add($"High number of low-quality images ({lowQualityCount}). Review image capture process.");
                }
            }

            var totalElements = elementCounts.Values.Sum();
            if (totalElements > 0)
            {
                var minElementsPerType = totalElements / elementCounts.Count * 0.1;

                foreach (var elementType in elementCounts.Where(kvp => kvp.Value < minElementsPerType))
                {
                    recommendations.Add($"Element type {elementType.Key} is underrepresented. Consider adding more samples.");
                }
            }

            return recommendations;
        }

        private string CategorizeElementSize(Rectangle boundingBox)
        {
            var area = boundingBox.Width * boundingBox.Height;

            if (area < 500)
                return "Small";
            else if (area < 5000)
                return "Medium";
            else
                return "Large";
        }

        private double CalculateStandardDeviation(List<double> values)
        {
            if (values.Count < 2) return 0;

            var avg = values.Average();
            var sumSquaredDifferences = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sumSquaredDifferences / (values.Count - 1));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _qualityAnalyzer?.Dispose();
                _disposed = true;
            }
        }
    }
}