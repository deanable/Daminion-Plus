namespace ImageTagger.Core.Models;

public enum ModelType
{
    Unknown = 0,
    Onnx = 1,
    PyTorch = 2
}

public enum ConversionStatus
{
    NotConverted = 0,
    Converting = 1,
    Converted = 2,
    Failed = 3
}

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

    // New fields for type and conversion status
    public ModelType ModelType { get; set; } = ModelType.Unknown;
    public ConversionStatus ConversionStatus { get; set; } = ConversionStatus.NotConverted;
}

public class ModelRegistry
{
    public List<ModelInfo> Models { get; set; } = new();
    public string DefaultModelName { get; set; } = string.Empty;
    public Dictionary<string, string> ModelAliases { get; set; } = new();
} 