using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Drawing;
using System.Linq;

namespace CitrixAI.Core.ML
{
    public interface IModelManager : IDisposable
    {
        bool LoadModel(string modelPath, string modelId = "default");
        float[] RunInference(Tensor<float> inputTensor, string modelId = "default");
        Tensor<float> PreprocessImage(Bitmap image, string modelId = "default");
        ModelMetadata GetModelInfo(string modelId = "default");
        bool IsModelLoaded(string modelId = "default");
    }

    public sealed class ModelManager : IModelManager
    {
        private readonly Dictionary<string, InferenceSession> _loadedModels;
        private readonly Dictionary<string, ModelMetadata> _modelMetadata;
        private bool _disposed;

        public ModelManager()
        {
            _loadedModels = new Dictionary<string, InferenceSession>();
            _modelMetadata = new Dictionary<string, ModelMetadata>();
        }

        public bool LoadModel(string modelPath, string modelId = "default")
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                throw new ArgumentException("Model path cannot be null or empty", nameof(modelPath));

            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

            try
            {
                if (!File.Exists(modelPath))
                {
                    return false;
                }

                var fileInfo = new FileInfo(modelPath);
                if (fileInfo.Length > 500 * 1024 * 1024)
                {
                    throw new InvalidOperationException($"Model file too large: {fileInfo.Length / (1024 * 1024)}MB");
                }

                var sessionOptions = new SessionOptions
                {
                    EnableCpuMemArena = true,
                    EnableMemoryPattern = true,
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED
                };

                var session = new InferenceSession(modelPath, sessionOptions);
                var metadata = ExtractModelMetadata(session);

                _loadedModels[modelId] = session;
                _modelMetadata[modelId] = metadata;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public float[] RunInference(Tensor<float> inputTensor, string modelId = "default")
        {
            if (inputTensor == null)
                throw new ArgumentNullException(nameof(inputTensor));

            if (!_loadedModels.ContainsKey(modelId))
                throw new InvalidOperationException($"Model '{modelId}' not loaded");

            var session = _loadedModels[modelId];
            var metadata = _modelMetadata[modelId];

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(metadata.InputName, inputTensor)
            };

            using (var results = session.Run(inputs))
            {
                var outputTensor = results.FirstOrDefault()?.AsTensor<float>();
                if (outputTensor == null)
                    throw new InvalidOperationException("No output tensor received from model");

                return outputTensor.ToArray();
            }
        }

        public Tensor<float> PreprocessImage(Bitmap image, string modelId = "default")
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            if (!_modelMetadata.ContainsKey(modelId))
                throw new InvalidOperationException($"Model metadata for '{modelId}' not found");

            var metadata = _modelMetadata[modelId];
            var targetSize = new Size(metadata.InputWidth, metadata.InputHeight);

            using (var resizedImage = new Bitmap(image, targetSize))
            {
                var tensor = new DenseTensor<float>(new[] { 1, 3, metadata.InputHeight, metadata.InputWidth });

                for (int y = 0; y < metadata.InputHeight; y++)
                {
                    for (int x = 0; x < metadata.InputWidth; x++)
                    {
                        var pixel = resizedImage.GetPixel(x, y);

                        tensor[0, 0, y, x] = pixel.R / 255.0f;
                        tensor[0, 1, y, x] = pixel.G / 255.0f;
                        tensor[0, 2, y, x] = pixel.B / 255.0f;
                    }
                }

                return tensor;
            }
        }

        public ModelMetadata GetModelInfo(string modelId = "default")
        {
            return _modelMetadata.ContainsKey(modelId) ? _modelMetadata[modelId] : null;
        }

        public bool IsModelLoaded(string modelId = "default")
        {
            return _loadedModels.ContainsKey(modelId);
        }

        private ModelMetadata ExtractModelMetadata(InferenceSession session)
        {
            var inputInfo = session.InputMetadata.FirstOrDefault();
            var outputInfo = session.OutputMetadata;

            if (inputInfo.Key == null)
                throw new InvalidOperationException("Model has no input information");

            var inputShape = inputInfo.Value.Dimensions.ToArray();

            if (inputShape.Length != 4)
                throw new InvalidOperationException($"Expected 4D input tensor, got {inputShape.Length}D");

            return new ModelMetadata
            {
                InputName = inputInfo.Key,
                InputShape = inputShape,
                InputWidth = inputShape[3],
                InputHeight = inputShape[2],
                OutputCount = outputInfo.Count,
                ModelType = "ObjectDetection"
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var session in _loadedModels.Values)
                {
                    session?.Dispose();
                }
                _loadedModels.Clear();
                _modelMetadata.Clear();
                _disposed = true;
            }
        }
    }

    public class ModelMetadata
    {
        public string InputName { get; set; }
        public int[] InputShape { get; set; }
        public int InputWidth { get; set; }
        public int InputHeight { get; set; }
        public int OutputCount { get; set; }
        public string ModelType { get; set; }
    }
}