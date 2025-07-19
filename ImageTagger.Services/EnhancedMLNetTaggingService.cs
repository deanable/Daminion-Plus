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
        try
        {
            lock (_sessionLock)
            {
                if (_modelSessions.ContainsKey(model.Name) && _modelLabels.ContainsKey(model.Name))
                {
                    _loggingService.LogVerbose($"Model {model.Name} already loaded, skipping");
                    return;
                }
            }

            _loggingService.Log($"Loading model: {model.Name}");
            _loggingService.LogVerbose($"Model path: {model.ModelPath}");
            _loggingService.LogVerbose($"Labels path: {model.LabelsPath}");

            // Verify files exist before loading
            if (!File.Exists(model.ModelPath))
            {
                throw new FileNotFoundException($"Model file not found: {model.ModelPath}");
            }

            if (!File.Exists(model.LabelsPath))
            {
                throw new FileNotFoundException($"Labels file not found: {model.LabelsPath}");
            }

            // Get file sizes for debugging
            var modelFileInfo = new FileInfo(model.ModelPath);
            var labelsFileInfo = new FileInfo(model.LabelsPath);
            _loggingService.LogVerbose($"Model file size: {modelFileInfo.Length:N0} bytes");
            _loggingService.LogVerbose($"Labels file size: {labelsFileInfo.Length:N0} bytes");

            // Load ONNX session
            _loggingService.LogVerbose("Creating ONNX inference session...");
            var session = new InferenceSession(model.ModelPath);
            
            var inputMetadata = session.InputMetadata;
            var outputMetadata = session.OutputMetadata;
            _loggingService.LogVerbose($"ONNX session created successfully");
            _loggingService.LogVerbose($"Inputs: {string.Join(", ", inputMetadata.Keys)}");
            _loggingService.LogVerbose($"Outputs: {string.Join(", ", outputMetadata.Keys)}");
            
            // Load labels
            _loggingService.LogVerbose("Reading labels file...");
            var labels = await File.ReadAllLinesAsync(model.LabelsPath, cancellationToken);
            if (labels.Length == 0)
            {
                throw new InvalidOperationException($"Labels file is empty for model {model.Name}");
            }

            _loggingService.LogVerbose($"Read {labels.Length} labels from file");

            lock (_sessionLock)
            {
                _modelSessions[model.Name] = session;
                _modelLabels[model.Name] = labels;
                _loggingService.LogVerbose($"Model {model.Name} added to cache");
            }

            _loggingService.Log($"Model loaded successfully: {model.Name} with {labels.Length} labels", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"EnsureModelLoadedAsync for {model.Name}");
            _loggingService.Log($"Failed to load model '{model.Name}': {ex.Message}", LogLevel.Error);
            throw;
        }
    }

    private List<(string Label, double Score)> RunInference(string imagePath, ModelInfo model)
    {
        try
        {
            lock (_sessionLock)
            {
                if (!_modelSessions.ContainsKey(model.Name) || !_modelLabels.ContainsKey(model.Name))
                {
                    throw new InvalidOperationException($"Model {model.Name} not loaded");
                }

                var session = _modelSessions[model.Name];
                var labels = _modelLabels[model.Name];

                _loggingService.LogVerbose($"Starting inference for model: {model.Name}");
                _loggingService.LogVerbose($"Image path: {imagePath}");
                _loggingService.LogVerbose($"Image dimensions: {model.ImageWidth}x{model.ImageHeight}");
                _loggingService.LogVerbose($"Labels count: {labels.Length}");

                // Create MLContext
                var mlContext = new MLContext(seed: 1);

                // Define input data
                var data = new List<ImageInput> { new() { ImagePath = imagePath } };
                var imageData = mlContext.Data.LoadFromEnumerable(data);
                _loggingService.LogVerbose("Created ML.NET data view");

                // Get ONNX output column name
                string outputColumnName = GetOnnxOutputColumnName(session);
                _loggingService.LogVerbose($"Using ONNX output column: {outputColumnName}");

                // Define pipeline
                _loggingService.LogVerbose("Building ML.NET pipeline...");
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
                _loggingService.LogVerbose("Fitting ML.NET pipeline...");
                var mlModel = pipeline.Fit(imageData);
                _loggingService.LogVerbose("Creating prediction engine...");
                var predictionEngine = mlContext.Model.CreatePredictionEngine<ImageInput, ImagePrediction>(mlModel);
                
                _loggingService.LogVerbose("Running prediction...");
                var prediction = predictionEngine.Predict(new ImageInput { ImagePath = imagePath });

                if (prediction.PredictedLabels == null || prediction.PredictedLabels.Length == 0)
                {
                    throw new ApplicationException($"Model {model.Name} returned empty predictions");
                }

                _loggingService.LogVerbose($"Raw prediction length: {prediction.PredictedLabels.Length}");

                // Get top predictions with confidence scores
                var predictions = prediction.PredictedLabels
                    .Select((score, index) => (Label: index < labels.Length ? labels[index] : $"Unknown_{index}", Score: (double)score))
                    .OrderByDescending(x => x.Score)
                    .ToList();

                _loggingService.LogVerbose($"Generated {predictions.Count} predictions for model {model.Name}");
                
                // Log top predictions for debugging
                var topPredictions = predictions.Take(5).ToList();
                foreach (var pred in topPredictions)
                {
                    _loggingService.LogVerbose($"  - {pred.Label}: {pred.Score:F4}");
                }

                return predictions;
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"RunInference for model {model.Name}");
            _loggingService.Log($"Inference failed for model '{model.Name}': {ex.Message}", LogLevel.Error);
            throw;
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