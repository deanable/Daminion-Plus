using System.Text.Json;
using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;

namespace ImageTagger.Services;

public class MicrosoftModelZooService
{
    private readonly ILoggingService _loggingService;
    private readonly HttpClient _httpClient;
    private readonly string _modelsDirectory;
    private const string MODEL_ZOO_API_BASE = "https://raw.githubusercontent.com/onnx/models/main";
    private const string MODEL_ZOO_INDEX_URL = "https://raw.githubusercontent.com/onnx/models/main/vision/classification/index.json";

    public MicrosoftModelZooService(ILoggingService loggingService, string modelsDirectory = "models")
    {
        _loggingService = loggingService;
        _httpClient = new HttpClient();
        _modelsDirectory = modelsDirectory;
    }

    public async Task<List<ModelInfo>> GetAvailableModelsAsync()
    {
        try
        {
            _loggingService.Log("Fetching models from Microsoft ONNX Model Zoo...");
            
            var response = await _httpClient.GetStringAsync(MODEL_ZOO_INDEX_URL);
            var models = JsonSerializer.Deserialize<List<MicrosoftModelZooModel>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<MicrosoftModelZooModel>();

            var modelInfos = new List<ModelInfo>();
            
            foreach (var model in models)
            {
                if (model.Framework?.ToLowerInvariant() == "onnx")
                {
                    var modelInfo = new ModelInfo
                    {
                        Name = model.Name?.Replace("/", "-") ?? "unknown",
                        DisplayName = model.Name ?? "Unknown Model",
                        Description = model.Description ?? "No description available",
                        Source = "Microsoft ONNX Model Zoo",
                        License = model.License ?? "Unknown",
                        ImageWidth = 224, // Default, can be updated after download
                        ImageHeight = 224,
                        ConfidenceThreshold = 0.1,
                        MaxTags = 5,
                        Priority = CalculatePriority(model),
                        IsEnabled = false,
                        AdditionalProperties = new Dictionary<string, object>
                        {
                            ["model_zoo_id"] = model.Name ?? "",
                            ["framework"] = model.Framework ?? "",
                            ["domain"] = model.Domain ?? "",
                            ["dataset"] = model.Dataset ?? "",
                            ["accuracy"] = model.Accuracy ?? "",
                            ["model_size"] = model.ModelSize ?? "",
                            ["download_url"] = model.DownloadUrl ?? "",
                            ["labels_url"] = model.LabelsUrl ?? ""
                        }
                    };
                    
                    modelInfos.Add(modelInfo);
                }
            }

            _loggingService.Log($"Found {modelInfos.Count} ONNX models from Microsoft Model Zoo");
            return modelInfos;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetAvailableModelsAsync from Microsoft Model Zoo");
            return new List<ModelInfo>();
        }
    }

    public async Task<bool> DownloadModelAsync(string modelName, string? customPath = null)
    {
        try
        {
            _loggingService.Log($"Starting download of Microsoft Model Zoo model: {modelName}");
            
            // Get model info
            var models = await GetAvailableModelsAsync();
            var model = models.FirstOrDefault(m => m.AdditionalProperties.GetValueOrDefault("model_zoo_id", "").ToString() == modelName);
            
            if (model == null)
            {
                _loggingService.Log($"Model {modelName} not found in Microsoft Model Zoo", LogLevel.Warning);
                return false;
            }

            var targetPath = customPath ?? Path.Combine(_modelsDirectory, modelName.Replace("/", "-"));
            _loggingService.LogVerbose($"Target path: {targetPath}");
            
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // Download model file
            var downloadUrl = model.AdditionalProperties.GetValueOrDefault("download_url", "").ToString();
            if (string.IsNullOrEmpty(downloadUrl))
            {
                _loggingService.Log($"No download URL found for model {modelName}", LogLevel.Warning);
                return false;
            }

            var modelFileName = Path.GetFileName(downloadUrl);
            var modelPath = Path.Combine(targetPath, modelFileName);
            
            await DownloadFileAsync(downloadUrl, modelPath);
            _loggingService.Log($"Downloaded model file: {modelPath}");

            // Download labels file
            var labelsUrl = model.AdditionalProperties.GetValueOrDefault("labels_url", "").ToString();
            if (!string.IsNullOrEmpty(labelsUrl))
            {
                var labelsFileName = Path.GetFileName(labelsUrl);
                var labelsPath = Path.Combine(targetPath, labelsFileName);
                await DownloadFileAsync(labelsUrl, labelsPath);
                _loggingService.Log($"Downloaded labels file: {labelsPath}");
            }
            else
            {
                // Create basic labels file if none exists
                var labelsPath = Path.Combine(targetPath, "labels.txt");
                await CreateBasicLabelsFileAsync(labelsPath);
            }

            _loggingService.Log($"Successfully downloaded Microsoft Model Zoo model {modelName} to {targetPath}");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Download Microsoft Model Zoo model {modelName}");
            return false;
        }
    }

    private async Task DownloadFileAsync(string url, string localPath)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            using var fileStream = File.Create(localPath);
            await response.Content.CopyToAsync(fileStream);
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
            // Create a basic ImageNet labels file
            var labels = Enumerable.Range(0, 1000).Select(i => $"class_{i}").ToArray();
            await File.WriteAllLinesAsync(labelsPath, labels);
            _loggingService.Log($"Created basic labels file: {labelsPath}");
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "CreateBasicLabelsFileAsync");
        }
    }

    private int CalculatePriority(MicrosoftModelZooModel model)
    {
        var priority = 50; // Base priority
        
        // Boost by dataset (ImageNet is preferred)
        if (model.Dataset?.Contains("imagenet", StringComparison.OrdinalIgnoreCase) == true)
        {
            priority += 30;
        }
        
        // Boost by accuracy
        if (!string.IsNullOrEmpty(model.Accuracy))
        {
            if (model.Accuracy.Contains("top-1") || model.Accuracy.Contains("top-5"))
            {
                priority += 20;
            }
        }
        
        // Boost by model size (smaller models preferred)
        if (!string.IsNullOrEmpty(model.ModelSize))
        {
            if (model.ModelSize.Contains("MB") && !model.ModelSize.Contains("GB"))
            {
                priority += 15;
            }
        }
        
        return priority;
    }

    public class MicrosoftModelZooModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Framework { get; set; }
        public string? Domain { get; set; }
        public string? Dataset { get; set; }
        public string? Accuracy { get; set; }
        public string? ModelSize { get; set; }
        public string? License { get; set; }
        public string? DownloadUrl { get; set; }
        public string? LabelsUrl { get; set; }
    }
} 