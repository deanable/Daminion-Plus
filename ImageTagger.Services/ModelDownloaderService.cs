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

            // Get the Hugging Face model ID from the model info
            var availableModels = await GetAvailableModelsFromRepositoryAsync();
            var templateModel = availableModels.FirstOrDefault(m => m.Name == modelName);
            
            if (templateModel == null)
            {
                _loggingService.Log($"Model {modelName} not found in available models", LogLevel.Warning);
                return false;
            }

            // Check if it has a Hugging Face ID
            if (!templateModel.AdditionalProperties.ContainsKey("huggingface_id"))
            {
                _loggingService.Log($"Model {modelName} does not have a Hugging Face ID", LogLevel.Warning);
                return false;
            }

            var huggingfaceId = templateModel.AdditionalProperties["huggingface_id"].ToString();
            if (string.IsNullOrEmpty(huggingfaceId))
            {
                _loggingService.Log($"Invalid Hugging Face ID for model {modelName}", LogLevel.Warning);
                return false;
            }

            _loggingService.LogVerbose($"Hugging Face ID: {huggingfaceId}");

            // Use Hugging Face service to download the model
            var hfService = new HuggingFaceModelService(_loggingService, _modelsDirectory);
            var success = await hfService.DownloadModelAsync(huggingfaceId, customPath);

            if (success)
            {
                _loggingService.Log($"Successfully downloaded Hugging Face model {huggingfaceId} for {modelName}");
            }
            else
            {
                _loggingService.Log($"Failed to download Hugging Face model {huggingfaceId} for {modelName}", LogLevel.Error);
            }

            return success;
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
            _loggingService.Log("Fetching available models from Hugging Face Hub");

            // Use Hugging Face API to get available ONNX models
            var hfService = new HuggingFaceModelService(_loggingService, _modelsDirectory);
            var availableModels = await hfService.GetAvailableModelsAsync("onnx image classification", 20);

            // Add some known ONNX models that are available
            var popularModels = new List<ModelInfo>
            {
                new ModelInfo
                {
                    Name = "onnx-resnet50",
                    DisplayName = "ONNX ResNet-50",
                    Description = "ResNet-50 model in ONNX format for image classification",
                    Source = "Hugging Face Hub",
                    License = "MIT",
                    ImageWidth = 224,
                    ImageHeight = 224,
                    ConfidenceThreshold = 0.1,
                    MaxTags = 5,
                    Priority = 100,
                    AdditionalProperties = new Dictionary<string, object>
                    {
                        ["huggingface_id"] = "onnx/resnet50",
                        ["recommended"] = true
                    }
                },
                new ModelInfo
                {
                    Name = "microsoft-resnet50-onnx",
                    DisplayName = "Microsoft ResNet-50 ONNX",
                    Description = "Microsoft ResNet-50 model converted to ONNX format",
                    Source = "Hugging Face Hub",
                    License = "MIT",
                    ImageWidth = 224,
                    ImageHeight = 224,
                    ConfidenceThreshold = 0.1,
                    MaxTags = 5,
                    Priority = 95,
                    AdditionalProperties = new Dictionary<string, object>
                    {
                        ["huggingface_id"] = "microsoft/resnet50-onnx",
                        ["recommended"] = true
                    }
                }
            };

            // Combine Hugging Face models with popular curated models
            availableModels.AddRange(popularModels);

            _loggingService.Log($"Found {availableModels.Count} available models from Hugging Face Hub");
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
            
            // For Hugging Face models, we need to find the actual files
            var onnxFile = Directory.GetFiles(modelPath, "*.onnx").FirstOrDefault();
            var labelsFile = Directory.GetFiles(modelPath, "*.txt").FirstOrDefault();

            if (string.IsNullOrEmpty(onnxFile))
            {
                _loggingService.Log($"No ONNX file found in {modelPath}", LogLevel.Warning);
                _loggingService.LogVerbose($"Directory contents: {string.Join(", ", Directory.GetFiles(modelPath))}");
                return false;
            }

            if (string.IsNullOrEmpty(labelsFile))
            {
                _loggingService.Log($"No labels file found in {modelPath}", LogLevel.Warning);
                _loggingService.LogVerbose($"Directory contents: {string.Join(", ", Directory.GetFiles(modelPath))}");
                return false;
            }

            _loggingService.LogVerbose($"Found ONNX file: {onnxFile}");
            _loggingService.LogVerbose($"Found labels file: {labelsFile}");

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
        try
        {
            _loggingService.Log($"Creating model info for downloaded model: {modelName}");
            
            var availableModels = await GetAvailableModelsFromRepositoryAsync();
            var template = availableModels.FirstOrDefault(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));

            if (template == null)
            {
                _loggingService.Log($"Template for model {modelName} not found, creating basic info", LogLevel.Warning);
                
                // Create basic model info for downloaded model
                var basicModelInfo = new ModelInfo
                {
                    Name = modelName,
                    DisplayName = modelName,
                    ModelPath = Path.Combine(modelPath, Directory.GetFiles(modelPath, "*.onnx").FirstOrDefault() ?? ""),
                    LabelsPath = Path.Combine(modelPath, Directory.GetFiles(modelPath, "*.txt").FirstOrDefault() ?? ""),
                    ImageWidth = 224,
                    ImageHeight = 224,
                    ConfidenceThreshold = 0.1,
                    MaxTags = 5,
                    Description = $"Downloaded model: {modelName}",
                    Source = "Hugging Face Hub",
                    License = "Unknown",
                    Priority = 100,
                    IsEnabled = true
                };
                
                return basicModelInfo;
            }

            // Use template info but update paths to actual downloaded files
            var onnxFile = Directory.GetFiles(modelPath, "*.onnx").FirstOrDefault();
            var labelsFile = Directory.GetFiles(modelPath, "*.txt").FirstOrDefault();
            
            var modelInfo = new ModelInfo
            {
                Name = modelName,
                DisplayName = template.DisplayName,
                ModelPath = onnxFile ?? "",
                LabelsPath = labelsFile ?? "",
                ImageWidth = template.ImageWidth,
                ImageHeight = template.ImageHeight,
                ConfidenceThreshold = template.ConfidenceThreshold,
                MaxTags = template.MaxTags,
                Description = template.Description,
                Source = template.Source,
                License = template.License,
                Priority = template.Priority,
                IsEnabled = true,
                AdditionalProperties = template.AdditionalProperties
            };

            _loggingService.Log($"Created model info for {modelName} with ONNX: {onnxFile}, Labels: {labelsFile}");
            return modelInfo;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"CreateModelInfoFromDownloadedAsync for {modelName}");
            throw;
        }
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