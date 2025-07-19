using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using MetadataExtractor;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Iptc;
using SixLabors.ImageSharp.Metadata.Profiles.Xmp;
using ImageInfo = ImageTagger.Core.Models.ImageInfo;

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
                _loggingService.Log($"=== METADATA WRITING START ===");
                _loggingService.Log($"Image path: {imagePath}");
                _loggingService.Log($"Tags to write: {string.Join(", ", tags)}");
                _loggingService.Log($"Tags count: {tags.Count}");
                _loggingService.Log($"Create backups: {_createBackups}");
                _loggingService.Log($"Supported formats: {string.Join(", ", _supportedFormats)}");

                if (!IsSupported(imagePath))
                {
                    _loggingService.Log($"Unsupported image format: {Path.GetExtension(imagePath)}", LogLevel.Warning);
                    _loggingService.Log($"=== METADATA WRITING FAILED - UNSUPPORTED FORMAT ===");
                    return false;
                }

                _loggingService.Log($"Image format is supported: {Path.GetExtension(imagePath)}");

                // Check file exists and get file info
                if (!File.Exists(imagePath))
                {
                    _loggingService.Log($"Image file does not exist: {imagePath}", LogLevel.Error);
                    _loggingService.Log($"=== METADATA WRITING FAILED - FILE NOT FOUND ===");
                    return false;
                }

                var fileInfo = new FileInfo(imagePath);
                _loggingService.Log($"File size: {fileInfo.Length} bytes");
                _loggingService.Log($"File attributes: {fileInfo.Attributes}");
                _loggingService.Log($"File is read-only: {fileInfo.IsReadOnly}");

                // Check if file is read-only
                if (fileInfo.IsReadOnly)
                {
                    _loggingService.Log($"File is read-only, attempting to make writable...", LogLevel.Warning);
                    try
                    {
                        fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                        _loggingService.Log($"Successfully made file writable");
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogException(ex, "Make file writable");
                        _loggingService.Log($"=== METADATA WRITING FAILED - CANNOT MAKE WRITABLE ===");
                        return false;
                    }
                }

                // Create backup if enabled
                string? backupPath = null;
                if (_createBackups)
                {
                    backupPath = imagePath + ".backup";
                    _loggingService.Log($"Creating backup at: {backupPath}");
                    
                    try
                    {
                        File.Copy(imagePath, backupPath, true);
                        var backupInfo = new FileInfo(backupPath);
                        _loggingService.Log($"Backup created successfully. Size: {backupInfo.Length} bytes");
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogException(ex, "Create backup");
                        _loggingService.Log($"=== METADATA WRITING FAILED - BACKUP CREATION ===");
                        return false;
                    }
                }

                try
                {
                    // Try to write metadata
                    _loggingService.Log($"Starting metadata writing process...");
                    var success = WriteTagsToImageFile(imagePath, tags);

                    if (success)
                    {
                        _loggingService.Log($"Successfully wrote tags to {imagePath}");
                        
                        // Verify the write operation
                        try
                        {
                            var verificationTags = ReadTagsAsync(imagePath, cancellationToken).Result;
                            _loggingService.Log($"Verification: Read back {verificationTags.Count} tags from file");
                            _loggingService.Log($"Verification tags: {string.Join(", ", verificationTags)}");
                        }
                        catch (Exception verifyEx)
                        {
                            _loggingService.LogException(verifyEx, "Verify metadata write");
                            _loggingService.Log("Warning: Could not verify written metadata", LogLevel.Warning);
                        }
                        
                        // Remove backup on success
                        if (backupPath != null && File.Exists(backupPath))
                        {
                            try 
                            { 
                                File.Delete(backupPath); 
                                _loggingService.Log("Backup file removed successfully after successful write");
                            } 
                            catch (Exception ex) 
                            { 
                                _loggingService.LogException(ex, "Remove Backup File");
                                _loggingService.Log("Warning: Could not remove backup file", LogLevel.Warning);
                            }
                        }
                        
                        _loggingService.Log($"=== METADATA WRITING SUCCESS ===");
                        return true;
                    }
                    else
                    {
                        _loggingService.Log($"Failed to write tags to {imagePath}", LogLevel.Error);
                        
                        // Restore backup on failure
                        if (backupPath != null && File.Exists(backupPath))
                        {
                            _loggingService.Log($"Restoring backup from: {backupPath}");
                            try 
                            { 
                                File.Copy(backupPath, imagePath, true); 
                                _loggingService.Log("Backup file restored due to write failure");
                            } 
                            catch (Exception ex) 
                            { 
                                _loggingService.LogException(ex, "Restore Backup File");
                                _loggingService.Log("CRITICAL: Could not restore backup file!", LogLevel.Error);
                            }
                        }
                        
                        _loggingService.Log($"=== METADATA WRITING FAILED - WRITE OPERATION ===");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogException(ex, "Metadata writing operation");
                    
                    // Restore backup on exception
                    if (backupPath != null && File.Exists(backupPath))
                    {
                        _loggingService.Log($"Restoring backup due to exception from: {backupPath}");
                        try 
                        { 
                            File.Copy(backupPath, imagePath, true); 
                            _loggingService.Log("Backup file restored due to exception");
                        } 
                        catch (Exception backupEx) 
                        { 
                            _loggingService.LogException(backupEx, "Restore Backup File After Exception");
                            _loggingService.Log("CRITICAL: Could not restore backup file after exception!", LogLevel.Error);
                        }
                    }
                    
                    _loggingService.Log($"=== METADATA WRITING FAILED - EXCEPTION ===");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException(ex, "SaveTagsAsync outer exception");
                _loggingService.Log($"=== METADATA WRITING FAILED - OUTER EXCEPTION ===");
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
                _loggingService.Log($"=== METADATA READING START ===");
                _loggingService.Log($"Image path: {imagePath}");
                
                if (!IsSupported(imagePath))
                {
                    _loggingService.Log($"Unsupported image format: {Path.GetExtension(imagePath)}", LogLevel.Warning);
                    _loggingService.Log($"=== METADATA READING FAILED - UNSUPPORTED FORMAT ===");
                    return new List<string>();
                }

                _loggingService.Log($"Image format is supported: {Path.GetExtension(imagePath)}");
                
                if (!File.Exists(imagePath))
                {
                    _loggingService.Log($"Image file does not exist: {imagePath}", LogLevel.Error);
                    _loggingService.Log($"=== METADATA READING FAILED - FILE NOT FOUND ===");
                    return new List<string>();
                }

                _loggingService.Log("Reading metadata using ImageMetadataReader...");
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                _loggingService.Log($"Found {directories.Count} metadata directories");
                
                var tags = new List<string>();
                var totalTagsFound = 0;

                // Try to read from different metadata formats
                foreach (var directory in directories)
                {
                    _loggingService.Log($"Processing directory: {directory.Name} with {directory.Tags.Count} tags");
                    
                    foreach (var tag in directory.Tags)
                    {
                        totalTagsFound++;
                        
                        if (tag.Name.Contains("comment", StringComparison.OrdinalIgnoreCase) ||
                            tag.Name.Contains("description", StringComparison.OrdinalIgnoreCase) ||
                            tag.Name.Contains("keywords", StringComparison.OrdinalIgnoreCase))
                        {
                            _loggingService.Log($"Found relevant tag: {tag.Name} = {tag.Description}");
                            
                            if (!string.IsNullOrEmpty(tag.Description))
                            {
                                tags.Add(tag.Description);
                                _loggingService.Log($"Added tag: {tag.Description}");
                            }
                        }
                        else
                        {
                            _loggingService.Log($"Skipped tag: {tag.Name} = {tag.Description}");
                        }
                    }
                }

                _loggingService.Log($"Total tags examined: {totalTagsFound}");
                _loggingService.Log($"Relevant tags found: {tags.Count}");
                _loggingService.Log($"Tags: {string.Join(", ", tags)}");
                _loggingService.Log($"=== METADATA READING SUCCESS ===");
                
                return tags;
            }
            catch (Exception ex)
            {
                _loggingService.LogException(ex, "ReadTagsAsync");
                _loggingService.Log($"=== METADATA READING FAILED - EXCEPTION ===");
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
            _loggingService.Log($"=== FORMAT-SPECIFIC METADATA WRITING ===");
            _loggingService.Log($"Image path: {imagePath}");
            _loggingService.Log($"Tags to write: {string.Join(", ", tags)}");
            
            var extension = Path.GetExtension(imagePath).ToLowerInvariant();
            _loggingService.Log($"Detected file extension: {extension}");

            var result = extension switch
            {
                ".jpg" or ".jpeg" => WriteTagsToJpeg(imagePath, tags),
                ".png" => WriteTagsToPng(imagePath, tags),
                ".tiff" or ".tif" => WriteTagsToTiff(imagePath, tags),
                _ => false
            };

            _loggingService.Log($"Format-specific write result: {result}");
            _loggingService.Log($"=== FORMAT-SPECIFIC METADATA WRITING COMPLETE ===");
            
            return result;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "WriteTagsToImageFile");
            _loggingService.Log($"=== FORMAT-SPECIFIC METADATA WRITING FAILED ===");
            return false;
        }
    }

    private bool WriteTagsUsingSystemDrawing(string imagePath, List<string> tags)
    {
        try
        {
            _loggingService.Log($"=== SYSTEM.DRAWING METADATA WRITING ===");
            _loggingService.Log($"Image path: {imagePath}");
            _loggingService.Log($"Tags to write: {string.Join(", ", tags)}");
            
            _loggingService.Log("Loading image using System.Drawing.Image.FromFile...");
            using var image = System.Drawing.Image.FromFile(imagePath);
            _loggingService.Log($"Image loaded successfully. Size: {image.Width}x{image.Height}, Format: {image.PixelFormat}");
            
            _loggingService.Log($"Image has {image.PropertyItems.Length} property items");
            
            // Log existing property items for debugging
            for (int i = 0; i < image.PropertyItems.Length; i++)
            {
                var prop = image.PropertyItems[i];
                _loggingService.Log($"Property {i}: ID=0x{prop.Id:X4}, Type={prop.Type}, Length={prop.Len}");
            }
            
            // Set image properties (this is limited and may not work for all formats)
            bool foundUserComment = false;
            foreach (var prop in image.PropertyItems)
            {
                if (prop.Id == 0x9286) // UserComment
                {
                    _loggingService.Log("Found existing UserComment property, updating it...");
                    var tagsString = string.Join(", ", tags);
                    var bytes = Encoding.UTF8.GetBytes(tagsString);
                    prop.Value = bytes;
                    prop.Len = bytes.Length;
                    image.SetPropertyItem(prop);
                    foundUserComment = true;
                    _loggingService.Log($"Updated UserComment property with {bytes.Length} bytes");
                    break;
                }
            }
            
            if (!foundUserComment)
            {
                _loggingService.Log("No existing UserComment property found, attempting to create new one...", LogLevel.Warning);
                try
                {
                    var tagsString = string.Join(", ", tags);
                    var bytes = Encoding.UTF8.GetBytes(tagsString);
                    
                    // Create new property item using reflection since PropertyItem constructor is not public
                    var newProp = (System.Drawing.Imaging.PropertyItem)typeof(System.Drawing.Imaging.PropertyItem)
                        .GetConstructor(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null)!
                        .Invoke(null);
                    
                    typeof(System.Drawing.Imaging.PropertyItem).GetProperty("Id")!.SetValue(newProp, 0x9286); // UserComment
                    typeof(System.Drawing.Imaging.PropertyItem).GetProperty("Type")!.SetValue(newProp, (short)2); // ASCII - use short instead of int
                    typeof(System.Drawing.Imaging.PropertyItem).GetProperty("Len")!.SetValue(newProp, bytes.Length);
                    typeof(System.Drawing.Imaging.PropertyItem).GetProperty("Value")!.SetValue(newProp, bytes);
                    
                    image.SetPropertyItem(newProp);
                    _loggingService.Log($"Created new UserComment property with {bytes.Length} bytes");
                }
                catch (Exception ex)
                {
                    _loggingService.LogException(ex, "Create new UserComment property");
                    _loggingService.Log("Failed to create new UserComment property", LogLevel.Warning);
                }
            }

            // Save the modified image
            _loggingService.Log("Saving modified image...");
            image.Save(imagePath);
            _loggingService.Log("Image saved successfully");
            
            _loggingService.Log($"=== SYSTEM.DRAWING METADATA WRITING SUCCESS ===");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "WriteTagsUsingSystemDrawing");
            _loggingService.Log($"=== SYSTEM.DRAWING METADATA WRITING FAILED ===");
            return false;
        }
    }

    private bool WriteTagsToJpeg(string imagePath, List<string> tags)
    {
        _loggingService.Log($"=== JPEG METADATA WRITING ===");
        _loggingService.Log($"Image path: {imagePath}");
        _loggingService.Log($"Tags to write: {string.Join(", ", tags)}");
        
        // Try multiple methods in order of preference - prioritize embedded metadata
        var methods = new[]
        {
            new { Name = "ImageSharp Embedded", Method = (Func<string, List<string>, bool>)WriteTagsWithImageSharp },
            new { Name = "System.Drawing (Optimized)", Method = (Func<string, List<string>, bool>)WriteTagsWithSystemDrawingOptimized },
            new { Name = "System.Drawing (Fallback)", Method = (Func<string, List<string>, bool>)WriteTagsUsingSystemDrawing },
            new { Name = "File-Based Metadata", Method = (Func<string, List<string>, bool>)WriteTagsToCompanionFile }
        };
        
        foreach (var method in methods)
        {
            _loggingService.Log($"Trying {method.Name} method...");
            try
            {
                var result = method.Method(imagePath, tags);
                if (result)
                {
                    _loggingService.Log($"{method.Name} method succeeded");
                    _loggingService.Log($"JPEG metadata writing result: {result}");
                    _loggingService.Log($"=== JPEG METADATA WRITING COMPLETE ===");
                    return true;
                }
                else
                {
                    _loggingService.Log($"{method.Name} method failed, trying next method...", LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException(ex, $"{method.Name} method");
                _loggingService.Log($"{method.Name} method threw exception, trying next method...", LogLevel.Warning);
            }
        }
        
        _loggingService.Log("All JPEG metadata writing methods failed", LogLevel.Error);
        _loggingService.Log($"JPEG metadata writing result: False");
        _loggingService.Log($"=== JPEG METADATA WRITING COMPLETE ===");
        return false;
    }

    private bool WriteTagsWithImageSharp(string imagePath, List<string> tags)
    {
        try
        {
            _loggingService.Log("=== IMAGESHARP EMBEDDED METADATA WRITING ===");
            _loggingService.Log($"Image path: {imagePath}");
            _loggingService.Log($"Tags to write: {string.Join(", ", tags)}");
            
            // Load image with ImageSharp
            _loggingService.Log("Loading image with ImageSharp...");
            using var image = Image.Load(imagePath);
            _loggingService.Log($"Image loaded successfully. Size: {image.Width}x{image.Height}");
            
            // Get or create EXIF profile
            var exifProfile = image.Metadata.ExifProfile ?? new ExifProfile();
            _loggingService.Log("EXIF profile ready");
            
            // Create tags string
            var tagsString = string.Join(", ", tags);
            _loggingService.Log($"Tags string: {tagsString}");
            
            // Add tags to EXIF UserComment (0x9286)
            exifProfile.SetValue(ExifTag.UserComment, tagsString);
            _loggingService.Log("Added UserComment to EXIF");
            
            // Add tags to EXIF ImageDescription (0x010E)
            exifProfile.SetValue(ExifTag.ImageDescription, tagsString);
            _loggingService.Log("Added ImageDescription to EXIF");
            
            // Add software tag
            exifProfile.SetValue(ExifTag.Software, "ImageTagger v1.0");
            _loggingService.Log("Added Software tag to EXIF");
            
            // Set EXIF profile on image
            image.Metadata.ExifProfile = exifProfile;
            _loggingService.Log("EXIF profile set on image");
            
            // Get or create IPTC profile for keywords
            var iptcProfile = image.Metadata.IptcProfile ?? new IptcProfile();
            _loggingService.Log("IPTC profile ready");
            
            // Add keywords to IPTC (this is what ACDSee reads)
            foreach (var tag in tags)
            {
                iptcProfile.SetValue(IptcTag.Keywords, tag);
                _loggingService.Log($"Added IPTC keyword: {tag}");
            }
            
            // Set IPTC profile on image
            image.Metadata.IptcProfile = iptcProfile;
            _loggingService.Log("IPTC profile set on image");
            
            // Save image with embedded metadata
            _loggingService.Log("Saving image with embedded metadata...");
            image.Save(imagePath);
            _loggingService.Log("Image saved successfully with embedded metadata");
            
            _loggingService.Log("=== IMAGESHARP EMBEDDED METADATA WRITING SUCCESS ===");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.Log($"ImageSharp embedded metadata writing failed: {ex.Message}");
            _loggingService.Log($"Exception details: {ex}");
            return false;
        }
    }

    private bool WriteTagsToCompanionFile(string imagePath, List<string> tags)
    {
        try
        {
            _loggingService.Log("=== FILE-BASED METADATA WRITING ===");
            _loggingService.Log($"Image path: {imagePath}");
            _loggingService.Log($"Tags to write: {string.Join(", ", tags)}");
            
            // Create companion metadata file
            var metadataPath = Path.ChangeExtension(imagePath, ".tags");
            _loggingService.Log($"Creating companion metadata file: {metadataPath}");
            
            // Write tags to companion file
            var tagsString = string.Join(", ", tags);
            File.WriteAllText(metadataPath, tagsString);
            _loggingService.Log($"Successfully wrote {tags.Count} tags to companion file");
            
            // Also create a JSON metadata file for more structured data
            var jsonMetadataPath = Path.ChangeExtension(imagePath, ".metadata.json");
            _loggingService.Log($"Creating JSON metadata file: {jsonMetadataPath}");
            
            var metadata = new
            {
                ImagePath = imagePath,
                Tags = tags,
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "ImageTagger",
                Version = "1.0"
            };
            
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(jsonMetadataPath, jsonContent);
            _loggingService.Log($"Successfully wrote JSON metadata to: {jsonMetadataPath}");
            
            _loggingService.Log("=== FILE-BASED METADATA WRITING SUCCESS ===");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "WriteTagsToCompanionFile");
            _loggingService.Log("=== FILE-BASED METADATA WRITING FAILED ===");
            return false;
        }
    }

    private bool WriteTagsWithSystemDrawingOptimized(string imagePath, List<string> tags)
    {
        try
        {
            _loggingService.Log("=== SYSTEM.DRAWING OPTIMIZED METADATA WRITING ===");
            _loggingService.Log($"Image path: {imagePath}");
            _loggingService.Log($"Tags to write: {string.Join(", ", tags)}");
            
            _loggingService.Log("Loading image using System.Drawing.Image.FromFile...");
            using var image = System.Drawing.Image.FromFile(imagePath);
            _loggingService.Log($"Image loaded successfully. Size: {image.Width}x{image.Height}, Format: {image.PixelFormat}");
            
            _loggingService.Log($"Image has {image.PropertyItems.Length} property items");
            
            // Log existing property items for debugging
            for (int i = 0; i < image.PropertyItems.Length; i++)
            {
                var prop = image.PropertyItems[i];
                _loggingService.Log($"Property {i}: ID=0x{prop.Id:X4}, Type={prop.Type}, Length={prop.Len}");
            }
            
            // Set image properties (this is limited and may not work for all formats)
            bool foundUserComment = false;
            foreach (var prop in image.PropertyItems)
            {
                if (prop.Id == 0x9286) // UserComment
                {
                    _loggingService.Log("Found existing UserComment property, updating it...");
                    var tagsString = string.Join(", ", tags);
                    var bytes = Encoding.UTF8.GetBytes(tagsString);
                    prop.Value = bytes;
                    prop.Len = bytes.Length;
                    image.SetPropertyItem(prop);
                    foundUserComment = true;
                    _loggingService.Log($"Updated UserComment property with {bytes.Length} bytes");
                    break;
                }
            }
            
            if (!foundUserComment)
            {
                _loggingService.Log("No existing UserComment property found, attempting to create new one...", LogLevel.Warning);
                try
                {
                    var tagsString = string.Join(", ", tags);
                    var bytes = Encoding.UTF8.GetBytes(tagsString);
                    
                    // Create new property item using reflection since PropertyItem constructor is not public
                    var newProp = (System.Drawing.Imaging.PropertyItem)typeof(System.Drawing.Imaging.PropertyItem)
                        .GetConstructor(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null)!
                        .Invoke(null);
                    
                    typeof(System.Drawing.Imaging.PropertyItem).GetProperty("Id")!.SetValue(newProp, 0x9286); // UserComment
                    typeof(System.Drawing.Imaging.PropertyItem).GetProperty("Type")!.SetValue(newProp, (short)2); // ASCII - use short instead of int
                    typeof(System.Drawing.Imaging.PropertyItem).GetProperty("Len")!.SetValue(newProp, bytes.Length);
                    typeof(System.Drawing.Imaging.PropertyItem).GetProperty("Value")!.SetValue(newProp, bytes);
                    
                    image.SetPropertyItem(newProp);
                    _loggingService.Log($"Created new UserComment property with {bytes.Length} bytes");
                }
                catch (Exception ex)
                {
                    _loggingService.LogException(ex, "Create new UserComment property");
                    _loggingService.Log("Failed to create new UserComment property", LogLevel.Warning);
                }
            }

            // Save the modified image with optimized parameters
            _loggingService.Log("Saving modified image with optimized parameters...");
            
            // Try different save approaches
            var saveMethods = new[]
            {
                new { Name = "High Quality Save", Method = (Action)delegate 
                {
                    var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                    encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
                    var jpegCodec = System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders()
                        .FirstOrDefault(codec => codec.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
                    if (jpegCodec != null)
                    {
                        image.Save(imagePath, jpegCodec, encoderParams);
                    }
                    else
                    {
                        image.Save(imagePath);
                    }
                }},
                new { Name = "JPEG Save", Method = (Action)delegate { image.Save(imagePath, System.Drawing.Imaging.ImageFormat.Jpeg); } },
                new { Name = "Default Save", Method = (Action)delegate { image.Save(imagePath); } }
            };
            
            foreach (var saveMethod in saveMethods)
            {
                try
                {
                    _loggingService.Log($"Trying {saveMethod.Name}...");
                    saveMethod.Method();
                    _loggingService.Log($"Image saved successfully using {saveMethod.Name}");
                    _loggingService.Log("=== SYSTEM.DRAWING OPTIMIZED METADATA WRITING SUCCESS ===");
                    return true;
                }
                catch (Exception ex)
                {
                    _loggingService.LogException(ex, saveMethod.Name);
                    _loggingService.Log($"{saveMethod.Name} failed, trying next method...", LogLevel.Warning);
                }
            }
            
            _loggingService.Log("All System.Drawing save methods failed", LogLevel.Error);
            _loggingService.Log("=== SYSTEM.DRAWING OPTIMIZED METADATA WRITING FAILED ===");
            return false;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "WriteTagsWithSystemDrawingOptimized");
            _loggingService.Log("=== SYSTEM.DRAWING OPTIMIZED METADATA WRITING FAILED ===");
            return false;
        }
    }

    private bool WriteTagsToPng(string imagePath, List<string> tags)
    {
        _loggingService.Log($"=== PNG METADATA WRITING ===");
        _loggingService.Log($"Image path: {imagePath}");
        _loggingService.Log($"Tags to write: {string.Join(", ", tags)}");
        
        // Try multiple methods in order of preference - prioritize embedded metadata
        var methods = new[]
        {
            new { Name = "ImageSharp Embedded", Method = (Func<string, List<string>, bool>)WriteTagsWithImageSharpPng },
            new { Name = "System.Drawing (Optimized)", Method = (Func<string, List<string>, bool>)WriteTagsWithSystemDrawingOptimized },
            new { Name = "System.Drawing (Fallback)", Method = (Func<string, List<string>, bool>)WriteTagsUsingSystemDrawing },
            new { Name = "File-Based Metadata", Method = (Func<string, List<string>, bool>)WriteTagsToCompanionFile }
        };
        
        foreach (var method in methods)
        {
            _loggingService.Log($"Trying {method.Name} method...");
            try
            {
                var result = method.Method(imagePath, tags);
                if (result)
                {
                    _loggingService.Log($"{method.Name} method succeeded");
                    _loggingService.Log($"PNG metadata writing result: {result}");
                    _loggingService.Log($"=== PNG METADATA WRITING COMPLETE ===");
                    return true;
                }
                else
                {
                    _loggingService.Log($"{method.Name} method failed, trying next method...", LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException(ex, $"{method.Name} method");
                _loggingService.Log($"{method.Name} method threw exception, trying next method...", LogLevel.Warning);
            }
        }
        
        _loggingService.Log("All PNG metadata writing methods failed", LogLevel.Error);
        _loggingService.Log($"PNG metadata writing result: False");
        _loggingService.Log($"=== PNG METADATA WRITING COMPLETE ===");
        return false;
    }

    private bool WriteTagsWithImageSharpPng(string imagePath, List<string> tags)
    {
        try
        {
            _loggingService.Log("=== IMAGESHARP PNG EMBEDDED METADATA WRITING ===");
            _loggingService.Log($"Image path: {imagePath}");
            _loggingService.Log($"Tags to write: {string.Join(", ", tags)}");
            
            // Load image with ImageSharp
            _loggingService.Log("Loading PNG image with ImageSharp...");
            using var image = Image.Load(imagePath);
            _loggingService.Log($"PNG image loaded successfully. Size: {image.Width}x{image.Height}");
            
            // Create tags string
            var tagsString = string.Join(", ", tags);
            _loggingService.Log($"Tags string: {tagsString}");
            
            // For PNG, we'll use EXIF profile as well (PNG can store EXIF data)
            var exifProfile = image.Metadata.ExifProfile ?? new ExifProfile();
            
            // Add UserComment to EXIF
            exifProfile.SetValue(ExifTag.UserComment, tagsString);
            _loggingService.Log("Added UserComment to EXIF");
            
            // Add ImageDescription to EXIF
            exifProfile.SetValue(ExifTag.ImageDescription, tagsString);
            _loggingService.Log("Added ImageDescription to EXIF");
            
            // Add Software tag
            exifProfile.SetValue(ExifTag.Software, "ImageTagger v1.0");
            _loggingService.Log("Added Software tag to EXIF");
            
            // Set EXIF profile back to image
            image.Metadata.ExifProfile = exifProfile;
            _loggingService.Log("EXIF profile set on PNG image");
            
            // Save image with embedded metadata
            _loggingService.Log("Saving PNG image with embedded metadata...");
            image.Save(imagePath);
            _loggingService.Log("PNG image saved successfully with embedded metadata");
            
            _loggingService.Log("=== IMAGESHARP PNG EMBEDDED METADATA WRITING SUCCESS ===");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "WriteTagsWithImageSharpPng");
            _loggingService.Log("=== IMAGESHARP PNG EMBEDDED METADATA WRITING FAILED ===");
            return false;
        }
    }

    private bool WriteTagsToTiff(string imagePath, List<string> tags)
    {
        _loggingService.Log($"=== TIFF METADATA WRITING ===");
        _loggingService.Log($"Image path: {imagePath}");
        _loggingService.Log($"Tags to write: {string.Join(", ", tags)}");
        
        // Try multiple methods in order of preference - prioritize embedded metadata
        var methods = new[]
        {
            new { Name = "ImageSharp Embedded", Method = (Func<string, List<string>, bool>)WriteTagsWithImageSharp },
            new { Name = "System.Drawing (Optimized)", Method = (Func<string, List<string>, bool>)WriteTagsWithSystemDrawingOptimized },
            new { Name = "System.Drawing (Fallback)", Method = (Func<string, List<string>, bool>)WriteTagsUsingSystemDrawing },
            new { Name = "File-Based Metadata", Method = (Func<string, List<string>, bool>)WriteTagsToCompanionFile }
        };
        
        foreach (var method in methods)
        {
            _loggingService.Log($"Trying {method.Name} method...");
            try
            {
                var result = method.Method(imagePath, tags);
                if (result)
                {
                    _loggingService.Log($"{method.Name} method succeeded");
                    _loggingService.Log($"TIFF metadata writing result: {result}");
                    _loggingService.Log($"=== TIFF METADATA WRITING COMPLETE ===");
                    return true;
                }
                else
                {
                    _loggingService.Log($"{method.Name} method failed, trying next method...", LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException(ex, $"{method.Name} method");
                _loggingService.Log($"{method.Name} method threw exception, trying next method...", LogLevel.Warning);
            }
        }
        
        _loggingService.Log("All TIFF metadata writing methods failed", LogLevel.Error);
        _loggingService.Log($"TIFF metadata writing result: False");
        _loggingService.Log($"=== TIFF METADATA WRITING COMPLETE ===");
        return false;
    }
} 