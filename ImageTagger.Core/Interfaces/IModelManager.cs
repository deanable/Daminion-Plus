using ImageTagger.Core.Models;

namespace ImageTagger.Core.Interfaces;

public interface IModelManager
{
    Task<ModelRegistry> LoadModelRegistryAsync(string registryPath);
    Task SaveModelRegistryAsync(ModelRegistry registry, string registryPath);
    Task<ModelInfo?> GetModelAsync(string modelName);
    Task<List<ModelInfo>> GetAllModelsAsync();
    Task<ModelInfo?> GetDefaultModelAsync();
    Task SetDefaultModelAsync(string modelName);
    Task<bool> ValidateModelAsync(ModelInfo model);
    Task<bool> DownloadModelAsync(ModelInfo model, string downloadPath);
    Task<List<ModelInfo>> GetAvailableModelsAsync();
    Task<bool> EnableModelAsync(string modelName, bool enabled);
    Task<ModelInfo> CreateModelFromTemplateAsync(string templateName, string modelName, string modelPath);
} 