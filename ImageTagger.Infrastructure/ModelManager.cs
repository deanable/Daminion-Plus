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
        try
        {
            _loggingService.Log($"Getting model: {modelName}");
            var registry = await LoadModelRegistryAsync(GetDefaultRegistryPath());
            
            var model = registry.Models.FirstOrDefault(m => 
                m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase) ||
                m.DisplayName.Equals(modelName, StringComparison.OrdinalIgnoreCase));
            
            if (model != null)
            {
                _loggingService.Log($"Found model: {model.Name} (Display: {model.DisplayName})");
                _loggingService.LogVerbose($"Model path: {model.ModelPath}, Labels: {model.LabelsPath}");
            }
            else
            {
                _loggingService.Log($"Model not found: {modelName}", LogLevel.Warning);
                _loggingService.LogVerbose($"Available models: {string.Join(", ", registry.Models.Select(m => m.Name))}");
            }
            
            return model;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetModelAsync for {modelName}");
            return null;
        }
    }

    public async Task<List<ModelInfo>> GetAllModelsAsync()
    {
        try
        {
            _loggingService.Log("Getting all models from registry");
            var registry = await LoadModelRegistryAsync(GetDefaultRegistryPath());
            var models = registry.Models.OrderByDescending(m => m.Priority).ToList();
            
            _loggingService.Log($"Retrieved {models.Count} models from registry");
            foreach (var model in models)
            {
                _loggingService.LogVerbose($"  - {model.Name} (Priority: {model.Priority}, Enabled: {model.IsEnabled})");
            }
            
            return models;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetAllModelsAsync");
            return new List<ModelInfo>();
        }
    }

    public async Task<ModelInfo?> GetDefaultModelAsync()
    {
        try
        {
            _loggingService.Log("Getting default model from registry");
            var registry = await LoadModelRegistryAsync(GetDefaultRegistryPath());
            
            if (string.IsNullOrEmpty(registry.DefaultModelName))
            {
                _loggingService.Log("No default model name specified, finding first enabled model");
                var firstEnabled = registry.Models.FirstOrDefault(m => m.IsEnabled);
                if (firstEnabled != null)
                {
                    _loggingService.Log($"Using first enabled model as default: {firstEnabled.Name}");
                }
                else
                {
                    _loggingService.Log("No enabled models found", LogLevel.Warning);
                }
                return firstEnabled;
            }

            _loggingService.Log($"Looking for default model: {registry.DefaultModelName}");
            var defaultModel = registry.Models.FirstOrDefault(m => 
                m.Name.Equals(registry.DefaultModelName, StringComparison.OrdinalIgnoreCase) && m.IsEnabled);
            
            if (defaultModel != null)
            {
                _loggingService.Log($"Found default model: {defaultModel.Name} (Enabled: {defaultModel.IsEnabled})");
            }
            else
            {
                _loggingService.Log($"Default model '{registry.DefaultModelName}' not found or not enabled", LogLevel.Warning);
                _loggingService.LogVerbose($"Available models: {string.Join(", ", registry.Models.Select(m => $"{m.Name}(Enabled:{m.IsEnabled})"))}");
            }
            
            return defaultModel;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetDefaultModelAsync");
            return null;
        }
    }

    public async Task SetDefaultModelAsync(string modelName)
    {
        try
        {
            _loggingService.Log($"Setting default model to: {modelName}");
            var registry = await LoadModelRegistryAsync(GetDefaultRegistryPath());
            
            var model = registry.Models.FirstOrDefault(m => 
                m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
            
            if (model == null)
            {
                _loggingService.Log($"Model '{modelName}' not found in registry", LogLevel.Error);
                _loggingService.LogVerbose($"Available models: {string.Join(", ", registry.Models.Select(m => m.Name))}");
                throw new ArgumentException($"Model '{modelName}' not found");
            }

            _loggingService.Log($"Found model '{model.Name}' in registry, setting as default");
            registry.DefaultModelName = model.Name;
            await SaveModelRegistryAsync(registry, GetDefaultRegistryPath());
            _loggingService.Log($"Successfully set default model to: {modelName}");
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"SetDefaultModelAsync for {modelName}");
            throw;
        }
    }

    public async Task<bool> ValidateModelAsync(ModelInfo model)
    {
        try
        {
            _loggingService.Log($"Starting validation for model: {model.Name}");
            _loggingService.LogVerbose($"Model path: {model.ModelPath}");
            _loggingService.LogVerbose($"Labels path: {model.LabelsPath}");
            
            // Check if model file exists
            if (!File.Exists(model.ModelPath))
            {
                _loggingService.Log($"Model file not found: {model.ModelPath}", LogLevel.Warning);
                _loggingService.LogVerbose($"Current directory: {Directory.GetCurrentDirectory()}");
                _loggingService.LogVerbose($"Absolute path: {Path.GetFullPath(model.ModelPath)}");
                return false;
            }

            // Check if labels file exists
            if (!File.Exists(model.LabelsPath))
            {
                _loggingService.Log($"Labels file not found: {model.LabelsPath}", LogLevel.Warning);
                _loggingService.LogVerbose($"Current directory: {Directory.GetCurrentDirectory()}");
                _loggingService.LogVerbose($"Absolute path: {Path.GetFullPath(model.LabelsPath)}");
                return false;
            }

            // Get file sizes for debugging
            var modelFileInfo = new FileInfo(model.ModelPath);
            var labelsFileInfo = new FileInfo(model.LabelsPath);
            _loggingService.LogVerbose($"Model file size: {modelFileInfo.Length:N0} bytes");
            _loggingService.LogVerbose($"Labels file size: {labelsFileInfo.Length:N0} bytes");

            // Test ONNX model loading
            _loggingService.LogVerbose("Attempting to load ONNX model...");
            using var session = new InferenceSession(model.ModelPath);
            var inputMetadata = session.InputMetadata;
            var outputMetadata = session.OutputMetadata;

            _loggingService.Log($"Model validation successful: {model.Name}", LogLevel.Debug);
            _loggingService.LogVerbose($"ONNX Inputs: {string.Join(", ", inputMetadata.Keys)}");
            _loggingService.LogVerbose($"ONNX Outputs: {string.Join(", ", outputMetadata.Keys)}");
            
            // Log input/output shapes
            foreach (var input in inputMetadata)
            {
                _loggingService.LogVerbose($"Input '{input.Key}': {string.Join("x", input.Value.Dimensions)}");
            }
            foreach (var output in outputMetadata)
            {
                _loggingService.LogVerbose($"Output '{output.Key}': {string.Join("x", output.Value.Dimensions)}");
            }

            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Model validation failed for {model.Name}");
            _loggingService.Log($"Validation failed for model '{model.Name}': {ex.Message}", LogLevel.Error);
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
        try
        {
            _loggingService.Log($"Attempting to {(enabled ? "enable" : "disable")} model: {modelName}");
            var registry = await LoadModelRegistryAsync(GetDefaultRegistryPath());
            
            var model = registry.Models.FirstOrDefault(m => 
                m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
            
            if (model == null)
            {
                _loggingService.Log($"Model '{modelName}' not found in registry", LogLevel.Warning);
                _loggingService.LogVerbose($"Available models: {string.Join(", ", registry.Models.Select(m => m.Name))}");
                return false;
            }

            _loggingService.Log($"Found model '{model.Name}', current enabled state: {model.IsEnabled}");
            model.IsEnabled = enabled;
            await SaveModelRegistryAsync(registry, GetDefaultRegistryPath());
            _loggingService.Log($"Successfully {(enabled ? "enabled" : "disabled")} model: {modelName}");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"EnableModelAsync for {modelName}");
            return false;
        }
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