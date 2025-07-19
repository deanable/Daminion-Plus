using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Xmp;
using System.Text.Json;
using System.ComponentModel;
using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using ImageTagger.Core.Configuration;
using ImageTagger.Services;
using ImageTagger.Infrastructure;
using Microsoft.Extensions.Configuration;
using LogLevel = ImageTagger.Core.Interfaces.LogLevel;

namespace ImageTagger;

public partial class MainForm : Form
{
    private string? selectedImagePath;
    private List<string> currentTags = new();
    private bool isProcessing = false;

    // Services
    private readonly ILoggingService _loggingService;
    private readonly IMetadataService _metadataService;
    private readonly List<IImageTaggingService> _taggingServices;
    private readonly AppSettings _settings;

    public MainForm()
    {
        InitializeComponent();
        
        // Load configuration
        _settings = LoadConfiguration();
        
        // Initialize services
        _loggingService = new FileLoggingService();
        _metadataService = new MetadataService(_loggingService, _settings.Metadata.CreateBackups, _settings.Metadata.SupportedFormats);
        
        // Initialize tagging services
        _taggingServices = InitializeTaggingServices();
        
        InitializeUI();
        VerifyOnnxModel();
        _loggingService.Log("Application started successfully");
    }

    private AppSettings LoadConfiguration()
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var settings = new AppSettings();
            configuration.Bind(settings);

            // Set default model paths if not configured
            if (string.IsNullOrEmpty(settings.Model.ModelPath))
            {
                settings.Model.ModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "resnet50-v1-12.onnx");
            }
            if (string.IsNullOrEmpty(settings.Model.LabelsPath))
            {
                settings.Model.LabelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "imagenet_classes.txt");
            }

            return settings;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load configuration: {ex.Message}", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return new AppSettings();
        }
    }

    private List<IImageTaggingService> InitializeTaggingServices()
    {
        var services = new List<IImageTaggingService>();

        // Add ML.NET service
        services.Add(new MLNetTaggingService(
            _loggingService,
            _settings.Model.ModelPath,
            _settings.Model.LabelsPath,
            maxTags: _settings.Model.MaxTags,
            confidenceThreshold: _settings.Model.ConfidenceThreshold));

        // Add Cloud API service
        services.Add(new CloudApiTaggingService(
            _loggingService,
            _settings.CloudApi.Endpoint,
            _settings.CloudApi.ApiKey,
            _settings.CloudApi.TimeoutSeconds));

        return services;
    }

    private void InitializeUI()
    {
        comboBoxTagMethod.Items.Clear();
        foreach (var service in _taggingServices)
        {
            comboBoxTagMethod.Items.Add(service.ServiceName);
        }
        comboBoxTagMethod.SelectedIndex = 0;
        
        // Add progress bar and status label if they don't exist
        if (!Controls.Contains(progressBar))
        {
            progressBar = new ProgressBar
            {
                Location = new Point(12, 300),
                Size = new Size(400, 23),
                Visible = false
            };
            Controls.Add(progressBar);
        }

        if (!Controls.Contains(lblStatus))
        {
            lblStatus = new Label
            {
                Location = new Point(12, 330),
                Size = new Size(400, 20),
                Text = "Ready"
            };
            Controls.Add(lblStatus);
        }
    }



    private void UpdateStatus(string message, bool showProgress = false)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => UpdateStatus(message, showProgress)));
            return;
        }

        lblStatus.Text = message;
        progressBar.Visible = showProgress;
        if (showProgress)
        {
            progressBar.Style = ProgressBarStyle.Marquee;
        }
        _loggingService.Log($"Status: {message}");
    }

    private void VerifyOnnxModel()
    {
        try
        {
            if (!File.Exists(_settings.Model.ModelPath))
            {
                var errorMsg = $"ONNX model not found at: {_settings.Model.ModelPath}";
                _loggingService.Log(errorMsg, LogLevel.Error);
                MessageBox.Show(errorMsg, "Model Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var session = new Microsoft.ML.OnnxRuntime.InferenceSession(_settings.Model.ModelPath);
            var outputs = session.OutputMetadata;
            string outputInfo = string.Join("\n", outputs.Select(o => $"{o.Key}: {o.Value.ElementType} [{string.Join(",", o.Value.Dimensions)}]"));
            _loggingService.Log($"ONNX Model Verification:\n{outputInfo}");

            // Log the available output columns for debugging
            _loggingService.Log($"Available ONNX output columns: {string.Join(", ", outputs.Keys)}");

            if (outputs.Count == 0)
            {
                var errorMsg = $"No output columns found in ONNX model.\nModel outputs:\n{outputInfo}";
                _loggingService.Log(errorMsg, LogLevel.Error);
                MessageBox.Show(errorMsg, "ONNX Model Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // The model uses 'resnetv17_dense0_fwd' instead of 'probabilities'
            if (!outputs.ContainsKey("probabilities") && !outputs.ContainsKey("resnetv17_dense0_fwd"))
            {
                var warningMsg = $"Warning: Expected output column 'probabilities' or 'resnetv17_dense0_fwd' not found.\nModel outputs:\n{outputInfo}";
                _loggingService.Log(warningMsg, LogLevel.Warning);
                MessageBox.Show(warningMsg, "ONNX Model Verification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Failed to verify ONNX model: {ex.Message}";
            _loggingService.Log(errorMsg, LogLevel.Error);
            MessageBox.Show(errorMsg, "Model Verification Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnSelectImage_Click(object sender, EventArgs e)
    {
        try
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.tif",
                Title = "Select an image to tag"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                selectedImagePath = ofd.FileName;

                // Validate file exists and is readable
                if (!File.Exists(selectedImagePath))
                {
                    throw new FileNotFoundException("Selected file does not exist.");
                }

                // Load and display image with error handling
                try
                {
                    using var image = Image.FromFile(selectedImagePath);
                    pictureBoxPreview.Image = new Bitmap(image);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to load image: {ex.Message}");
                }

                listBoxTags.Items.Clear();
                currentTags.Clear();

                var fileInfo = new FileInfo(selectedImagePath);
                _loggingService.Log($"Image selected: {selectedImagePath} ({fileInfo.Length / 1024} KB)");
                UpdateStatus($"Image loaded: {Path.GetFileName(selectedImagePath)}");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "Select Image");
            MessageBox.Show($"Error selecting image:\n{ex.Message}", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("Error selecting image");
        }
    }

    private async void btnTagImage_Click(object sender, EventArgs e)
    {
        if (isProcessing)
        {
            MessageBox.Show("Please wait for the current operation to complete.", "Operation in Progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrEmpty(selectedImagePath))
        {
            MessageBox.Show("Please select an image first.", "No Image Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!File.Exists(selectedImagePath))
        {
            MessageBox.Show("Selected image file no longer exists.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        isProcessing = true;
        btnTagImage.Enabled = false;
        btnSaveTags.Enabled = false;

        try
        {
            listBoxTags.Items.Clear();
            currentTags.Clear();

            var selectedItem = comboBoxTagMethod.SelectedItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Please select a tagging method.", "No Method Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var methodName = selectedItem.ToString();
            var taggingService = _taggingServices.FirstOrDefault(s => s.ServiceName == methodName);
            
            if (taggingService == null)
            {
                throw new InvalidOperationException($"Tagging service '{methodName}' not found.");
            }

            UpdateStatus($"Tagging image using {methodName}...", true);

            var result = await taggingService.TagImageAsync(selectedImagePath);

            if (result.Success && result.Tags.Count > 0)
            {
                foreach (var tag in result.Tags)
                {
                    listBoxTags.Items.Add($"{tag.Tag} ({tag.Confidence:F2})");
                    currentTags.Add(tag.Tag);
                }

                _loggingService.Log($"Successfully generated {result.Tags.Count} tags using {methodName}");
                UpdateStatus($"Generated {result.Tags.Count} tags successfully");
            }
            else
            {
                _loggingService.Log($"No tags generated using {methodName}", LogLevel.Warning);
                UpdateStatus("No tags generated");
                MessageBox.Show("No tags were generated for this image.", "No Tags", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "Tag Image");
            MessageBox.Show($"Error during image tagging:\n{ex.Message}", "Tagging Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("Tagging failed");
        }
        finally
        {
            isProcessing = false;
            btnTagImage.Enabled = true;
            btnSaveTags.Enabled = currentTags.Count > 0;
        }
    }

    private async void btnSaveTags_Click(object sender, EventArgs e)
    {
        if (isProcessing)
        {
            MessageBox.Show("Please wait for the current operation to complete.", "Operation in Progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrEmpty(selectedImagePath) || currentTags.Count == 0)
        {
            MessageBox.Show("No tags to save. Please tag an image first.", "No Tags", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!File.Exists(selectedImagePath))
        {
            MessageBox.Show("Selected image file no longer exists.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        isProcessing = true;
        btnSaveTags.Enabled = false;

        try
        {
            UpdateStatus("Saving tags to image metadata...", true);

            var success = await _metadataService.SaveTagsAsync(selectedImagePath, currentTags);

            if (success)
            {
                _loggingService.Log($"Successfully saved {currentTags.Count} tags to {selectedImagePath}");
                UpdateStatus("Tags saved successfully");
                MessageBox.Show($"Successfully saved {currentTags.Count} tags to image metadata.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                throw new InvalidOperationException("Failed to save tags to image metadata.");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "Save Tags");
            MessageBox.Show($"Error saving tags to image:\n{ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("Failed to save tags");
        }
        finally
        {
            isProcessing = false;
            btnSaveTags.Enabled = currentTags.Count > 0;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _loggingService.Log("Application closing");
        base.OnFormClosing(e);
    }
}
