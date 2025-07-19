using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using System.Text;

namespace ImageTagger.Services;

public class MLNetTaggingService : IImageTaggingService
{
    private readonly ILoggingService _loggingService;
    private readonly string _modelPath;
    private readonly string _labelsPath;
    private readonly int _imageWidth;
    private readonly int _imageHeight;
    private readonly int _maxTags;
    private readonly double _confidenceThreshold;

    public string ServiceName => $"ML.NET ({Path.GetFileNameWithoutExtension(_modelPath)})";

    public MLNetTaggingService(
        ILoggingService loggingService,
        string modelPath,
        string labelsPath,
        int imageWidth = 224,
        int imageHeight = 224,
        int maxTags = 3,
        double confidenceThreshold = 0.1)
    {
        _loggingService = loggingService;
        _modelPath = modelPath;
        _labelsPath = labelsPath;
        _imageWidth = imageWidth;
        _imageHeight = imageHeight;
        _maxTags = maxTags;
        _confidenceThreshold = confidenceThreshold;
    }

    public async Task<TaggingResult> TagImageAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new TaggingResult
        {
            ImagePath = imagePath,
            Method = ServiceName,
            Success = false
        };

        try
        {
            _loggingService.Log($"Starting ML.NET tagging for {imagePath}");
            _loggingService.LogVerbose($"Model path: {_modelPath}");
            _loggingService.LogVerbose($"Labels path: {_labelsPath}");
            _loggingService.LogVerbose($"Image dimensions: {_imageWidth}x{_imageHeight}");
            _loggingService.LogVerbose($"Max tags: {_maxTags}, Confidence threshold: {_confidenceThreshold}");

            // Verify model and labels exist
            if (!File.Exists(_modelPath))
            {
                throw new FileNotFoundException($"ONNX model not found: {_modelPath}");
            }

            if (!File.Exists(_labelsPath))
            {
                throw new FileNotFoundException($"Labels file not found: {_labelsPath}");
            }

            // Load labels
            var labels = await File.ReadAllLinesAsync(_labelsPath, cancellationToken);
            if (labels.Length == 0)
            {
                throw new InvalidOperationException("Labels file is empty");
            }

            _loggingService.Log($"Loaded {labels.Length} labels", LogLevel.Debug);

            // Run ML.NET inference
            _loggingService.LogVerbose("Starting ML.NET inference pipeline");
            var predictions = await Task.Run(() => RunMLNetInference(imagePath, labels), cancellationToken);
            _loggingService.LogVerbose($"ML.NET inference completed, got {predictions.Count} predictions");

            // Convert to TagResult objects
            result.Tags = predictions
                .Where(p => p.Score >= _confidenceThreshold)
                .Take(_maxTags)
                .Select(p => new TagResult
                {
                    Tag = p.Label,
                    Confidence = p.Score,
                    Source = ServiceName
                })
                .ToList();

            result.Success = true;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _loggingService.Log($"ML.NET generated {result.Tags.Count} tags successfully", LogLevel.Info);
            _loggingService.LogPerformance("ML.NET Tagging", result.ProcessingTime);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _loggingService.LogException(ex, "ML.NET Tagging");
            throw new ApplicationException($"ML.NET inference failed: {ex.Message}", ex);
        }

        return result;
    }

    public bool IsSupported(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return false;

        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" or ".tif";
    }

    private List<(string Label, double Score)> RunMLNetInference(string imagePath, string[] labels)
    {
        // Create MLContext
        var mlContext = new MLContext(seed: 1);

        // Define input data
        var data = new List<ImageInput> { new() { ImagePath = imagePath } };
        var imageData = mlContext.Data.LoadFromEnumerable(data);

        // Get ONNX output column name programmatically
        string outputColumnName = GetOnnxOutputColumnName(_modelPath);
        _loggingService.Log($"Detected ONNX output column name: {outputColumnName}", LogLevel.Debug);

        // Define pipeline
        var pipeline = mlContext.Transforms.LoadImages(
                outputColumnName: "data",
                imageFolder: Path.GetDirectoryName(imagePath),
                inputColumnName: nameof(ImageInput.ImagePath))
            .Append(mlContext.Transforms.ResizeImages(
                outputColumnName: "data",
                imageWidth: _imageWidth,
                imageHeight: _imageHeight,
                inputColumnName: "data"))
            .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "data"))
            .Append(mlContext.Transforms.ApplyOnnxModel(
                modelFile: _modelPath,
                outputColumnNames: new[] { outputColumnName },
                inputColumnNames: new[] { "data" }));

        // Fit and transform
        var model = pipeline.Fit(imageData);
        var predictionEngine = mlContext.Model.CreatePredictionEngine<ImageInput, ImagePrediction>(model);
        var prediction = predictionEngine.Predict(new ImageInput { ImagePath = imagePath });

        // Debug logging
        _loggingService.LogVerbose($"Prediction object type: {prediction.GetType().Name}");
        _loggingService.LogVerbose($"PredictedLabels is null: {prediction.PredictedLabels == null}");
        _loggingService.LogVerbose($"PredictedLabels length: {prediction.PredictedLabels?.Length ?? 0}");

        if (prediction.PredictedLabels == null)
        {
            throw new ApplicationException($"Model prediction is null. Output column: {outputColumnName}.");
        }

        if (prediction.PredictedLabels.Length == 0)
        {
            // Try to get the raw prediction data using reflection to debug
            var predictionType = prediction.GetType();
            var properties = predictionType.GetProperties();
            _loggingService.LogVerbose($"Prediction properties: {string.Join(", ", properties.Select(p => $"{p.Name}:{p.PropertyType.Name}"))}");
            
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(prediction);
                    if (value is Array arr)
                    {
                        _loggingService.LogVerbose($"Property {prop.Name} is array with length: {arr.Length}");
                        if (arr.Length > 0)
                        {
                            _loggingService.LogVerbose($"First few values: {string.Join(", ", arr.Cast<object>().Take(5))}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogVerbose($"Error reading property {prop.Name}: {ex.Message}");
                }
            }
            
            throw new ApplicationException($"Model returned empty predictions. Output column: {outputColumnName}.");
        }

        if (prediction.PredictedLabels.Length != labels.Length)
        {
            _loggingService.Log($"Warning: Prediction length ({prediction.PredictedLabels.Length}) doesn't match labels length ({labels.Length})", LogLevel.Warning);
        }

        // Get top predictions with confidence scores
        var predictions = prediction.PredictedLabels
            .Select((score, index) => (Label: index < labels.Length ? labels[index] : $"Unknown_{index}", Score: (double)score))
            .OrderByDescending(x => x.Score)
            .Take(_maxTags)
            .ToList();

        _loggingService.LogVerbose($"Generated {predictions.Count} predictions with scores: {string.Join(", ", predictions.Select(p => $"{p.Label}:{p.Score:F3}"))}");

        return predictions;
    }

    private string GetOnnxOutputColumnName(string modelPath)
    {
        try
        {
            using var session = new InferenceSession(modelPath);
            var outputName = session.OutputMetadata.Keys.First();
            _loggingService.Log($"ONNX output column name: {outputName}", LogLevel.Debug);
            return outputName;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetOnnxOutputColumnName");
            throw new ApplicationException($"Failed to get ONNX output column name: {ex.Message}", ex);
        }
    }

    private class ImageInput
    {
        public string ImagePath { get; set; } = string.Empty;
    }

    private class ImagePrediction
    {
        [ColumnName("resnetv17_dense0_fwd")]
        public float[] PredictedLabels { get; set; } = Array.Empty<float>();
    }
} 