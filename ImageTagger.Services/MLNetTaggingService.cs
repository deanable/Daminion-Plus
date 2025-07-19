using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using System.Text;

namespace ImageTagger.Services;

public class MLNetTaggingService : IImageTaggingService
{
    private readonly ILoggingService _loggingService;
    private readonly string _modelPath;
    private readonly string _labelsPath;
    private readonly int _imageWidth;
    private readonly int _imageHeight;
    private readonly int _maxTags;
    private readonly double _confidenceThreshold;

    public string ServiceName => $"ML.NET ({Path.GetFileNameWithoutExtension(_modelPath)})";

    public MLNetTaggingService(
        ILoggingService loggingService,
        string modelPath,
        string labelsPath,
        int imageWidth = 224,
        int imageHeight = 224,
        int maxTags = 3,
        double confidenceThreshold = 0.1)
    {
        _loggingService = loggingService;
        _modelPath = modelPath;
        _labelsPath = labelsPath;
        _imageWidth = imageWidth;
        _imageHeight = imageHeight;
        _maxTags = maxTags;
        _confidenceThreshold = confidenceThreshold;
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
            _loggingService.Log($"Starting ML.NET tagging for {imagePath}");
            _loggingService.LogVerbose($"Model path: {_modelPath}");
            _loggingService.LogVerbose($"Labels path: {_labelsPath}");
            _loggingService.LogVerbose($"Image dimensions: {_imageWidth}x{_imageHeight}");
            _loggingService.LogVerbose($"Max tags: {_maxTags}, Confidence threshold: {_confidenceThreshold}");

            // Verify model and labels exist
            if (!File.Exists(_modelPath))
            {
                throw new FileNotFoundException($"ONNX model not found: {_modelPath}");
            }

            if (!File.Exists(_labelsPath))
            {
                throw new FileNotFoundException($"Labels file not found: {_labelsPath}");
            }

            // Load labels
            var labels = await File.ReadAllLinesAsync(_labelsPath, cancellationToken);
            if (labels.Length == 0)
            {
                throw new InvalidOperationException("Labels file is empty");
            }

            _loggingService.Log($"Loaded {labels.Length} labels", LogLevel.Debug);
            
            // Check if labels are generic class names and map to ImageNet labels if needed
            if (labels.Length > 0 && labels[0].StartsWith("class_"))
            {
                _loggingService.LogVerbose("Detected generic class labels, mapping to ImageNet labels...");
                labels = MapGenericLabelsToImageNet(labels);
                _loggingService.LogVerbose($"Mapped {labels.Length} generic labels to ImageNet labels");
            }

            // Run ML.NET inference
            _loggingService.LogVerbose("Starting ML.NET inference pipeline");
            var predictions = await Task.Run(() => RunMLNetInference(imagePath, labels), cancellationToken);
            _loggingService.LogVerbose($"ML.NET inference completed, got {predictions.Count} predictions");

            // Convert to TagResult objects
            result.Tags = predictions
                .Where(p => p.Score >= _confidenceThreshold)
                .Take(_maxTags)
                .Select(p => new TagResult
                {
                    Tag = p.Label,
                    Confidence = p.Score,
                    Source = ServiceName
                })
                .ToList();

            result.Success = true;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _loggingService.Log($"ML.NET generated {result.Tags.Count} tags successfully", LogLevel.Info);
            _loggingService.LogPerformance("ML.NET Tagging", result.ProcessingTime);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _loggingService.LogException(ex, "ML.NET Tagging");
            throw new ApplicationException($"ML.NET inference failed: {ex.Message}", ex);
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

    private List<(string Label, double Score)> RunMLNetInference(string imagePath, string[] labels)
    {
        // Create MLContext
        var mlContext = new MLContext(seed: 1);

        // Define input data
        var data = new List<ImageInput> { new() { ImagePath = imagePath } };
        var imageData = mlContext.Data.LoadFromEnumerable(data);

        // Get ONNX input and output column names programmatically
        var (inputColumnName, outputColumnName) = GetOnnxColumnNames(_modelPath);
        _loggingService.Log($"Detected ONNX input column name: {inputColumnName}", LogLevel.Debug);
        _loggingService.Log($"Detected ONNX output column name: {outputColumnName}", LogLevel.Debug);

        // Log ONNX model details for debugging
        LogOnnxModelDetails(_modelPath);

        // Define pipeline
        _loggingService.LogVerbose("Creating ML.NET pipeline...");
        var pipeline = mlContext.Transforms.LoadImages(
                outputColumnName: inputColumnName,
                imageFolder: Path.GetDirectoryName(imagePath),
                inputColumnName: nameof(ImageInput.ImagePath))
            .Append(mlContext.Transforms.ResizeImages(
                outputColumnName: inputColumnName,
                imageWidth: _imageWidth,
                imageHeight: _imageHeight,
                inputColumnName: inputColumnName))
            .Append(mlContext.Transforms.ExtractPixels(outputColumnName: inputColumnName))
            .Append(mlContext.Transforms.ApplyOnnxModel(
                modelFile: _modelPath,
                outputColumnNames: new[] { outputColumnName },
                inputColumnNames: new[] { inputColumnName }));

        _loggingService.LogVerbose("Pipeline created successfully");

        // Fit and transform
        _loggingService.LogVerbose("Fitting pipeline...");
        var model = pipeline.Fit(imageData);
        _loggingService.LogVerbose("Pipeline fitted successfully");

        // Create prediction engine with dynamic output mapping
        _loggingService.LogVerbose("Creating prediction engine...");
        var predictionEngine = mlContext.Model.CreatePredictionEngine<ImageInput, DynamicImagePrediction>(model);
        _loggingService.LogVerbose("Prediction engine created successfully");

        // Make prediction
        _loggingService.LogVerbose("Making prediction...");
        var prediction = predictionEngine.Predict(new ImageInput { ImagePath = imagePath });
        _loggingService.LogVerbose("Prediction completed");

        // Comprehensive debug logging
        _loggingService.LogVerbose($"Prediction object type: {prediction.GetType().Name}");
        _loggingService.LogVerbose($"Prediction object properties: {string.Join(", ", prediction.GetType().GetProperties().Select(p => p.Name))}");

        // Try to get the output data using reflection
        var predictionType = prediction.GetType();
        var properties = predictionType.GetProperties();
        
        _loggingService.LogVerbose($"Available properties: {string.Join(", ", properties.Select(p => $"{p.Name}:{p.PropertyType.Name}"))}");
        
        float[]? predictedScores = null;
        string? foundPropertyName = null;
        
        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(prediction);
                _loggingService.LogVerbose($"Property {prop.Name}: {value?.GetType().Name ?? "null"} = {value}");
                
                if (value is Array arr)
                {
                    _loggingService.LogVerbose($"Property {prop.Name} is array with length: {arr.Length}");
                    if (arr.Length > 0)
                    {
                        _loggingService.LogVerbose($"First few values: {string.Join(", ", arr.Cast<object>().Take(5))}");
                        
                        // Check if this looks like our prediction scores
                        if (arr is float[] floatArr && floatArr.Length == labels.Length)
                        {
                            predictedScores = floatArr;
                            foundPropertyName = prop.Name;
                            _loggingService.LogVerbose($"Found prediction scores in property: {prop.Name}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogVerbose($"Error reading property {prop.Name}: {ex.Message}");
            }
        }

        if (predictedScores == null)
        {
            // Try to get the output column directly from the transformed data
            _loggingService.LogVerbose("Attempting to get output directly from transformed data...");
            var transformedData = model.Transform(imageData);
            var schema = transformedData.Schema;
            
            _loggingService.LogVerbose($"Transformed data schema columns: {string.Join(", ", schema.Select(c => $"{c.Name}:{c.Type}"))}");
            
            // Try to get the output column
            int outputColumnIndex = -1;
            for (int i = 0; i < schema.Count; i++)
            {
                if (schema[i].Name == outputColumnName)
                {
                    outputColumnIndex = i;
                    break;
                }
            }
            
            if (outputColumnIndex >= 0)
            {
                _loggingService.LogVerbose($"Found output column '{outputColumnName}' at index {outputColumnIndex}");
                
                var outputColumn = schema[outputColumnIndex];
                _loggingService.LogVerbose($"Output column type: {outputColumn.Type}");
                
                // Get the first row
                var cursor = transformedData.GetRowCursor(new[] { outputColumn });
                
                // Try different output types and cast to float
                predictedScores = TryExtractOutputWithTypeCasting(cursor, outputColumn, outputColumnIndex);
            }
            else
            {
                _loggingService.LogVerbose($"Output column '{outputColumnName}' not found in schema");
            }
        }

        if (predictedScores == null || predictedScores.Length == 0)
        {
            throw new ApplicationException($"Failed to extract prediction scores. Output column: {outputColumnName}. Found property: {foundPropertyName ?? "none"}");
        }

        _loggingService.LogVerbose($"Successfully extracted {predictedScores.Length} prediction scores");
        _loggingService.LogVerbose($"Score range: {predictedScores.Min():F3} to {predictedScores.Max():F3}");

        if (predictedScores.Length != labels.Length)
        {
            _loggingService.Log($"Warning: Prediction length ({predictedScores.Length}) doesn't match labels length ({labels.Length})", LogLevel.Warning);
        }

        // Get top predictions with confidence scores
        var predictions = predictedScores
            .Select((score, index) => (Label: index < labels.Length ? labels[index] : $"Unknown_{index}", Score: (double)score))
            .OrderByDescending(x => x.Score)
            .Take(_maxTags)
            .ToList();

        _loggingService.LogVerbose($"Generated {predictions.Count} predictions with scores: {string.Join(", ", predictions.Select(p => $"{p.Label}:{p.Score:F3}"))}");

        return predictions;
    }

    private (string inputColumnName, string outputColumnName) GetOnnxColumnNames(string modelPath)
    {
        try
        {
            using var session = new InferenceSession(modelPath);
            
            // Get input column name
            var inputName = session.InputMetadata.Keys.First();
            _loggingService.Log($"ONNX input column name: {inputName}", LogLevel.Debug);
            
            // Get output column name
            var outputName = session.OutputMetadata.Keys.First();
            _loggingService.Log($"ONNX output column name: {outputName}", LogLevel.Debug);
            
            return (inputName, outputName);
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetOnnxColumnNames");
            throw new ApplicationException($"Failed to get ONNX column names: {ex.Message}", ex);
        }
    }

    private string GetOnnxOutputColumnName(string modelPath)
    {
        try
        {
            using var session = new InferenceSession(modelPath);
            var outputName = session.OutputMetadata.Keys.First();
            _loggingService.Log($"ONNX output column name: {outputName}", LogLevel.Debug);
            return outputName;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetOnnxOutputColumnName");
            throw new ApplicationException($"Failed to get ONNX output column name: {ex.Message}", ex);
        }
    }

    private float[]? TryExtractOutputWithTypeCasting(DataViewRowCursor cursor, DataViewSchema.Column outputColumn, int outputColumnIndex)
    {
        if (!cursor.MoveNext())
        {
            _loggingService.LogVerbose("No data rows available");
            return null;
        }

        // Try different output types and cast to float
        try
        {
            // Try float buffer first (most common)
            try
            {
                var floatGetter = cursor.GetGetter<VBuffer<float>>(outputColumn);
                var floatBuffer = new VBuffer<float>();
                floatGetter(ref floatBuffer);
                
                if (floatBuffer.Length > 0)
                {
                    var scores = floatBuffer.DenseValues().ToArray();
                    _loggingService.LogVerbose($"Extracted {scores.Length} float scores from output buffer");
                    return scores;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogVerbose($"Float buffer extraction failed: {ex.Message}");
            }

            // Try Int64 buffer (for ArgMax outputs)
            try
            {
                var int64Getter = cursor.GetGetter<VBuffer<long>>(outputColumn);
                var int64Buffer = new VBuffer<long>();
                int64Getter(ref int64Buffer);
                
                if (int64Buffer.Length > 0)
                {
                    var int64Scores = int64Buffer.DenseValues().ToArray();
                    _loggingService.LogVerbose($"Extracted {int64Scores.Length} Int64 scores from output buffer");
                    
                    // Convert Int64 to float (one-hot encoding to probabilities)
                    var floatScores = new float[int64Scores.Length];
                    for (int i = 0; i < int64Scores.Length; i++)
                    {
                        // Convert one-hot encoding to probability-like scores
                        floatScores[i] = int64Scores[i] == 1 ? 1.0f : 0.0f;
                    }
                    
                    _loggingService.LogVerbose($"Converted Int64 scores to float scores");
                    return floatScores;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogVerbose($"Int64 buffer extraction failed: {ex.Message}");
            }

            // Try Int32 buffer
            try
            {
                var int32Getter = cursor.GetGetter<VBuffer<int>>(outputColumn);
                var int32Buffer = new VBuffer<int>();
                int32Getter(ref int32Buffer);
                
                if (int32Buffer.Length > 0)
                {
                    var int32Scores = int32Buffer.DenseValues().ToArray();
                    _loggingService.LogVerbose($"Extracted {int32Scores.Length} Int32 scores from output buffer");
                    
                    // Convert Int32 to float
                    var floatScores = int32Scores.Select(x => (float)x).ToArray();
                    _loggingService.LogVerbose($"Converted Int32 scores to float scores");
                    return floatScores;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogVerbose($"Int32 buffer extraction failed: {ex.Message}");
            }

            // Try double buffer
            try
            {
                var doubleGetter = cursor.GetGetter<VBuffer<double>>(outputColumn);
                var doubleBuffer = new VBuffer<double>();
                doubleGetter(ref doubleBuffer);
                
                if (doubleBuffer.Length > 0)
                {
                    var doubleScores = doubleBuffer.DenseValues().ToArray();
                    _loggingService.LogVerbose($"Extracted {doubleScores.Length} double scores from output buffer");
                    
                    // Convert double to float
                    var floatScores = doubleScores.Select(x => (float)x).ToArray();
                    _loggingService.LogVerbose($"Converted double scores to float scores");
                    return floatScores;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogVerbose($"Double buffer extraction failed: {ex.Message}");
            }

            // Try single value extraction (for scalar outputs)
            try
            {
                var singleGetter = cursor.GetGetter<float>(outputColumn);
                float singleValue = 0;
                singleGetter(ref singleValue);
                _loggingService.LogVerbose($"Extracted single float value: {singleValue}");
                return new float[] { singleValue };
            }
            catch (Exception ex)
            {
                _loggingService.LogVerbose($"Single float extraction failed: {ex.Message}");
            }

            // Try single Int64 value
            try
            {
                var singleInt64Getter = cursor.GetGetter<long>(outputColumn);
                long singleInt64Value = 0;
                singleInt64Getter(ref singleInt64Value);
                _loggingService.LogVerbose($"Extracted single Int64 value: {singleInt64Value}");
                return new float[] { (float)singleInt64Value };
            }
            catch (Exception ex)
            {
                _loggingService.LogVerbose($"Single Int64 extraction failed: {ex.Message}");
            }

            _loggingService.LogVerbose("All output extraction methods failed");
            return null;
        }
        catch (Exception ex)
        {
            _loggingService.LogVerbose($"Error in output extraction: {ex.Message}");
            return null;
        }
    }

    private string[] MapGenericLabelsToImageNet(string[] genericLabels)
    {
        try
        {
            // Path to the standard ImageNet labels file
            var imagenetLabelsPath = Path.Combine(
                Path.GetDirectoryName(_modelPath) ?? "",
                "..",
                "imagenet_classes.txt");
            
            // If the standard ImageNet labels file doesn't exist, try the root models directory
            if (!File.Exists(imagenetLabelsPath))
            {
                imagenetLabelsPath = Path.Combine(
                    Path.GetDirectoryName(_modelPath) ?? "",
                    "..",
                    "..",
                    "imagenet_classes.txt");
            }
            
            if (!File.Exists(imagenetLabelsPath))
            {
                _loggingService.LogVerbose($"ImageNet labels file not found at {imagenetLabelsPath}, using generic labels");
                return genericLabels;
            }
            
            // Load ImageNet labels
            var imagenetLabels = File.ReadAllLines(imagenetLabelsPath);
            _loggingService.LogVerbose($"Loaded {imagenetLabels.Length} ImageNet labels from {imagenetLabelsPath}");
            
            // Map generic labels to ImageNet labels
            var mappedLabels = new string[genericLabels.Length];
            for (int i = 0; i < genericLabels.Length && i < imagenetLabels.Length; i++)
            {
                mappedLabels[i] = imagenetLabels[i];
            }
            
            // If we have more generic labels than ImageNet labels, keep the generic ones for the extra indices
            for (int i = imagenetLabels.Length; i < genericLabels.Length; i++)
            {
                mappedLabels[i] = genericLabels[i];
            }
            
            _loggingService.LogVerbose($"Successfully mapped {Math.Min(genericLabels.Length, imagenetLabels.Length)} labels to ImageNet format");
            return mappedLabels;
        }
        catch (Exception ex)
        {
            _loggingService.LogVerbose($"Error mapping labels to ImageNet: {ex.Message}, using original labels");
            return genericLabels;
        }
    }

    private void LogOnnxModelDetails(string modelPath)
    {
        try
        {
            using var session = new InferenceSession(modelPath);
            
            _loggingService.LogVerbose("=== ONNX Model Details ===");
            _loggingService.LogVerbose($"Model path: {modelPath}");
            
            // Log input metadata
            _loggingService.LogVerbose("Input metadata:");
            foreach (var input in session.InputMetadata)
            {
                _loggingService.LogVerbose($"  {input.Key}: {string.Join("x", input.Value.Dimensions)} ({input.Value.ElementType})");
            }
            
            // Log output metadata
            _loggingService.LogVerbose("Output metadata:");
            foreach (var output in session.OutputMetadata)
            {
                _loggingService.LogVerbose($"  {output.Key}: {string.Join("x", output.Value.Dimensions)} ({output.Value.ElementType})");
            }
            
            _loggingService.LogVerbose("=== End ONNX Model Details ===");
        }
        catch (Exception ex)
        {
            _loggingService.LogVerbose($"Error logging ONNX model details: {ex.Message}");
        }
    }

    private class ImageInput
    {
        public string ImagePath { get; set; } = string.Empty;
    }

    private class DynamicImagePrediction
    {
        public float[] PredictedLabels { get; set; } = Array.Empty<float>();
    }
} 