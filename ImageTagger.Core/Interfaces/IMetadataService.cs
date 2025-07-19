using ImageTagger.Core.Models;

namespace ImageTagger.Core.Interfaces;

public interface IMetadataService
{
    Task<bool> SaveTagsAsync(string imagePath, List<string> tags, CancellationToken cancellationToken = default);
    Task<List<string>> ReadTagsAsync(string imagePath, CancellationToken cancellationToken = default);
    bool IsSupported(string imagePath);
    Task<ImageInfo> GetImageInfoAsync(string imagePath, CancellationToken cancellationToken = default);
} 