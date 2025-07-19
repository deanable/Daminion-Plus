namespace ImageTagger.Core.Models;

public class ImageInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public long FileSize { get; set; }
    public string Extension => Path.GetExtension(FilePath).ToLowerInvariant();
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool HasTags => Tags.Count > 0;
} 