# ImageTagger WinForms App

A Windows Forms application to auto-tag images with descriptive metadata using either a cloud API or ML.NET (local model), and save tags into the image file.

## Features
- Select and preview images
- Choose tagging method: Cloud API or ML.NET (local)
- Auto-tag images and display tags
- Save tags into image metadata (EXIF/IPTC/XMP)

## Setup
1. **Requirements:**
   - .NET 6.0 SDK or later
   - Visual Studio 2022 or later (with WinForms support)

2. **Dependencies:**
   - [Microsoft.ML](https://www.nuget.org/packages/Microsoft.ML)
   - [MetadataExtractor](https://www.nuget.org/packages/MetadataExtractor)

   These are already referenced in the project.

## Build & Run
1. Open the solution in Visual Studio, or use the command line:
   ```sh
   dotnet build ImageTagger
   dotnet run --project ImageTagger
   ```

## Usage
1. Click **Select Image** to choose an image file.
2. Choose **Cloud API** or **ML.NET (Local)** as the tagging method.
3. Click **Tag Image** to generate tags.
4. Click **Save Tags to Image** to write tags into the image metadata.

## Integration Notes
- **Cloud API**: You must implement the call to your chosen cloud vision API (e.g., Azure, Google). Insert your API key and endpoint in `TagImageWithCloudApi`.
- **ML.NET**: You must provide or train a local image classification model. Integrate it in `TagImageWithMLNet`.
- **Metadata Writing**: The `SaveTagsToImage` method is a placeholder. Use `MetadataExtractor` or another library to write tags to EXIF/IPTC/XMP fields.

## TODO
- Implement cloud API and ML.NET tagging logic
- Implement metadata writing
- Add error handling and polish UI

---

*This project was generated as a WinForms-only solution (no WPF).* 