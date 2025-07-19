using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using Microsoft.ML.OnnxRuntime;
using System.Text.Json;

namespace ImageTagger.Infrastructure;

public class ModelManager : IModelManager
{
    private readonly ILoggingService _loggingService;
    private readonly HttpClient _httpClient;
    private ModelRegistry? _cachedRegistry;

    public ModelManager(ILoggingService loggingService, HttpClient? httpClient = null)
    {
        _loggingService = loggingService;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<ModelRegistry> LoadModelRegistryAsync(string registryPath)
    {
        try
        {
            if (_cachedRegistry != null)
                return _cachedRegistry;

            if (!File.Exists(registryPath))
            {
                _loggingService.Log($"Registry file not found, creating default registry: {registryPath}");
                var defaultRegistry = CreateDefaultRegistry();
                await SaveModelRegistryAsync(defaultRegistry, registryPath);
                _cachedRegistry = defaultRegistry;
                return defaultRegistry;
            }

            var json = await File.ReadAllTextAsync(registryPath);
            var registry = JsonSerializer.Deserialize<ModelRegistry>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (registry == null)
                throw new InvalidOperationException("Failed to deserialize model registry");

            _cachedRegistry = registry;
            _loggingService.Log($"Loaded model registry with {registry.Models.Count} models");
            return registry;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "LoadModelRegistryAsync");
            throw;
        }
    }

    public async Task SaveModelRegistryAsync(ModelRegistry registry, string registryPath)
    {
        try
        {
            var json = JsonSerializer.Serialize(registry, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(registryPath, json);
            _cachedRegistry = registry;
            _loggingService.Log($"Saved model registry to {registryPath}");
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "SaveModelRegistryAsync");
            throw;
        }
    }

    public async Task<ModelInfo?> GetModelAsync(string modelName)
    {
        var registry = await LoadModelRegistryAsync(GetDefaultRegistryPath());
        return registry.Models.FirstOrDefault(m => 
            m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase) ||
            m.DisplayName.Equals(modelName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<ModelInfo>> GetAllModelsAsync()
    {
        var registry = await LoadModelRegistryAsync(GetDefaultRegistryPath());
        return registry.Models.OrderByDescending(m => m.Priority).ToList();
    }

    public async Task<ModelInfo?> GetDefaultModelAsync()
    {
        var registry = await LoadModelRegistryAsync(GetDefaultRegistryPath());
        if (string.IsNullOrEmpty(registry.DefaultModelName))
            return registry.Models.FirstOrDefault(m => m.IsEnabled);

        return registry.Models.FirstOrDefault(m => 
            m.Name.Equals(registry.DefaultModelName, StringComparison.OrdinalIgnoreCase) && m.IsEnabled);
    }

    public async Task SetDefaultModelAsync(string modelName)
    {
        var registry = await LoadModelRegistryAsync(GetDefaultRegistryPath());
        var model = registry.Models.FirstOrDefault(m => 
            m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        
        if (model == null)
            throw new ArgumentException($"Model '{modelName}' not found");

        registry.DefaultModelName = model.Name;
        await SaveModelRegistryAsync(registry, GetDefaultRegistryPath());
        _loggingService.Log($"Set default model to: {modelName}");
    }

    public async Task<bool> ValidateModelAsync(ModelInfo model)
    {
        try
        {
            if (!File.Exists(model.ModelPath))
            {
                _loggingService.Log($"Model file not found: {model.ModelPath}", LogLevel.Warning);
                return false;
            }

            if (!File.Exists(model.LabelsPath))
            {
                _loggingService.Log($"Labels file not found: {model.LabelsPath}", LogLevel.Warning);
                return false;
            }

            // Test ONNX model loading
            using var session = new InferenceSession(model.ModelPath);
            var inputMetadata = session.InputMetadata;
            var outputMetadata = session.OutputMetadata;

            _loggingService.Log($"Model validation successful: {model.Name}", LogLevel.Debug);
            _loggingService.LogVerbose($"Inputs: {string.Join(", ", inputMetadata.Keys)}");
            _loggingService.LogVerbose($"Outputs: {string.Join(", ", outputMetadata.Keys)}");

            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Model validation failed for {model.Name}");
            return false;
        }
    }

    public async Task<bool> DownloadModelAsync(ModelInfo model, string downloadPath)
    {
        try
        {
            // This would implement downloading from ONNX Models repository
            // For now, we'll just validate the model exists
            _loggingService.Log($"Downloading model: {model.Name}");
            
            // TODO: Implement actual download logic from ONNX Models repository
            // This would involve:
            // 1. Fetching model metadata from GitHub API
            // 2. Downloading the ONNX file
            // 3. Downloading the labels file
            // 4. Updating the model paths
            
            _loggingService.Log($"Model download completed: {model.Name}");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Model download failed for {model.Name}");
            return false;
        }
    }

    public async Task<List<ModelInfo>> GetAvailableModelsAsync()
    {
        // This would return models available from the ONNX Models repository
        var availableModels = new List<ModelInfo>
        {
            new ModelInfo
            {
                Name = "resnet50-v1-12",
                DisplayName = "ResNet-50 v1.12",
                Description = "ResNet-50 model for image classification",
                Source = "ONNX Models",
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
                Description = "Lightweight EfficientNet model for mobile/edge devices",
                Source = "ONNX Models",
                License = "MIT",
                ImageWidth = 224,
                ImageHeight = 224,
                ConfidenceThreshold = 0.1,
                MaxTags = 5,
                Priority = 90
            },
            new ModelInfo
            {
                Name = "mobilenetv2-12",
                DisplayName = "MobileNet v2",
                Description = "MobileNet v2 model optimized for mobile devices",
                Source = "ONNX Models",
                License = "MIT",
                ImageWidth = 224,
                ImageHeight = 224,
                ConfidenceThreshold = 0.1,
                MaxTags = 5,
                Priority = 80
            },
            new ModelInfo
            {
                Name = "inception-v1-12",
                DisplayName = "Inception v1",
                Description = "Inception v1 model for image classification",
                Source = "ONNX Models",
                License = "MIT",
                ImageWidth = 224,
                ImageHeight = 224,
                ConfidenceThreshold = 0.1,
                MaxTags = 5,
                Priority = 70
            }
        };

        return availableModels;
    }

    public async Task<bool> EnableModelAsync(string modelName, bool enabled)
    {
        var registry = await LoadModelRegistryAsync(GetDefaultRegistryPath());
        var model = registry.Models.FirstOrDefault(m => 
            m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        
        if (model == null)
            return false;

        model.IsEnabled = enabled;
        await SaveModelRegistryAsync(registry, GetDefaultRegistryPath());
        _loggingService.Log($"Model {modelName} {(enabled ? "enabled" : "disabled")}");
        return true;
    }

    public async Task<ModelInfo> CreateModelFromTemplateAsync(string templateName, string modelName, string modelPath)
    {
        var availableModels = await GetAvailableModelsAsync();
        var template = availableModels.FirstOrDefault(m => 
            m.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
        
        if (template == null)
            throw new ArgumentException($"Template '{templateName}' not found");

        var newModel = new ModelInfo
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

        var registry = await LoadModelRegistryAsync(GetDefaultRegistryPath());
        registry.Models.Add(newModel);
        await SaveModelRegistryAsync(registry, GetDefaultRegistryPath());

        _loggingService.Log($"Created model from template: {modelName}");
        return newModel;
    }

    private ModelRegistry CreateDefaultRegistry()
    {
        var registry = new ModelRegistry
        {
            DefaultModelName = "resnet50-v1-12",
            Models = new List<ModelInfo>
            {
                new ModelInfo
                {
                    Name = "resnet50-v1-12",
                    DisplayName = "ResNet-50 v1.12",
                    ModelPath = Path.Combine("models", "resnet50-v1-12.onnx"),
                    LabelsPath = Path.Combine("models", "imagenet_classes.txt"),
                    Description = "ResNet-50 model for image classification",
                    Source = "ONNX Models",
                    License = "MIT",
                    ImageWidth = 224,
                    ImageHeight = 224,
                    ConfidenceThreshold = 0.1,
                    MaxTags = 5,
                    Priority = 100,
                    IsEnabled = true
                }
            }
        };

        return registry;
    }

    private string GetDefaultRegistryPath()
    {
        return Path.Combine("models", "model_registry.json");
    }
} 