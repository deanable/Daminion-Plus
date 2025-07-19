using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using System.Text;

namespace ImageTagger.Services;

public class EnhancedMLNetTaggingService : IImageTaggingService
{
    private readonly ILoggingService _loggingService;
    private readonly IModelManager _modelManager;
    private readonly Dictionary<string, InferenceSession> _modelSessions;
    private readonly Dictionary<string, string[]> _modelLabels;
    private readonly object _sessionLock = new();

    public string ServiceName => "Enhanced ML.NET";

    public EnhancedMLNetTaggingService(ILoggingService loggingService, IModelManager modelManager)
    {
        _loggingService = loggingService;
        _modelManager = modelManager;
        _modelSessions = new Dictionary<string, InferenceSession>();
        _modelLabels = new Dictionary<string, string[]>();
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
            _loggingService.Log($"Starting Enhanced ML.NET tagging for {imagePath}");

            // Get the default model
            var model = await _modelManager.GetDefaultModelAsync();
            if (model == null)
            {
                throw new InvalidOperationException("No default model configured");
            }

            _loggingService.LogVerbose($"Using model: {model.DisplayName} ({model.Name})");

            // Validate model
            if (!await _modelManager.ValidateModelAsync(model))
            {
                throw new InvalidOperationException($"Model validation failed for {model.Name}");
            }

            // Load model session and labels if not already loaded
            await EnsureModelLoadedAsync(model, cancellationToken);

            // Run inference
            var predictions = await Task.Run(() => RunInference(imagePath, model), cancellationToken);

            // Convert to TagResult objects
            result.Tags = predictions
                .Where(p => p.Score >= model.ConfidenceThreshold)
                .Take(model.MaxTags)
                .Select(p => new TagResult
                {
                    Tag = p.Label,
                    Confidence = p.Score,
                    Source = $"{ServiceName} ({model.DisplayName})"
                })
                .ToList();

            result.Success = true;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _loggingService.Log($"Enhanced ML.NET generated {result.Tags.Count} tags using {model.DisplayName}", LogLevel.Info);
            _loggingService.LogPerformance($"Enhanced ML.NET Tagging ({model.Name})", result.ProcessingTime);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _loggingService.LogException(ex, "Enhanced ML.NET Tagging");
            throw new ApplicationException($"Enhanced ML.NET inference failed: {ex.Message}", ex);
        }

        return result;
    }

    public async Task<TaggingResult> TagImageWithModelAsync(string imagePath, string modelName, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new TaggingResult
        {
            ImagePath = imagePath,
            Method = $"{ServiceName} ({modelName})",
            Success = false
        };

        try
        {
            _loggingService.Log($"Starting Enhanced ML.NET tagging with model {modelName} for {imagePath}");

            // Get the specified model
            var model = await _modelManager.GetModelAsync(modelName);
            if (model == null)
            {
                throw new ArgumentException($"Model '{modelName}' not found");
            }

            if (!model.IsEnabled)
            {
                throw new InvalidOperationException($"Model '{modelName}' is disabled");
            }

            // Validate model
            if (!await _modelManager.ValidateModelAsync(model))
            {
                throw new InvalidOperationException($"Model validation failed for {model.Name}");
            }

            // Load model session and labels if not already loaded
            await EnsureModelLoadedAsync(model, cancellationToken);

            // Run inference
            var predictions = await Task.Run(() => RunInference(imagePath, model), cancellationToken);

            // Convert to TagResult objects
            result.Tags = predictions
                .Where(p => p.Score >= model.ConfidenceThreshold)
                .Take(model.MaxTags)
                .Select(p => new TagResult
                {
                    Tag = p.Label,
                    Confidence = p.Score,
                    Source = $"{ServiceName} ({model.DisplayName})"
                })
                .ToList();

            result.Success = true;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _loggingService.Log($"Enhanced ML.NET generated {result.Tags.Count} tags using {model.DisplayName}", LogLevel.Info);
            _loggingService.LogPerformance($"Enhanced ML.NET Tagging ({model.Name})", result.ProcessingTime);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _loggingService.LogException(ex, $"Enhanced ML.NET Tagging with {modelName}");
            throw new ApplicationException($"Enhanced ML.NET inference failed: {ex.Message}", ex);
        }

        return result;
    }

    public async Task<TaggingResult> TagImageWithMultipleModelsAsync(string imagePath, List<string> modelNames, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new TaggingResult
        {
            ImagePath = imagePath,
            Method = $"{ServiceName} (Multi-Model)",
            Success = false
        };

        try
        {
            _loggingService.Log($"Starting Enhanced ML.NET tagging with {modelNames.Count} models for {imagePath}");

            var allTags = new List<TagResult>();

            foreach (var modelName in modelNames)
            {
                try
                {
                    var modelResult = await TagImageWithModelAsync(imagePath, modelName, cancellationToken);
                    if (modelResult.Success)
                    {
                        allTags.AddRange(modelResult.Tags);
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.Log($"Failed to tag with model {modelName}: {ex.Message}", LogLevel.Warning);
                }
            }

            // Remove duplicates and sort by confidence
            result.Tags = allTags
                .GroupBy(t => t.Tag)
                .Select(g => g.OrderByDescending(t => t.Confidence).First())
                .OrderByDescending(t => t.Confidence)
                .ToList();

            result.Success = true;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _loggingService.Log($"Enhanced ML.NET generated {result.Tags.Count} unique tags using {modelNames.Count} models", LogLevel.Info);
            _loggingService.LogPerformance("Enhanced ML.NET Multi-Model Tagging", result.ProcessingTime);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _loggingService.LogException(ex, "Enhanced ML.NET Multi-Model Tagging");
            throw new ApplicationException($"Enhanced ML.NET multi-model inference failed: {ex.Message}", ex);
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

    private async Task EnsureModelLoadedAsync(ModelInfo model, CancellationToken cancellationToken)
    {
        lock (_sessionLock)
        {
            if (_modelSessions.ContainsKey(model.Name) && _modelLabels.ContainsKey(model.Name))
                return;
        }

        _loggingService.Log($"Loading model: {model.Name}");

        // Load ONNX session
        var session = new InferenceSession(model.ModelPath);
        
        // Load labels
        var labels = await File.ReadAllLinesAsync(model.LabelsPath, cancellationToken);
        if (labels.Length == 0)
        {
            throw new InvalidOperationException($"Labels file is empty for model {model.Name}");
        }

        lock (_sessionLock)
        {
            _modelSessions[model.Name] = session;
            _modelLabels[model.Name] = labels;
        }

        _loggingService.Log($"Model loaded successfully: {model.Name} with {labels.Length} labels", LogLevel.Debug);
    }

    private List<(string Label, double Score)> RunInference(string imagePath, ModelInfo model)
    {
        lock (_sessionLock)
        {
            if (!_modelSessions.ContainsKey(model.Name) || !_modelLabels.ContainsKey(model.Name))
            {
                throw new InvalidOperationException($"Model {model.Name} not loaded");
            }

            var session = _modelSessions[model.Name];
            var labels = _modelLabels[model.Name];

            // Create MLContext
            var mlContext = new MLContext(seed: 1);

            // Define input data
            var data = new List<ImageInput> { new() { ImagePath = imagePath } };
            var imageData = mlContext.Data.LoadFromEnumerable(data);

            // Get ONNX output column name
            string outputColumnName = GetOnnxOutputColumnName(session);
            _loggingService.Log($"Detected ONNX output column name: {outputColumnName}", LogLevel.Debug);

            // Define pipeline
            var pipeline = mlContext.Transforms.LoadImages(
                    outputColumnName: "data",
                    imageFolder: Path.GetDirectoryName(imagePath),
                    inputColumnName: nameof(ImageInput.ImagePath))
                .Append(mlContext.Transforms.ResizeImages(
                    outputColumnName: "data",
                    imageWidth: model.ImageWidth,
                    imageHeight: model.ImageHeight,
                    inputColumnName: "data"))
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "data"))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    modelFile: model.ModelPath,
                    outputColumnNames: new[] { outputColumnName },
                    inputColumnNames: new[] { "data" }));

            // Fit and transform
            var mlModel = pipeline.Fit(imageData);
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ImageInput, ImagePrediction>(mlModel);
            var prediction = predictionEngine.Predict(new ImageInput { ImagePath = imagePath });

            if (prediction.PredictedLabels == null || prediction.PredictedLabels.Length == 0)
            {
                throw new ApplicationException($"Model {model.Name} returned empty predictions");
            }

            // Get top predictions with confidence scores
            var predictions = prediction.PredictedLabels
                .Select((score, index) => (Label: index < labels.Length ? labels[index] : $"Unknown_{index}", Score: (double)score))
                .OrderByDescending(x => x.Score)
                .ToList();

            _loggingService.LogVerbose($"Generated {predictions.Count} predictions for model {model.Name}");

            return predictions;
        }
    }

    private string GetOnnxOutputColumnName(InferenceSession session)
    {
        try
        {
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

    public void Dispose()
    {
        lock (_sessionLock)
        {
            foreach (var session in _modelSessions.Values)
            {
                session?.Dispose();
            }
            _modelSessions.Clear();
            _modelLabels.Clear();
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