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
            _loggingService.LogVerbose($"Models directory: {_modelsDirectory}");
            _loggingService.LogVerbose($"Custom path: {customPath ?? "None"}");

            // Define model URLs based on ONNX Models repository structure
            _loggingService.LogVerbose("Looking up model URLs...");
            var modelUrls = GetModelUrls(modelName);
            if (modelUrls == null)
            {
                _loggingService.Log($"Model {modelName} not found in repository", LogLevel.Warning);
                _loggingService.LogVerbose("Available models: resnet50-v1-12, efficientnet-lite4-11, mobilenetv2-12, inception-v1-12, squeezenet1.1-12, densenet-12");
                return false;
            }

            _loggingService.LogVerbose($"ONNX URL: {modelUrls.OnnxUrl}");
            _loggingService.LogVerbose($"Labels URL: {modelUrls.LabelsUrl}");

            var targetPath = customPath ?? Path.Combine(_modelsDirectory, modelName);
            _loggingService.LogVerbose($"Target path: {targetPath}");
            
            if (!Directory.Exists(targetPath))
            {
                _loggingService.LogVerbose($"Creating directory: {targetPath}");
                Directory.CreateDirectory(targetPath);
            }
            else
            {
                _loggingService.LogVerbose($"Directory already exists: {targetPath}");
            }

            // Download ONNX model file
            var onnxPath = Path.Combine(targetPath, $"{modelName}.onnx");
            _loggingService.Log($"Downloading ONNX model to: {onnxPath}");
            await DownloadFileAsync(modelUrls.OnnxUrl, onnxPath);

            // Download labels file
            var labelsPath = Path.Combine(targetPath, $"{modelName}_labels.txt");
            _loggingService.Log($"Downloading labels to: {labelsPath}");
            await DownloadFileAsync(modelUrls.LabelsUrl, labelsPath);

            // Verify downloaded files
            var onnxFileInfo = new FileInfo(onnxPath);
            var labelsFileInfo = new FileInfo(labelsPath);
            _loggingService.LogVerbose($"Downloaded ONNX file size: {onnxFileInfo.Length:N0} bytes");
            _loggingService.LogVerbose($"Downloaded labels file size: {labelsFileInfo.Length:N0} bytes");

            _loggingService.Log($"Successfully downloaded model {modelName} to {targetPath}");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Download model {modelName}");
            _loggingService.Log($"Download failed for model '{modelName}': {ex.Message}", LogLevel.Error);
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
                }
                // Note: Other models temporarily disabled due to ONNX Models repository structure changes
                // The repository URLs are returning 404 errors, suggesting the structure has changed
                // These models will be re-enabled once the correct URLs are identified
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
            _loggingService.Log($"Validating downloaded model: {modelName}");
            _loggingService.LogVerbose($"Model path: {modelPath}");
            
            var onnxFile = Path.Combine(modelPath, $"{modelName}.onnx");
            var labelsFile = Path.Combine(modelPath, $"{modelName}_labels.txt");

            _loggingService.LogVerbose($"Checking ONNX file: {onnxFile}");
            if (!File.Exists(onnxFile))
            {
                _loggingService.Log($"ONNX file not found: {onnxFile}", LogLevel.Warning);
                _loggingService.LogVerbose($"Directory contents: {string.Join(", ", Directory.GetFiles(modelPath))}");
                return false;
            }

            _loggingService.LogVerbose($"Checking labels file: {labelsFile}");
            if (!File.Exists(labelsFile))
            {
                _loggingService.Log($"Labels file not found: {labelsFile}", LogLevel.Warning);
                _loggingService.LogVerbose($"Directory contents: {string.Join(", ", Directory.GetFiles(modelPath))}");
                return false;
            }

            // Get file sizes
            var onnxFileInfo = new FileInfo(onnxFile);
            var labelsFileInfo = new FileInfo(labelsFile);
            _loggingService.LogVerbose($"ONNX file size: {onnxFileInfo.Length:N0} bytes");
            _loggingService.LogVerbose($"Labels file size: {labelsFileInfo.Length:N0} bytes");

            // Test ONNX model loading
            _loggingService.LogVerbose("Testing ONNX model loading...");
            using var session = new Microsoft.ML.OnnxRuntime.InferenceSession(onnxFile);
            var inputMetadata = session.InputMetadata;
            var outputMetadata = session.OutputMetadata;
            
            _loggingService.LogVerbose($"ONNX model loaded successfully");
            _loggingService.LogVerbose($"Inputs: {string.Join(", ", inputMetadata.Keys)}");
            _loggingService.LogVerbose($"Outputs: {string.Join(", ", outputMetadata.Keys)}");

            // Test labels file
            _loggingService.LogVerbose("Reading labels file...");
            var labels = await File.ReadAllLinesAsync(labelsFile);
            _loggingService.LogVerbose($"Read {labels.Length} labels from file");

            _loggingService.Log($"Model validation successful: {modelName} with {labels.Length} labels", LogLevel.Debug);
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Validate downloaded model {modelName}");
            _loggingService.Log($"Validation failed for downloaded model '{modelName}': {ex.Message}", LogLevel.Error);
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
        // Note: Many ONNX Models repository URLs are currently returning 404 errors
        // This suggests the repository structure has changed or models have been moved
        // For now, we'll return null for models that don't have working URLs
        // and log a warning about the repository structure changes
        
        _loggingService.Log($"Looking up URLs for model: {modelName}");
        
        var result = modelName.ToLowerInvariant() switch
        {
            "resnet50-v1-12" => new ModelUrls(
                "https://github.com/onnx/models/raw/main/vision/classification/resnet/model/resnet50-v1-12.onnx",
                "https://github.com/onnx/models/raw/main/vision/classification/resnet/model/synset.txt"
            ),
            // Temporarily disable other models due to repository structure changes
            // "efficientnet-lite4-11" => new ModelUrls(
            //     "https://github.com/onnx/models/raw/main/vision/classification/efficientnet-lite4/model/efficientnet-lite4-11.onnx",
            //     "https://github.com/onnx/models/raw/main/vision/classification/efficientnet-lite4/model/synset.txt"
            // ),
            // "mobilenetv2-12" => new ModelUrls(
            //     "https://github.com/onnx/models/raw/main/vision/classification/mobilenet/model/mobilenetv2-12.onnx",
            //     "https://github.com/onnx/models/raw/main/vision/classification/mobilenet/model/synset.txt"
            // ),
            // "inception-v1-12" => new ModelUrls(
            //     "https://github.com/onnx/models/raw/main/vision/classification/inception_and_googlenet/inception_v1/model/inception-v1-12.onnx",
            //     "https://github.com/onnx/models/raw/main/vision/classification/inception_and_googlenet/inception_v1/model/synset.txt"
            // ),
            // "squeezenet1.1-12" => new ModelUrls(
            //     "https://github.com/onnx/models/raw/main/vision/classification/squeezenet/model/squeezenet1.1-12.onnx",
            //     "https://github.com/onnx/models/raw/main/vision/classification/squeezenet/model/synset.txt"
            // ),
            // "densenet-12" => new ModelUrls(
            //     "https://github.com/onnx/models/raw/main/vision/classification/densenet-121/model/densenet-12.onnx",
            //     "https://github.com/onnx/models/raw/main/vision/classification/densenet-121/model/synset.txt"
            // ),
            _ => null
        };
        
        if (result == null)
        {
            _loggingService.Log($"Model {modelName} not available for download (repository structure may have changed)", LogLevel.Warning);
        }
        
        return result;
    }

    private async Task DownloadFileAsync(string url, string localPath)
    {
        try
        {
            _loggingService.Log($"Starting download from {url}");
            _loggingService.LogVerbose($"Target local path: {localPath}");
            
            // Check if target directory exists
            var targetDir = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                _loggingService.LogVerbose($"Creating target directory: {targetDir}");
                Directory.CreateDirectory(targetDir);
            }
            
            _loggingService.LogVerbose("Sending HTTP request...");
            using var response = await _httpClient.GetAsync(url);
            
            _loggingService.LogVerbose($"HTTP response status: {response.StatusCode}");
            _loggingService.LogVerbose($"Content length: {response.Content.Headers.ContentLength ?? -1} bytes");
            
            response.EnsureSuccessStatusCode();
            
            _loggingService.LogVerbose("Creating local file...");
            using var fileStream = File.Create(localPath);
            
            _loggingService.LogVerbose("Copying content to file...");
            await response.Content.CopyToAsync(fileStream);
            
            // Verify file was created
            var fileInfo = new FileInfo(localPath);
            _loggingService.LogVerbose($"File created successfully, size: {fileInfo.Length:N0} bytes");
            
            _loggingService.Log($"Downloaded {Path.GetFileName(localPath)} successfully");
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Download file from {url}");
            _loggingService.Log($"Download failed for {url}: {ex.Message}", LogLevel.Error);
            
            // Clean up partial file if it exists
            if (File.Exists(localPath))
            {
                try
                {
                    File.Delete(localPath);
                    _loggingService.LogVerbose($"Cleaned up partial file: {localPath}");
                }
                catch (Exception cleanupEx)
                {
                    _loggingService.LogException(cleanupEx, $"Cleanup partial file {localPath}");
                }
            }
            
            throw;
        }
    }

    private record ModelUrls(string OnnxUrl, string LabelsUrl);
} 