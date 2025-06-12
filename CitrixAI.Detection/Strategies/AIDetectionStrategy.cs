using CitrixAI.Core.Interfaces;
using CitrixAI.Core.Models;
using CitrixAI.Core.ML;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CitrixAI.Detection.Strategies
{
    public sealed class AIDetectionStrategy : IDetectionStrategy, IDisposable
    {
        private readonly IModelManager _modelManager;
        private readonly string _modelPath;
        private readonly double _confidenceThreshold;
        private readonly double _nmsThreshold;
        private bool _disposed;
        private bool _isInitialized;

        public AIDetectionStrategy(string modelPath = null, double confidenceThreshold = 0.5, double nmsThreshold = 0.4)
        {
            _modelPath = modelPath ?? GetDefaultModelPath();
            _confidenceThreshold = confidenceThreshold;
            _nmsThreshold = nmsThreshold;
            _modelManager = new ModelManager();

            StrategyId = "AI_Detection";
            Name = "AI Neural Network Detection";
            Priority = 90;
        }

        public string StrategyId { get; }
        public string Name { get; }
        public int Priority { get; }

        public bool CanHandle(IDetectionContext context)
        {
            return context?.SourceImage != null && IsConfigured();
        }

        public async Task<IDetectionResult> DetectAsync(IDetectionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await EnsureModelLoadedAsync();
                var detectedElements = new List<IElementInfo>();

                if (_modelManager.IsModelLoaded("ui_detection"))
                {
                    detectedElements = await RunRealAIDetection(context);
                }
                else
                {
                    detectedElements = GenerateMockDetections(context.SourceImage.Size);
                }

                stopwatch.Stop();

                var metadata = CreateMetadata(stopwatch.Elapsed, detectedElements.Count);
                var overallConfidence = CalculateOverallConfidence(detectedElements);

                return new DetectionResult(
                    StrategyId,
                    detectedElements,
                    overallConfidence,
                    stopwatch.Elapsed,
                    metadata);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return DetectionResult.CreateFailure(StrategyId, $"AI detection failed: {ex.Message}", stopwatch.Elapsed);
            }
        }

        public TimeSpan GetEstimatedProcessingTime(Size imageSize)
        {
            var baseTime = 1000;
            var complexityFactor = (imageSize.Width * imageSize.Height) / (640.0 * 480.0);
            return TimeSpan.FromMilliseconds(baseTime * Math.Min(complexityFactor, 3.0));
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(_modelPath);
        }

        private async Task EnsureModelLoadedAsync()
        {
            if (_isInitialized) return;

            await Task.Run(() =>
            {
                if (System.IO.File.Exists(_modelPath))
                {
                    _modelManager.LoadModel(_modelPath, "ui_detection");
                }
                _isInitialized = true;
            });
        }

        private async Task<List<IElementInfo>> RunRealAIDetection(IDetectionContext context)
        {
            return await Task.Run(() =>
            {
                var preprocessedTensor = _modelManager.PreprocessImage(context.SourceImage);
                var rawOutput = _modelManager.RunInference(preprocessedTensor);
                var detections = PostProcessDetections(rawOutput, context.SourceImage.Size);
                var filteredDetections = ApplyNonMaximumSuppression(detections);

                return filteredDetections.Take(context.MaxResults)
                    .Where(d => d.Confidence >= context.MinimumConfidence)
                    .Select(d => CreateElementInfo(d))
                    .ToList();
            });
        }

        private List<IElementInfo> GenerateMockDetections(Size imageSize)
        {
            var random = new Random(42);
            var detections = new List<IElementInfo>();
            int numElements = random.Next(3, 9);

            for (int i = 0; i < numElements; i++)
            {
                var width = random.Next(60, 200);
                var height = random.Next(25, 60);
                var x = random.Next(0, Math.Max(1, imageSize.Width - width));
                var y = random.Next(0, Math.Max(1, imageSize.Height - height));

                var elementTypes = new[] { ElementType.Button, ElementType.TextBox, ElementType.Label, ElementType.Dropdown };
                var elementType = elementTypes[random.Next(elementTypes.Length)];
                var confidence = 0.7 + (random.NextDouble() * 0.25);

                var properties = new Dictionary<string, object>
                {
                    ["AIModel"] = "Mock",
                    ["MockGenerated"] = true
                };

                var element = new ElementInfo(
                    new Rectangle(x, y, width, height),
                    elementType,
                    confidence,
                    text: $"Mock_{elementType}",
                    properties: properties);

                detections.Add(element);
            }

            return detections;
        }

        private List<Detection> PostProcessDetections(float[] output, Size imageSize)
        {
            var detections = new List<Detection>();
            int numDetections = output.Length / 85;

            for (int i = 0; i < numDetections; i++)
            {
                int offset = i * 85;

                float centerX = output[offset + 0];
                float centerY = output[offset + 1];
                float width = output[offset + 2];
                float height = output[offset + 3];
                float confidence = output[offset + 4];

                if (confidence < _confidenceThreshold) continue;

                int x = (int)((centerX - width / 2) * imageSize.Width);
                int y = (int)((centerY - height / 2) * imageSize.Height);
                int w = (int)(width * imageSize.Width);
                int h = (int)(height * imageSize.Height);

                float maxClassProb = 0;
                int bestClass = 0;
                for (int c = 0; c < 80; c++)
                {
                    float classProb = output[offset + 5 + c];
                    if (classProb > maxClassProb)
                    {
                        maxClassProb = classProb;
                        bestClass = c;
                    }
                }

                var finalConfidence = confidence * maxClassProb;
                if (finalConfidence >= _confidenceThreshold)
                {
                    detections.Add(new Detection
                    {
                        BoundingBox = new Rectangle(x, y, w, h),
                        Confidence = finalConfidence,
                        ElementType = MapClassToElementType(bestClass),
                        ClassId = bestClass
                    });
                }
            }

            return detections;
        }

        private List<Detection> ApplyNonMaximumSuppression(List<Detection> detections)
        {
            if (detections.Count <= 1) return detections;

            var sortedDetections = detections.OrderByDescending(d => d.Confidence).ToList();
            var keepDetections = new List<Detection>();

            for (int i = 0; i < sortedDetections.Count; i++)
            {
                var current = sortedDetections[i];
                bool shouldKeep = true;

                foreach (var kept in keepDetections)
                {
                    if (CalculateIoU(current.BoundingBox, kept.BoundingBox) > _nmsThreshold)
                    {
                        shouldKeep = false;
                        break;
                    }
                }

                if (shouldKeep)
                {
                    keepDetections.Add(current);
                }
            }

            return keepDetections;
        }

        private double CalculateIoU(Rectangle rect1, Rectangle rect2)
        {
            var intersection = Rectangle.Intersect(rect1, rect2);
            if (intersection.IsEmpty) return 0.0;

            var intersectionArea = intersection.Width * intersection.Height;
            var unionArea = (rect1.Width * rect1.Height) + (rect2.Width * rect2.Height) - intersectionArea;

            return unionArea > 0 ? (double)intersectionArea / unionArea : 0.0;
        }

        private ElementType MapClassToElementType(int classId)
        {
            return classId switch
            {
                0 => ElementType.Button,
                1 => ElementType.TextBox,
                2 => ElementType.Label,
                3 => ElementType.Dropdown,
                _ => ElementType.Unknown
            };
        }

        private IElementInfo CreateElementInfo(Detection detection)
        {
            var properties = new Dictionary<string, object>
            {
                ["AIModel"] = "ONNX Neural Network",
                ["ClassId"] = detection.ClassId,
                ["NMSApplied"] = true
            };

            return new ElementInfo(
                detection.BoundingBox,
                detection.ElementType,
                detection.Confidence,
                text: $"AI_{detection.ElementType}",
                properties: properties);
        }

        private Dictionary<string, object> CreateMetadata(TimeSpan processingTime, int elementCount)
        {
            return new Dictionary<string, object>
            {
                ["Strategy"] = Name,
                ["ModelType"] = "ONNX Neural Network",
                ["ProcessingTime"] = processingTime.TotalMilliseconds,
                ["ElementsDetected"] = elementCount,
                ["ConfidenceThreshold"] = _confidenceThreshold,
                ["NMSThreshold"] = _nmsThreshold
            };
        }

        private double CalculateOverallConfidence(List<IElementInfo> elements)
        {
            return elements.Any() ? elements.Average(e => e.Confidence) : 0.0;
        }

        private string GetDefaultModelPath()
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "ui_detection.onnx");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _modelManager?.Dispose();
                _disposed = true;
            }
        }
    }

    internal class Detection
    {
        public Rectangle BoundingBox { get; set; }
        public double Confidence { get; set; }
        public ElementType ElementType { get; set; }
        public int ClassId { get; set; }
    }
}