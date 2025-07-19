namespace ImageTagger.Core.Configuration;

public class AppSettings
{
    public CloudApiSettings CloudApi { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public ModelSettings Model { get; set; } = new();
    public MetadataSettings Metadata { get; set; } = new();
}

public class CloudApiSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}

public class LoggingSettings
{
    public string LogLevel { get; set; } = "Info";
    public bool LogToFile { get; set; } = true;
    public bool LogToConsole { get; set; } = false;
}

public class ModelSettings
{
    public double ConfidenceThreshold { get; set; } = 0.1;
    public int MaxTags { get; set; } = 3;
    public string ModelPath { get; set; } = string.Empty;
    public string LabelsPath { get; set; } = string.Empty;
}

public class MetadataSettings
{
    public bool CreateBackups { get; set; } = true;
    public string[] SupportedFormats { get; set; } = { "jpg", "jpeg", "png", "tiff", "tif" };
} 