using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using ImageTagger.Infrastructure;
using ImageTagger.Services;

namespace ImageTagger;

/// <summary>
/// Example class demonstrating how to integrate the model management system
/// into the main ImageTagger application.
/// </summary>
public class ModelManagementExample
{
    private readonly ILoggingService _loggingService;
    private readonly IModelManager _modelManager;
    private readonly ModelDownloaderService _modelDownloader;
    private readonly EnhancedMLNetTaggingService _enhancedTaggingService;

    public ModelManagementExample(ILoggingService loggingService)
    {
        _loggingService = loggingService;
        _modelManager = new ModelManager(loggingService);
        _modelDownloader = new ModelDownloaderService(loggingService);
        _enhancedTaggingService = new EnhancedMLNetTaggingService(loggingService, _modelManager);
    }

    /// <summary>
    /// Example: Initialize the model management system
    /// </summary>
    public async Task InitializeModelManagementAsync()
    {
        try
        {
            _loggingService.Log("Initializing model management system...");

            // Load or create the model registry
            var registry = await _modelManager.LoadModelRegistryAsync("models/model_registry.json");
            
            // Check if we have any models
            if (!registry.Models.Any())
            {
                _loggingService.Log("No models found in registry, setting up default model...");
                await SetupDefaultModelAsync();
            }

            // Validate all enabled models
            await ValidateAllModelsAsync();

            _loggingService.Log("Model management system initialized successfully");
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "InitializeModelManagement");
            throw;
        }
    }

    /// <summary>
    /// Example: Set up a default model if none exists
    /// </summary>
    private async Task SetupDefaultModelAsync()
    {
        try
        {
            // Download ResNet-50 as the default model
            var success = await _modelDownloader.DownloadModelFromRepositoryAsync("resnet50-v1-12");
            
            if (success)
            {
                // Create model info and add to registry
                var modelPath = Path.Combine("models", "resnet50-v1-12");
                var modelInfo = await _modelDownloader.CreateModelInfoFromDownloadedAsync("resnet50-v1-12", modelPath);
                
                var registry = await _modelManager.LoadModelRegistryAsync("models/model_registry.json");
                registry.Models.Add(modelInfo);
                registry.DefaultModelName = modelInfo.Name;
                
                await _modelManager.SaveModelRegistryAsync(registry, "models/model_registry.json");
                
                _loggingService.Log("Default model (ResNet-50) set up successfully");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "SetupDefaultModel");
            throw;
        }
    }

    /// <summary>
    /// Example: Validate all enabled models
    /// </summary>
    private async Task ValidateAllModelsAsync()
    {
        try
        {
            var models = await _modelManager.GetAllModelsAsync();
            var enabledModels = models.Where(m => m.IsEnabled).ToList();

            _loggingService.Log($"Validating {enabledModels.Count} enabled models...");

            foreach (var model in enabledModels)
            {
                var isValid = await _modelManager.ValidateModelAsync(model);
                if (isValid)
                {
                    _loggingService.Log($"✓ Model {model.DisplayName} is valid");
                }
                else
                {
                    _loggingService.Log($"✗ Model {model.DisplayName} validation failed", LogLevel.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "ValidateAllModels");
        }
    }

    /// <summary>
    /// Example: Tag an image using the enhanced service with default model
    /// </summary>
    public async Task<TaggingResult> TagImageWithDefaultModelAsync(string imagePath)
    {
        try
        {
            _loggingService.Log($"Tagging image with default model: {imagePath}");
            
            var result = await _enhancedTaggingService.TagImageAsync(imagePath);
            
            if (result.Success)
            {
                _loggingService.Log($"Successfully tagged image with {result.Tags.Count} tags");
                foreach (var tag in result.Tags)
                {
                    _loggingService.Log($"  - {tag.Tag} (confidence: {tag.Confidence:F3})");
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "TagImageWithDefaultModel");
            throw;
        }
    }

    /// <summary>
    /// Example: Tag an image with a specific model
    /// </summary>
    public async Task<TaggingResult> TagImageWithSpecificModelAsync(string imagePath, string modelName)
    {
        try
        {
            _loggingService.Log($"Tagging image with model {modelName}: {imagePath}");
            
            var result = await _enhancedTaggingService.TagImageWithModelAsync(imagePath, modelName);
            
            if (result.Success)
            {
                _loggingService.Log($"Successfully tagged image with {modelName}, got {result.Tags.Count} tags");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"TagImageWithSpecificModel {modelName}");
            throw;
        }
    }

    /// <summary>
    /// Example: Tag an image with multiple models for better results
    /// </summary>
    public async Task<TaggingResult> TagImageWithMultipleModelsAsync(string imagePath)
    {
        try
        {
            var modelNames = new List<string> { "resnet50-v1-12", "efficientnet-lite4-11" };
            _loggingService.Log($"Tagging image with multiple models: {string.Join(", ", modelNames)}");
            
            var result = await _enhancedTaggingService.TagImageWithMultipleModelsAsync(imagePath, modelNames);
            
            if (result.Success)
            {
                _loggingService.Log($"Successfully tagged image with multiple models, got {result.Tags.Count} unique tags");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "TagImageWithMultipleModels");
            throw;
        }
    }

    /// <summary>
    /// Example: Download and add a new model
    /// </summary>
    public async Task<bool> AddNewModelAsync(string modelName)
    {
        try
        {
            _loggingService.Log($"Adding new model: {modelName}");

            // Check if model is available
            var availableModels = await _modelDownloader.GetAvailableModelsFromRepositoryAsync();
            var model = availableModels.FirstOrDefault(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
            
            if (model == null)
            {
                _loggingService.Log($"Model {modelName} not found in available models", LogLevel.Warning);
                return false;
            }

            // Download the model
            var success = await _modelDownloader.DownloadModelFromRepositoryAsync(modelName);
            
            if (success)
            {
                // Validate and add to registry
                var modelPath = Path.Combine("models", modelName);
                var isValid = await _modelDownloader.ValidateDownloadedModelAsync(modelName, modelPath);
                
                if (isValid)
                {
                    var modelInfo = await _modelDownloader.CreateModelInfoFromDownloadedAsync(modelName, modelPath);
                    
                    var registry = await _modelManager.LoadModelRegistryAsync("models/model_registry.json");
                    registry.Models.Add(modelInfo);
                    await _modelManager.SaveModelRegistryAsync(registry, "models/model_registry.json");
                    
                    _loggingService.Log($"Model {modelName} added successfully");
                    return true;
                }
                else
                {
                    _loggingService.Log($"Model {modelName} downloaded but validation failed", LogLevel.Warning);
                    return false;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"AddNewModel {modelName}");
            return false;
        }
    }

    /// <summary>
    /// Example: Switch the default model
    /// </summary>
    public async Task<bool> SwitchDefaultModelAsync(string modelName)
    {
        try
        {
            _loggingService.Log($"Switching default model to: {modelName}");
            
            await _modelManager.SetDefaultModelAsync(modelName);
            
            _loggingService.Log($"Default model switched to {modelName} successfully");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"SwitchDefaultModel {modelName}");
            return false;
        }
    }

    /// <summary>
    /// Example: Get information about all available models
    /// </summary>
    public async Task<List<ModelInfo>> GetAllAvailableModelsAsync()
    {
        try
        {
            var availableModels = await _modelDownloader.GetAvailableModelsFromRepositoryAsync();
            var installedModels = await _modelManager.GetAllModelsAsync();
            
            // Mark which models are already installed
            foreach (var availableModel in availableModels)
            {
                var isInstalled = installedModels.Any(m => m.Name.Equals(availableModel.Name, StringComparison.OrdinalIgnoreCase));
                availableModel.AdditionalProperties["IsInstalled"] = isInstalled;
            }
            
            return availableModels;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetAllAvailableModels");
            return new List<ModelInfo>();
        }
    }

    /// <summary>
    /// Example: Get performance comparison between models
    /// </summary>
    public async Task<Dictionary<string, TimeSpan>> CompareModelPerformanceAsync(string imagePath)
    {
        var results = new Dictionary<string, TimeSpan>();
        
        try
        {
            var models = await _modelManager.GetAllModelsAsync();
            var enabledModels = models.Where(m => m.IsEnabled).ToList();

            _loggingService.Log($"Comparing performance of {enabledModels.Count} models...");

            foreach (var model in enabledModels)
            {
                try
                {
                    var startTime = DateTime.UtcNow;
                    var result = await _enhancedTaggingService.TagImageWithModelAsync(imagePath, model.Name);
                    var duration = DateTime.UtcNow - startTime;
                    
                    results[model.DisplayName] = duration;
                    
                    _loggingService.Log($"  {model.DisplayName}: {duration.TotalMilliseconds:F2}ms ({result.Tags.Count} tags)");
                }
                catch (Exception ex)
                {
                    _loggingService.Log($"Failed to test model {model.DisplayName}: {ex.Message}", LogLevel.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "CompareModelPerformance");
        }
        
        return results;
    }
} 