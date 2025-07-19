namespace ImageTagger.Core.Models;

public class ModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
    public string LabelsPath { get; set; } = string.Empty;
    public int ImageWidth { get; set; } = 224;
    public int ImageHeight { get; set; } = 224;
    public double ConfidenceThreshold { get; set; } = 0.1;
    public int MaxTags { get; set; } = 5;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "ONNX Models", "Custom", etc.
    public string License { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 0; // Higher number = higher priority
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}

public class ModelRegistry
{
    public List<ModelInfo> Models { get; set; } = new();
    public string DefaultModelName { get; set; } = string.Empty;
    public Dictionary<string, string> ModelAliases { get; set; } = new();
} 