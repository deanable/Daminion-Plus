using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using System.Text.Json;
using System.Text;

namespace ImageTagger.Services;

public class HuggingFaceModelService
{
    private readonly ILoggingService _loggingService;
    private readonly HttpClient _httpClient;
    private readonly string _modelsDirectory;
    private const string HF_API_BASE = "https://huggingface.co/api";

    public HuggingFaceModelService(ILoggingService loggingService, string modelsDirectory = "models")
    {
        _loggingService = loggingService;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ImageTagger-Plus/1.0");
        _modelsDirectory = modelsDirectory;
        
        // Ensure models directory exists
        Directory.CreateDirectory(_modelsDirectory);
    }

    public async Task<List<ModelInfo>> GetAvailableModelsAsync(string? search = null, int limit = 50)
    {
        try
        {
            _loggingService.Log("Fetching available models from Hugging Face Hub");
            
            var url = $"{HF_API_BASE}/models";
            var parameters = new List<string>();
            
            if (!string.IsNullOrEmpty(search))
            {
                parameters.Add($"search={Uri.EscapeDataString(search)}");
            }
            
            // Filter for computer vision models
            parameters.Add("filter=task_categories:image-classification");
            parameters.Add($"limit={limit}");
            parameters.Add("sort=downloads");
            parameters.Add("direction=-1");
            
            if (parameters.Count > 0)
            {
                url += "?" + string.Join("&", parameters);
            }
            
            _loggingService.LogVerbose($"API URL: {url}");
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var models = JsonSerializer.Deserialize<List<HuggingFaceModel>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<HuggingFaceModel>();
            
            _loggingService.Log($"Found {models.Count} models from Hugging Face Hub");
            
            // Convert to our ModelInfo format
            var availableModels = new List<ModelInfo>();
            foreach (var model in models)
            {
                try
                {
                    var modelInfo = new ModelInfo
                    {
                        Name = model.Id?.Replace("/", "-") ?? model.Id ?? "unknown",
                        DisplayName = model.Id ?? "Unknown Model",
                        Description = model.Description ?? "No description available",
                        Source = "Hugging Face Hub",
                        License = model.License ?? "Unknown",
                        ImageWidth = 224, // Default for most vision models
                        ImageHeight = 224,
                        ConfidenceThreshold = 0.1,
                        MaxTags = 5,
                        Priority = 100 - (availableModels.Count * 5), // Prioritize by order
                        IsEnabled = false, // Start disabled until downloaded
                        AdditionalProperties = new Dictionary<string, object>
                        {
                            ["huggingface_id"] = model.Id ?? "",
                            ["downloads"] = model.Downloads ?? 0,
                            ["likes"] = model.Likes ?? 0,
                            ["tags"] = model.Tags ?? new List<string>()
                        }
                    };
                    
                    availableModels.Add(modelInfo);
                    _loggingService.LogVerbose($"Added model: {modelInfo.DisplayName} (Downloads: {model.Downloads})");
                }
                catch (Exception ex)
                {
                    _loggingService.LogException(ex, $"Convert HuggingFace model {model.Id}");
                }
            }
            
            return availableModels;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetAvailableModelsAsync from Hugging Face");
            return new List<ModelInfo>();
        }
    }

    public async Task<bool> DownloadModelAsync(string modelId, string? customPath = null)
    {
        try
        {
            _loggingService.Log($"Starting download of Hugging Face model: {modelId}");
            
            // Get model info first
            var modelInfo = await GetModelInfoAsync(modelId);
            if (modelInfo == null)
            {
                _loggingService.Log($"Model {modelId} not found on Hugging Face Hub", LogLevel.Warning);
                return false;
            }
            
            var targetPath = customPath ?? Path.Combine(_modelsDirectory, modelId.Replace("/", "-"));
            _loggingService.LogVerbose($"Target path: {targetPath}");
            
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
            
            // Download model files
            var modelFiles = await GetModelFilesAsync(modelId);
            if (modelFiles == null || !modelFiles.Any())
            {
                _loggingService.Log($"No files found for model {modelId}", LogLevel.Warning);
                return false;
            }
            
            // Find ONNX model file
            var onnxFile = modelFiles.FirstOrDefault(f => f.EndsWith(".onnx"));
            if (string.IsNullOrEmpty(onnxFile))
            {
                _loggingService.Log($"No ONNX file found for model {modelId}", LogLevel.Warning);
                _loggingService.LogVerbose($"Available files: {string.Join(", ", modelFiles)}");
                return false;
            }
            
            // Download ONNX model
            var onnxPath = Path.Combine(targetPath, Path.GetFileName(onnxFile));
            await DownloadFileAsync($"{HF_API_BASE}/models/{modelId}/resolve/main/{onnxFile}", onnxPath);
            
            // Look for labels file
            var labelsFile = modelFiles.FirstOrDefault(f => f.Contains("labels") || f.Contains("classes") || f.EndsWith(".txt"));
            if (!string.IsNullOrEmpty(labelsFile))
            {
                var labelsPath = Path.Combine(targetPath, Path.GetFileName(labelsFile));
                await DownloadFileAsync($"{HF_API_BASE}/models/{modelId}/resolve/main/{labelsFile}", labelsPath);
            }
            else
            {
                // Create a basic labels file if none exists
                var labelsPath = Path.Combine(targetPath, "labels.txt");
                await CreateBasicLabelsFileAsync(labelsPath);
            }
            
            _loggingService.Log($"Successfully downloaded Hugging Face model {modelId} to {targetPath}");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Download Hugging Face model {modelId}");
            return false;
        }
    }

    public async Task<HuggingFaceModel?> GetModelInfoAsync(string modelId)
    {
        try
        {
            _loggingService.LogVerbose($"Getting model info for: {modelId}");
            
            var url = $"{HF_API_BASE}/models/{modelId}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _loggingService.Log($"Failed to get model info for {modelId}: {response.StatusCode}", LogLevel.Warning);
                return null;
            }
            
            var json = await response.Content.ReadAsStringAsync();
            var model = JsonSerializer.Deserialize<HuggingFaceModel>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            _loggingService.LogVerbose($"Retrieved model info for {modelId}");
            return model;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetModelInfoAsync for {modelId}");
            return null;
        }
    }

    private async Task<List<string>?> GetModelFilesAsync(string modelId)
    {
        try
        {
            _loggingService.LogVerbose($"Getting file list for model: {modelId}");
            
            var url = $"{HF_API_BASE}/models/{modelId}/tree/main";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _loggingService.Log($"Failed to get file list for {modelId}: {response.StatusCode}", LogLevel.Warning);
                return null;
            }
            
            var json = await response.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<HuggingFaceFile>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<HuggingFaceFile>();
            
            var filePaths = files.Select(f => f.Path).ToList();
            _loggingService.LogVerbose($"Found {filePaths.Count} files for model {modelId}");
            
            return filePaths;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetModelFilesAsync for {modelId}");
            return null;
        }
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
            
            var fileInfo = new FileInfo(localPath);
            _loggingService.LogVerbose($"Downloaded file: {Path.GetFileName(localPath)} ({fileInfo.Length:N0} bytes)");
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Download file from {url}");
            throw;
        }
    }

    private async Task CreateBasicLabelsFileAsync(string labelsPath)
    {
        try
        {
            _loggingService.LogVerbose("Creating basic labels file");
            
            // Create a basic ImageNet-style labels file
            var labels = Enumerable.Range(0, 1000)
                .Select(i => $"class_{i:D4}")
                .ToList();
            
            await File.WriteAllLinesAsync(labelsPath, labels);
            _loggingService.LogVerbose($"Created basic labels file with {labels.Count} classes");
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "CreateBasicLabelsFileAsync");
        }
    }

    public async Task<bool> ValidateDownloadedModelAsync(string modelId, string modelPath)
    {
        try
        {
            _loggingService.Log($"Validating downloaded Hugging Face model: {modelId}");
            
            var onnxFile = Directory.GetFiles(modelPath, "*.onnx").FirstOrDefault();
            var labelsFile = Directory.GetFiles(modelPath, "*.txt").FirstOrDefault();
            
            if (string.IsNullOrEmpty(onnxFile))
            {
                _loggingService.Log($"No ONNX file found in {modelPath}", LogLevel.Warning);
                return false;
            }
            
            if (string.IsNullOrEmpty(labelsFile))
            {
                _loggingService.Log($"No labels file found in {modelPath}", LogLevel.Warning);
                return false;
            }
            
            // Test ONNX model loading
            using var session = new Microsoft.ML.OnnxRuntime.InferenceSession(onnxFile);
            var labels = await File.ReadAllLinesAsync(labelsFile);
            
            _loggingService.Log($"Hugging Face model validation successful: {modelId} with {labels.Length} labels");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Validate Hugging Face model {modelId}");
            return false;
        }
    }

    public async Task<ModelInfo> CreateModelInfoFromDownloadedAsync(string modelId, string modelPath)
    {
        var hfModel = await GetModelInfoAsync(modelId);
        
        var modelInfo = new ModelInfo
        {
            Name = modelId.Replace("/", "-"),
            DisplayName = modelId,
            ModelPath = Path.Combine(modelPath, Directory.GetFiles(modelPath, "*.onnx").FirstOrDefault() ?? ""),
            LabelsPath = Path.Combine(modelPath, Directory.GetFiles(modelPath, "*.txt").FirstOrDefault() ?? ""),
            ImageWidth = 224,
            ImageHeight = 224,
            ConfidenceThreshold = 0.1,
            MaxTags = 5,
            Description = hfModel?.Description ?? $"Downloaded from Hugging Face: {modelId}",
            Source = "Hugging Face Hub",
            License = hfModel?.License ?? "Unknown",
            Priority = 100,
            IsEnabled = true,
            AdditionalProperties = new Dictionary<string, object>
            {
                ["huggingface_id"] = modelId,
                ["downloads"] = hfModel?.Downloads ?? 0,
                ["likes"] = hfModel?.Likes ?? 0
            }
        };
        
        return modelInfo;
    }

    // Data classes for Hugging Face API responses
    public class HuggingFaceModel
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public string? License { get; set; }
        public int? Downloads { get; set; }
        public int? Likes { get; set; }
        public List<string>? Tags { get; set; }
    }

    private class HuggingFaceFile
    {
        public string? Path { get; set; }
        public string? Type { get; set; }
        public long? Size { get; set; }
    }
} 