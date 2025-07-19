# ImageTagger Plus - Model Management System

## Overview

The ImageTagger Plus application now includes a comprehensive model management system that allows you to:

- Download pre-trained ONNX models from the [ONNX Models repository](https://github.com/onnx/models/tree/main/Computer_Vision)
- Manage multiple models simultaneously
- Switch between different models for image tagging
- Validate model integrity
- Enable/disable models as needed

## Supported Models

The system supports the following popular computer vision models from the ONNX Models repository:

### Image Classification Models

1. **ResNet-50 v1.12** (Priority: 100)
   - High accuracy image classification
   - 1000 ImageNet classes
   - Good balance of accuracy and speed

2. **EfficientNet-Lite4** (Priority: 95)
   - Lightweight model optimized for mobile/edge devices
   - Fast inference with good accuracy
   - Ideal for resource-constrained environments

3. **MobileNet v2** (Priority: 90)
   - Mobile-optimized model
   - Good accuracy/speed trade-off
   - Small model size

4. **Inception v1** (Priority: 85)
   - Classic Inception architecture
   - Good accuracy for general image classification

5. **SqueezeNet 1.1** (Priority: 80)
   - Very lightweight model
   - High accuracy with small size
   - Fast inference

6. **DenseNet-121** (Priority: 75)
   - Dense connections for better feature reuse
   - High accuracy with efficient parameter usage

## Model Management Features

### 1. Model Registry

The system maintains a JSON-based model registry (`models/model_registry.json`) that tracks:

- Installed models and their configurations
- Model metadata (name, description, license, etc.)
- Model settings (image dimensions, confidence thresholds, etc.)
- Default model selection
- Model enable/disable status

### 2. Model Downloader

The `ModelDownloaderService` can automatically download models from the ONNX Models repository:

```csharp
var downloader = new ModelDownloaderService(loggingService);
var success = await downloader.DownloadModelFromRepositoryAsync("resnet50-v1-12");
```

### 3. Model Manager

The `ModelManager` provides high-level model management operations:

```csharp
var modelManager = new ModelManager(loggingService);

// Load model registry
var registry = await modelManager.LoadModelRegistryAsync("models/model_registry.json");

// Get default model
var defaultModel = await modelManager.GetDefaultModelAsync();

// Switch default model
await modelManager.SetDefaultModelAsync("efficientnet-lite4-11");

// Validate model
var isValid = await modelManager.ValidateModelAsync(modelInfo);

// Enable/disable model
await modelManager.EnableModelAsync("resnet50-v1-12", false);
```

### 4. Enhanced ML.NET Tagging Service

The `EnhancedMLNetTaggingService` provides advanced tagging capabilities:

```csharp
var enhancedService = new EnhancedMLNetTaggingService(loggingService, modelManager);

// Tag with default model
var result = await enhancedService.TagImageAsync("path/to/image.jpg");

// Tag with specific model
var result = await enhancedService.TagImageWithModelAsync("path/to/image.jpg", "efficientnet-lite4-11");

// Tag with multiple models
var result = await enhancedService.TagImageWithMultipleModelsAsync("path/to/image.jpg", 
    new List<string> { "resnet50-v1-12", "efficientnet-lite4-11" });
```

## Using the Model Management UI

### Opening the Model Management Form

1. Launch ImageTagger Plus
2. Go to **Tools** → **Model Management** (or use the shortcut)
3. The Model Management form will open

### Managing Installed Models

The **Installed Models** section shows all models currently registered in your system:

- **Display Name**: Human-readable model name
- **Name**: Internal model identifier
- **Status**: Enabled/Disabled
- **Priority**: Model priority (higher = more important)
- **Model File**: ✓ if ONNX file exists, ✗ if missing
- **Labels File**: ✓ if labels file exists, ✗ if missing

#### Actions Available:

1. **Enable/Disable**: Toggle model availability
2. **Validate Model**: Check if model files are valid and loadable
3. **Set as Default**: Choose the default model for tagging

### Downloading New Models

The **Available Models** section shows models available for download from the ONNX Models repository:

1. Select a model from the list
2. Click **Download Selected Model**
3. The system will:
   - Download the ONNX model file
   - Download the corresponding labels file
   - Validate the downloaded files
   - Add the model to your registry
   - Enable the model for use

### Setting the Default Model

1. Use the **Default Model** dropdown to select your preferred model
2. The selected model will be used for automatic tagging operations

## Model Configuration

Each model can be configured with the following parameters:

### Basic Settings

- **Image Width/Height**: Input image dimensions (typically 224x224)
- **Confidence Threshold**: Minimum confidence score for tags (default: 0.1)
- **Max Tags**: Maximum number of tags to generate (default: 5)

### Advanced Settings

- **Priority**: Model priority for multi-model operations
- **Source**: Model source (e.g., "ONNX Models Repository")
- **License**: Model license information
- **Description**: Detailed model description

## File Structure

```
ImageTagger/
├── models/
│   ├── model_registry.json          # Model registry
│   ├── resnet50-v1-12/
│   │   ├── resnet50-v1-12.onnx      # ONNX model file
│   │   └── resnet50-v1-12_labels.txt # Labels file
│   ├── efficientnet-lite4-11/
│   │   ├── efficientnet-lite4-11.onnx
│   │   └── efficientnet-lite4-11_labels.txt
│   └── ...
```

## Performance Considerations

### Model Selection Guidelines

- **High Accuracy**: Use ResNet-50 or DenseNet-121
- **Fast Inference**: Use EfficientNet-Lite4 or MobileNet v2
- **Small Size**: Use SqueezeNet 1.1
- **Balanced**: Use Inception v1

### Memory Usage

- Models are loaded on-demand and cached in memory
- Multiple models can be loaded simultaneously
- Consider available RAM when using multiple models

### Inference Speed

- Smaller models (SqueezeNet, MobileNet) are faster
- Larger models (ResNet, DenseNet) are more accurate but slower
- Use multi-model tagging for best results (combines predictions from multiple models)

## Troubleshooting

### Common Issues

1. **Model Download Fails**
   - Check internet connection
   - Verify model name is correct
   - Check available disk space

2. **Model Validation Fails**
   - Ensure ONNX file is not corrupted
   - Verify labels file format
   - Check file permissions

3. **Inference Errors**
   - Verify image format is supported
   - Check model input dimensions
   - Ensure model is enabled in registry

### Logging

The system provides comprehensive logging for debugging:

- Model download progress
- Validation results
- Inference performance metrics
- Error details

Check the application logs for detailed information about any issues.

## Future Enhancements

Planned features for future releases:

1. **Automatic Model Updates**: Check for newer model versions
2. **Custom Model Support**: Add your own trained models
3. **Model Performance Metrics**: Track accuracy and speed
4. **Batch Processing**: Process multiple images with different models
5. **Model Ensembling**: Advanced multi-model prediction fusion

## Contributing

To add support for new models:

1. Update the `ModelDownloaderService.GetModelUrls()` method
2. Add model metadata to `GetAvailableModelsFromRepositoryAsync()`
3. Test model validation and inference
4. Update this documentation

## License

The model management system is part of ImageTagger Plus and follows the same license as the main application. Individual models may have their own licenses - check the model metadata for details. 