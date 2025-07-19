namespace ImageTagger.Core.Models;

/// <summary>
/// Options for filtering models when scanning the Hugging Face repository
/// </summary>
public class ModelFilterOptions
{
    /// <summary>
    /// Minimum number of downloads required for a model to be included
    /// </summary>
    public int MinDownloads { get; set; } = 100;

    /// <summary>
    /// Maximum model size in MB
    /// </summary>
    public int MaxModelSizeMB { get; set; } = 500;

    /// <summary>
    /// Supported model formats (e.g., "onnx", "pytorch", "tensorflow")
    /// </summary>
    public string[] SupportedFormats { get; set; } = new[] { "onnx" };

    /// <summary>
    /// Task categories to include (e.g., "image-classification", "computer-vision")
    /// </summary>
    public string[]? TaskCategories { get; set; }

    /// <summary>
    /// Licenses to include (e.g., "apache-2.0", "mit", "gpl")
    /// </summary>
    public string[]? Licenses { get; set; }

    /// <summary>
    /// Search terms to filter models
    /// </summary>
    public string[]? SearchTerms { get; set; }

    /// <summary>
    /// Whether to exclude archived models
    /// </summary>
    public bool ExcludeArchived { get; set; } = true;

    /// <summary>
    /// Whether to exclude private models
    /// </summary>
    public bool ExcludePrivate { get; set; } = true;

    /// <summary>
    /// Maximum number of models to return
    /// </summary>
    public int MaxModels { get; set; } = 0; // 0 = no limit

    /// <summary>
    /// Sort field ("downloads", "likes", "updated")
    /// </summary>
    public string SortBy { get; set; } = "downloads";

    /// <summary>
    /// Sort direction ("asc", "desc")
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Whether to include only verified models
    /// </summary>
    public bool OnlyVerified { get; set; } = false;

    /// <summary>
    /// Minimum number of likes required
    /// </summary>
    public int MinLikes { get; set; } = 0;

    /// <summary>
    /// Whether to include models with ImageNet labels
    /// </summary>
    public bool PreferImageNetLabels { get; set; } = true;

    /// <summary>
    /// Whether to include models with recent updates (within days)
    /// </summary>
    public int? MaxDaysSinceUpdate { get; set; } = null;
} 