using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using System.Text.Json;

namespace ImageTagger.Services;

public class ModelDownloaderService
{
    private readonly ILoggingService _loggingService;
    private readonly HttpClient _httpClient;
    private readonly string _modelsDirectory;

    public ModelDownloaderService(ILoggingService loggingService, string modelsDirectory = "models")
    {
        _loggingService = loggingService;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ImageTagger-Plus/1.0");
        _modelsDirectory = modelsDirectory;
        
        // Ensure models directory exists
        Directory.CreateDirectory(_modelsDirectory);
    }

    public async Task<bool> DownloadModelFromRepositoryAsync(string modelName, string? customPath = null)
    {
        try
        {
            _loggingService.Log($"Starting download of model: {modelName}");

            // Define model URLs based on ONNX Models repository structure
            var modelUrls = GetModelUrls(modelName);
            if (modelUrls == null)
            {
                _loggingService.Log($"Model {modelName} not found in repository", LogLevel.Warning);
                return false;
            }

            var targetPath = customPath ?? Path.Combine(_modelsDirectory, modelName);
            Directory.CreateDirectory(targetPath);

            // Download ONNX model file
            var onnxPath = Path.Combine(targetPath, $"{modelName}.onnx");
            await DownloadFileAsync(modelUrls.OnnxUrl, onnxPath);

            // Download labels file
            var labelsPath = Path.Combine(targetPath, $"{modelName}_labels.txt");
            await DownloadFileAsync(modelUrls.LabelsUrl, labelsPath);

            _loggingService.Log($"Successfully downloaded model {modelName} to {targetPath}");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Download model {modelName}");
            return false;
        }
    }

    public async Task<List<ModelInfo>> GetAvailableModelsFromRepositoryAsync()
    {
        try
        {
            _loggingService.Log("Fetching available models from ONNX Models repository");

            // This would typically fetch from GitHub API
            // For now, we'll return a curated list of popular models
            var availableModels = new List<ModelInfo>
            {
                new ModelInfo
                {
                    Name = "resnet50-v1-12",
                    DisplayName = "ResNet-50 v1.12",
                    Description = "ResNet-50 model for image classification with 1000 ImageNet classes",
                    Source = "ONNX Models Repository",
                    License = "MIT",
                    ImageWidth = 224,
                    ImageHeight = 224,
                    ConfidenceThreshold = 0.1,
                    MaxTags = 5,
                    Priority = 100
                },
                new ModelInfo
                {
                    Name = "efficientnet-lite4-11",
                    DisplayName = "EfficientNet-Lite4",
                    Description = "Lightweight EfficientNet model optimized for mobile and edge devices",
                    Source = "ONNX Models Repository",
                    License = "MIT",
                    ImageWidth = 224,
                    ImageHeight = 224,
                    ConfidenceThreshold = 0.1,
                    MaxTags = 5,
                    Priority = 95
                },
                new ModelInfo
                {
                    Name = "mobilenetv2-12",
                    DisplayName = "MobileNet v2",
                    Description = "MobileNet v2 model optimized for mobile devices with good accuracy/speed trade-off",
                    Source = "ONNX Models Repository",
                    License = "MIT",
                    ImageWidth = 224,
                    ImageHeight = 224,
                    ConfidenceThreshold = 0.1,
                    MaxTags = 5,
                    Priority = 90
                },
                new ModelInfo
                {
                    Name = "inception-v1-12",
                    DisplayName = "Inception v1",
                    Description = "Inception v1 model for image classification",
                    Source = "ONNX Models Repository",
                    License = "MIT",
                    ImageWidth = 224,
                    ImageHeight = 224,
                    ConfidenceThreshold = 0.1,
                    MaxTags = 5,
                    Priority = 85
                },
                new ModelInfo
                {
                    Name = "squeezenet1.1-12",
                    DisplayName = "SqueezeNet 1.1",
                    Description = "Lightweight SqueezeNet model with high accuracy and small size",
                    Source = "ONNX Models Repository",
                    License = "MIT",
                    ImageWidth = 224,
                    ImageHeight = 224,
                    ConfidenceThreshold = 0.1,
                    MaxTags = 5,
                    Priority = 80
                },
                new ModelInfo
                {
                    Name = "densenet-12",
                    DisplayName = "DenseNet-121",
                    Description = "DenseNet model with dense connections for better feature reuse",
                    Source = "ONNX Models Repository",
                    License = "MIT",
                    ImageWidth = 224,
                    ImageHeight = 224,
                    ConfidenceThreshold = 0.1,
                    MaxTags = 5,
                    Priority = 75
                }
            };

            _loggingService.Log($"Found {availableModels.Count} available models");
            return availableModels;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetAvailableModelsFromRepository");
            return new List<ModelInfo>();
        }
    }

    public async Task<bool> ValidateDownloadedModelAsync(string modelName, string modelPath)
    {
        try
        {
            var onnxFile = Path.Combine(modelPath, $"{modelName}.onnx");
            var labelsFile = Path.Combine(modelPath, $"{modelName}_labels.txt");

            if (!File.Exists(onnxFile))
            {
                _loggingService.Log($"ONNX file not found: {onnxFile}", LogLevel.Warning);
                return false;
            }

            if (!File.Exists(labelsFile))
            {
                _loggingService.Log($"Labels file not found: {labelsFile}", LogLevel.Warning);
                return false;
            }

            // Test ONNX model loading
            using var session = new Microsoft.ML.OnnxRuntime.InferenceSession(onnxFile);
            var labels = await File.ReadAllLinesAsync(labelsFile);

            _loggingService.Log($"Model validation successful: {modelName} with {labels.Length} labels", LogLevel.Debug);
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Validate downloaded model {modelName}");
            return false;
        }
    }

    public async Task<ModelInfo> CreateModelInfoFromDownloadedAsync(string modelName, string modelPath)
    {
        var availableModels = await GetAvailableModelsFromRepositoryAsync();
        var template = availableModels.FirstOrDefault(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));

        if (template == null)
        {
            throw new ArgumentException($"Template for model {modelName} not found");
        }

        var modelInfo = new ModelInfo
        {
            Name = modelName,
            DisplayName = template.DisplayName,
            ModelPath = Path.Combine(modelPath, $"{modelName}.onnx"),
            LabelsPath = Path.Combine(modelPath, $"{modelName}_labels.txt"),
            ImageWidth = template.ImageWidth,
            ImageHeight = template.ImageHeight,
            ConfidenceThreshold = template.ConfidenceThreshold,
            MaxTags = template.MaxTags,
            Description = template.Description,
            Source = template.Source,
            License = template.License,
            Priority = template.Priority,
            IsEnabled = true
        };

        return modelInfo;
    }

    private ModelUrls? GetModelUrls(string modelName)
    {
        // This would typically fetch from GitHub API or a model registry
        // For now, we'll return hardcoded URLs for known models
        return modelName.ToLowerInvariant() switch
        {
            "resnet50-v1-12" => new ModelUrls(
                "https://github.com/onnx/models/raw/main/vision/classification/resnet/model/resnet50-v1-12.onnx",
                "https://github.com/onnx/models/raw/main/vision/classification/resnet/model/synset.txt"
            ),
            "efficientnet-lite4-11" => new ModelUrls(
                "https://github.com/onnx/models/raw/main/vision/classification/efficientnet-lite4/model/efficientnet-lite4-11.onnx",
                "https://github.com/onnx/models/raw/main/vision/classification/efficientnet-lite4/model/synset.txt"
            ),
            "mobilenetv2-12" => new ModelUrls(
                "https://github.com/onnx/models/raw/main/vision/classification/mobilenet/model/mobilenetv2-12.onnx",
                "https://github.com/onnx/models/raw/main/vision/classification/mobilenet/model/synset.txt"
            ),
            "inception-v1-12" => new ModelUrls(
                "https://github.com/onnx/models/raw/main/vision/classification/inception_and_googlenet/inception_v1/model/inception-v1-12.onnx",
                "https://github.com/onnx/models/raw/main/vision/classification/inception_and_googlenet/inception_v1/model/synset.txt"
            ),
            "squeezenet1.1-12" => new ModelUrls(
                "https://github.com/onnx/models/raw/main/vision/classification/squeezenet/model/squeezenet1.1-12.onnx",
                "https://github.com/onnx/models/raw/main/vision/classification/squeezenet/model/synset.txt"
            ),
            "densenet-12" => new ModelUrls(
                "https://github.com/onnx/models/raw/main/vision/classification/densenet-121/model/densenet-12.onnx",
                "https://github.com/onnx/models/raw/main/vision/classification/densenet-121/model/synset.txt"
            ),
            _ => null
        };
    }

    private async Task DownloadFileAsync(string url, string localPath)
    {
        try
        {
            _loggingService.Log($"Downloading {url} to {localPath}");
            
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            using var fileStream = File.Create(localPath);
            await response.Content.CopyToAsync(fileStream);
            
            _loggingService.Log($"Downloaded {Path.GetFileName(localPath)} successfully");
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Download file from {url}");
            throw;
        }
    }

    private record ModelUrls(string OnnxUrl, string LabelsUrl);
} 