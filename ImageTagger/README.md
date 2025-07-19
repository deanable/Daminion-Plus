# ImageTagger WinForms App

A Windows Forms application to auto-tag images with descriptive metadata using either a cloud API or ML.NET (local model), and save tags into the image file.

## Features
- Select and preview images
- Choose tagging method: Cloud API or ML.NET (local)
- Auto-tag images and display tags
- Save tags into image metadata (EXIF/IPTC/XMP)
- **Enhanced error handling and logging**
- **Progress indicators and status updates**
- **Comprehensive metadata writing support**
- **Cloud API integration with Azure Computer Vision**
- **Clean Architecture with separated concerns**

## Architecture

The application follows Clean Architecture principles with separated concerns:

```
DaminionPlus/
├── ImageTagger.Core/          # Domain models & interfaces
├── ImageTagger.Services/      # Business logic implementations  
├── ImageTagger.Infrastructure/ # External dependencies
├── ImageTagger/              # WinForms UI
└── Daminion Plus.sln         # Solution file
```

### Project Structure
- **Core**: Domain models, interfaces, and configuration
- **Services**: ML.NET and Cloud API tagging implementations
- **Infrastructure**: Logging, metadata handling, and external dependencies
- **UI**: WinForms presentation layer with dependency injection

## Setup
1. **Requirements:**
   - .NET 9.0 SDK or later
   - Visual Studio 2022 or later (with WinForms support)

2. **Dependencies:**
   - [Microsoft.ML](https://www.nuget.org/packages/Microsoft.ML)
   - [MetadataExtractor](https://www.nuget.org/packages/MetadataExtractor)
   - [System.Drawing.Common](https://www.nuget.org/packages/System.Drawing.Common)
   - [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json)
   - [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration)

   These are already referenced in the projects.

## Build & Run
1. Open the solution in Visual Studio, or use the command line:
   ```sh
   dotnet build
   dotnet run --project ImageTagger
   ```

## Usage
1. Click **Select Image** to choose an image file.
2. Choose **Cloud API** or **ML.NET (Local)** as the tagging method.
3. Click **Tag Image** to generate tags.
4. Click **Save Tags to Image** to write tags into the image metadata.

## Configuration

### Cloud API Setup
To use cloud API tagging, you need to configure your API credentials:

1. **Azure Computer Vision** (recommended):
   - Get an API key from [Azure Portal](https://portal.azure.com)
   - Update the `appsettings.json` file:
   ```json
   {
     "CloudApi": {
       "Endpoint": "https://your-resource.cognitiveservices.azure.com/vision/v3.2/analyze",
       "ApiKey": "your-actual-api-key",
       "TimeoutSeconds": 30
     }
   }
   ```

2. **Alternative Cloud APIs**:
   - Google Cloud Vision API
   - AWS Rekognition
   - Custom vision services

## Error Handling & Logging

The application includes comprehensive error handling and logging:

- **Log File**: `ImageTagger.log` in the application directory
- **Log Levels**: Info, Warning, Error, Debug
- **Status Updates**: Real-time status messages in the UI
- **Progress Indicators**: Visual feedback during operations
- **File Validation**: Checks for file existence and readability
- **Backup System**: Creates backups before metadata modifications

## Metadata Writing

The application supports writing tags to various image formats:

- **JPEG**: EXIF UserComment field
- **PNG**: Text chunks (limited support)
- **TIFF**: EXIF metadata
- **Backup System**: Automatically creates backups before modifications

## ML.NET Local Model

The local tagging uses:
- **Model**: ResNet50 ONNX model (98MB)
- **Labels**: ImageNet 1000 classes
- **Input Size**: 224x224 pixels
- **Output**: Top 3 predictions with confidence scores

## Development & Architecture

### SOLID Principles
- **Single Responsibility**: Each class has one clear purpose
- **Open/Closed**: Easy to extend with new services
- **Liskov Substitution**: Services implement common interfaces
- **Interface Segregation**: Focused, specific interfaces
- **Dependency Inversion**: UI depends on abstractions, not concretions

### Dependency Injection
- Services are injected through interfaces
- Easy to swap implementations
- Testable architecture
- Configuration-driven initialization

### Adding New Services
To add a new tagging service:
1. Implement `IImageTaggingService` interface
2. Add the service to `InitializeTaggingServices()` in MainForm
3. The UI will automatically pick up the new service

## Troubleshooting

### Common Issues

1. **"ONNX model not found"**:
   - Ensure `models/resnet50-v1-12.onnx` exists in the output directory
   - Check file permissions

2. **"Cloud API key not configured"**:
   - Update the `appsettings.json` file with your API credentials
   - Verify your API endpoint URL

3. **"Failed to save tags"**:
   - Check file permissions
   - Ensure the image file isn't read-only
   - Check the log file for detailed error messages

4. **"No tags generated"**:
   - Try a different image
   - Check if the image format is supported
   - Verify model files are present

### Log Analysis

Check `ImageTagger.log` for detailed information:
- Application startup/shutdown
- Model verification results
- API call details
- Error stack traces
- Performance metrics

## Development Notes

- **Threading**: All long-running operations are async
- **Memory Management**: Proper disposal of resources
- **UI Threading**: Safe cross-thread UI updates
- **Exception Handling**: Comprehensive try-catch blocks
- **Resource Cleanup**: Automatic disposal of HttpClient and other resources
- **.NET 9**: Latest framework with performance improvements

## TODO
- [x] Implement cloud API integration
- [x] Implement metadata writing
- [x] Add error handling and logging
- [x] Add progress indicators
- [x] Separate concerns into class libraries
- [x] Upgrade to .NET 9
- [ ] Add batch processing capabilities
- [ ] Add custom model support
- [ ] Add tag editing interface
- [ ] Add metadata viewing interface
- [ ] Add unit tests
- [ ] Add integration tests

---

*This project uses Clean Architecture principles with .NET 9 and follows SOLID design patterns.* 