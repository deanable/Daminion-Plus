using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using System.Text;
using System.Text.Json;

namespace ImageTagger.Services;

public class CloudApiTaggingService : IImageTaggingService
{
    private readonly ILoggingService _loggingService;
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly int _timeoutSeconds;

    public string ServiceName => "Cloud API";

    public CloudApiTaggingService(
        ILoggingService loggingService,
        string endpoint,
        string apiKey,
        int timeoutSeconds = 30)
    {
        _loggingService = loggingService;
        _endpoint = endpoint;
        _apiKey = apiKey;
        _timeoutSeconds = timeoutSeconds;
        
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_timeoutSeconds)
        };
    }

    public async Task<TaggingResult> TagImageAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new TaggingResult
        {
            ImagePath = imagePath,
            Method = ServiceName,
            Success = false
        };

        try
        {
            _loggingService.Log($"Starting cloud API tagging for {imagePath}");

            if (_apiKey == "your-api-key-here")
            {
                throw new InvalidOperationException("Cloud API key not configured. Please update the API key in configuration.");
            }

            // Read image file
            var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            var base64Image = Convert.ToBase64String(imageBytes);

            // Prepare request payload (example for Azure Computer Vision)
            var requestData = new
            {
                url = "", // For local files, we'll use the base64 data
                features = new[]
                {
                    new { name = "tags", confidence = 0.8 }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Add headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);

            // Make API call
            var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"API request failed with status {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _loggingService.Log($"Cloud API response received: {responseContent.Length} characters", LogLevel.Debug);

            // Parse response (example for Azure Computer Vision)
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var tags = new List<TagResult>();
            if (responseData.TryGetProperty("tags", out var tagsElement))
            {
                foreach (var tag in tagsElement.EnumerateArray())
                {
                    if (tag.TryGetProperty("name", out var nameElement))
                    {
                        var tagName = nameElement.GetString();
                        if (!string.IsNullOrEmpty(tagName))
                        {
                            var confidence = 1.0;
                            if (tag.TryGetProperty("confidence", out var confidenceElement))
                            {
                                confidence = confidenceElement.GetDouble();
                            }

                            tags.Add(new TagResult
                            {
                                Tag = tagName,
                                Confidence = confidence,
                                Source = ServiceName
                            });
                        }
                    }
                }
            }

            result.Tags = tags;
            result.Success = true;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _loggingService.Log($"Cloud API returned {result.Tags.Count} tags", LogLevel.Info);
            _loggingService.LogPerformance("Cloud API Tagging", result.ProcessingTime);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _loggingService.LogException(ex, "Cloud API Tagging");
            throw new ApplicationException($"Cloud API tagging failed: {ex.Message}", ex);
        }

        return result;
    }

    public bool IsSupported(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return false;

        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" or ".tif";
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
} 