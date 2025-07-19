using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using MetadataExtractor;
using System.Text;

namespace ImageTagger.Infrastructure;

public class MetadataService : IMetadataService
{
    private readonly ILoggingService _loggingService;
    private readonly bool _createBackups;
    private readonly string[] _supportedFormats;

    public MetadataService(
        ILoggingService loggingService,
        bool createBackups = true,
        string[]? supportedFormats = null)
    {
        _loggingService = loggingService;
        _createBackups = createBackups;
        _supportedFormats = supportedFormats ?? new[] { "jpg", "jpeg", "png", "tiff", "tif" };
    }

    public async Task<bool> SaveTagsAsync(string imagePath, List<string> tags, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _loggingService.Log($"Attempting to save {tags.Count} tags to {imagePath}");

                if (!IsSupported(imagePath))
                {
                    _loggingService.Log($"Unsupported image format: {Path.GetExtension(imagePath)}", LogLevel.Warning);
                    return false;
                }

                // Create backup if enabled
                string? backupPath = null;
                if (_createBackups)
                {
                    backupPath = imagePath + ".backup";
                    File.Copy(imagePath, backupPath, true);
                    _loggingService.Log($"Created backup: {backupPath}", LogLevel.Debug);
                }

                try
                {
                    // Try to write metadata
                    var success = WriteTagsToImageFile(imagePath, tags);

                    if (success)
                    {
                        _loggingService.Log($"Successfully wrote tags to {imagePath}");
                        
                        // Remove backup on success
                        if (backupPath != null && File.Exists(backupPath))
                        {
                            try 
                            { 
                                File.Delete(backupPath); 
                                _loggingService.LogVerbose("Backup file removed successfully");
                            } 
                            catch (Exception ex) 
                            { 
                                _loggingService.LogException(ex, "Remove Backup File");
                            }
                        }
                        
                        return true;
                    }
                    else
                    {
                        // Restore backup on failure
                        if (backupPath != null && File.Exists(backupPath))
                        {
                            try 
                            { 
                                File.Copy(backupPath, imagePath, true); 
                                _loggingService.LogVerbose("Backup file restored due to failure");
                            } 
                            catch (Exception ex) 
                            { 
                                _loggingService.LogException(ex, "Restore Backup File");
                            }
                            _loggingService.Log($"Failed to write tags, restored backup", LogLevel.Warning);
                        }
                        
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    // Restore backup on exception
                    if (backupPath != null && File.Exists(backupPath))
                    {
                        try 
                        { 
                            File.Copy(backupPath, imagePath, true); 
                            _loggingService.LogVerbose("Backup file restored due to exception");
                        } 
                        catch (Exception backupEx) 
                        { 
                            _loggingService.LogException(backupEx, "Restore Backup File After Exception");
                        }
                        _loggingService.Log($"Exception during metadata writing, restored backup", LogLevel.Error);
                    }
                    
                    _loggingService.LogException(ex, "SaveTagsAsync");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException(ex, "SaveTagsAsync");
                return false;
            }
        }, cancellationToken);
    }

    public async Task<List<string>> ReadTagsAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!IsSupported(imagePath))
                {
                    return new List<string>();
                }

                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                var tags = new List<string>();

                // Try to read from different metadata formats
                foreach (var directory in directories)
                {
                    foreach (var tag in directory.Tags)
                    {
                        if (tag.Name.Contains("comment", StringComparison.OrdinalIgnoreCase) ||
                            tag.Name.Contains("description", StringComparison.OrdinalIgnoreCase) ||
                            tag.Name.Contains("keywords", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!string.IsNullOrEmpty(tag.Description))
                            {
                                tags.Add(tag.Description);
                            }
                        }
                    }
                }

                return tags;
            }
            catch (Exception ex)
            {
                _loggingService.LogException(ex, "ReadTagsAsync");
                return new List<string>();
            }
        }, cancellationToken);
    }

    public bool IsSupported(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return false;

        var extension = Path.GetExtension(imagePath).ToLowerInvariant().TrimStart('.');
        return _supportedFormats.Contains(extension);
    }

    public async Task<ImageInfo> GetImageInfoAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var fileInfo = new FileInfo(imagePath);
                var imageInfo = new ImageInfo
                {
                    FilePath = imagePath,
                    FileSize = fileInfo.Length,
                    CreatedDate = fileInfo.CreationTime,
                    ModifiedDate = fileInfo.LastWriteTime
                };

                // Try to read existing tags
                try
                {
                    imageInfo.Tags = ReadTagsAsync(imagePath, cancellationToken).Result;
                }
                catch (Exception tagEx)
                {
                    _loggingService.LogException(tagEx, "Read Tags in GetImageInfoAsync");
                    imageInfo.Tags = new List<string>();
                }

                return imageInfo;
            }
            catch (Exception ex)
            {
                _loggingService.LogException(ex, "GetImageInfoAsync");
                return new ImageInfo { FilePath = imagePath };
            }
        }, cancellationToken);
    }

    private bool WriteTagsToImageFile(string imagePath, List<string> tags)
    {
        try
        {
            var extension = Path.GetExtension(imagePath).ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" => WriteTagsToJpeg(imagePath, tags),
                ".png" => WriteTagsToPng(imagePath, tags),
                ".tiff" or ".tif" => WriteTagsToTiff(imagePath, tags),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "WriteTagsToImageFile");
            return false;
        }
    }

    private bool WriteTagsUsingSystemDrawing(string imagePath, List<string> tags)
    {
        try
        {
            using var image = System.Drawing.Image.FromFile(imagePath);
            
            // Set image properties (this is limited and may not work for all formats)
            foreach (var prop in image.PropertyItems)
            {
                if (prop.Id == 0x9286) // UserComment
                {
                    var tagsString = string.Join(", ", tags);
                    var bytes = Encoding.UTF8.GetBytes(tagsString);
                    prop.Value = bytes;
                    prop.Len = bytes.Length;
                    image.SetPropertyItem(prop);
                    break;
                }
            }

            // Save the modified image
            image.Save(imagePath);
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "WriteTagsUsingSystemDrawing");
            return false;
        }
    }

    private bool WriteTagsToJpeg(string imagePath, List<string> tags)
    {
        // Placeholder for JPEG metadata writing
        _loggingService.Log("JPEG metadata writing not implemented - using fallback method", LogLevel.Warning);
        return WriteTagsUsingSystemDrawing(imagePath, tags);
    }

    private bool WriteTagsToPng(string imagePath, List<string> tags)
    {
        // Placeholder for PNG metadata writing
        _loggingService.Log("PNG metadata writing not implemented - using fallback method", LogLevel.Warning);
        return WriteTagsUsingSystemDrawing(imagePath, tags);
    }

    private bool WriteTagsToTiff(string imagePath, List<string> tags)
    {
        // Placeholder for TIFF metadata writing
        _loggingService.Log("TIFF metadata writing not implemented - using fallback method", LogLevel.Warning);
        return WriteTagsUsingSystemDrawing(imagePath, tags);
    }
} 