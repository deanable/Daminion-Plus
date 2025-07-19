namespace ImageTagger.Core.Models;

public class TagResult
{
    public string Tag { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Source { get; set; } = string.Empty; // "ML.NET", "Cloud API", etc.
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class TaggingResult
{
    public string ImagePath { get; set; } = string.Empty;
    public List<TagResult> Tags { get; set; } = new();
    public string Method { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
} 