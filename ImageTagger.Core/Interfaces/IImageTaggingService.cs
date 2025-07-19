using ImageTagger.Core.Models;

namespace ImageTagger.Core.Interfaces;

public interface IImageTaggingService
{
    Task<TaggingResult> TagImageAsync(string imagePath, CancellationToken cancellationToken = default);
    bool IsSupported(string imagePath);
    string ServiceName { get; }
} 