using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using System.Text.Json;

namespace ImageTagger;

public partial class ModelsForm : Form
{
    private readonly ILoggingService _loggingService;
    private readonly string _modelsDirectory;
    private List<ModelInfo> _availableModels = new();
    private List<ModelInfo> _downloadedModels = new();

    public ModelsForm(ILoggingService loggingService)
    {
        _loggingService = loggingService;
        _modelsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");
        
        InitializeComponent();
        LoadModels();
    }

    private void InitializeComponent()
    {
        this.Text = "Model Manager";
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        // Create controls
        var tabControl = new TabControl();
        tabControl.Dock = DockStyle.Fill;

        // Available Models Tab
        var availableTab = new TabPage("Available Models");
        var availableListView = new ListView();
        availableListView.Dock = DockStyle.Fill;
        availableListView.View = View.Details;
        availableListView.FullRowSelect = true;
        availableListView.GridLines = true;
        availableListView.Columns.Add("Name", 150);
        availableListView.Columns.Add("Description", 300);
        availableListView.Columns.Add("Size", 80);
        availableListView.Columns.Add("Tags", 100);
        availableListView.Columns.Add("Status", 80);

        var downloadButton = new Button();
        downloadButton.Text = "Download Selected Model";
        downloadButton.Dock = DockStyle.Bottom;
        downloadButton.Height = 40;
        downloadButton.Click += DownloadButton_Click;

        var refreshButton = new Button();
        refreshButton.Text = "Refresh Available Models";
        refreshButton.Dock = DockStyle.Bottom;
        refreshButton.Height = 40;
        refreshButton.Click += RefreshButton_Click;

        availableTab.Controls.Add(availableListView);
        availableTab.Controls.Add(downloadButton);
        availableTab.Controls.Add(refreshButton);

        // Downloaded Models Tab
        var downloadedTab = new TabPage("Downloaded Models");
        var downloadedListView = new ListView();
        downloadedListView.Dock = DockStyle.Fill;
        downloadedListView.View = View.Details;
        downloadedListView.FullRowSelect = true;
        downloadedListView.GridLines = true;
        downloadedListView.Columns.Add("Name", 150);
        downloadedListView.Columns.Add("Description", 300);
        downloadedListView.Columns.Add("Size", 80);
        downloadedListView.Columns.Add("Tags", 100);
        downloadedListView.Columns.Add("Status", 80);

        var deleteButton = new Button();
        deleteButton.Text = "Delete Selected Model";
        deleteButton.Dock = DockStyle.Bottom;
        deleteButton.Height = 40;
        deleteButton.Click += DeleteButton_Click;

        downloadedTab.Controls.Add(downloadedListView);
        downloadedTab.Controls.Add(deleteButton);

        tabControl.TabPages.Add(availableTab);
        tabControl.TabPages.Add(downloadedTab);

        this.Controls.Add(tabControl);

        // Store references
        this.Tag = new { AvailableListView = availableListView, DownloadedListView = downloadedListView };
    }

    private async void LoadModels()
    {
        try
        {
            _loggingService.Log("Loading available models from repository");
            
            // Load available models from a JSON file or API
            await LoadAvailableModelsFromRepository();
            
            // Load downloaded models from local directory
            LoadDownloadedModels();
            
            RefreshModelLists();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "Load Models");
            MessageBox.Show($"Error loading models: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadAvailableModelsFromRepository()
    {
        // For now, we'll use a local JSON file with model definitions
        // In a real application, this would be an API call
        var modelsJson = @"[
            {
                ""name"": ""resnet50-v1-12"",
                ""displayName"": ""ResNet-50 (ImageNet)"",
                ""description"": ""Pre-trained ResNet-50 model for general image classification. Recognizes 1000 ImageNet classes including animals, objects, and scenes."",
                ""size"": ""98MB"",
                ""tags"": ""general, classification, imagenet"",
                ""downloadUrl"": ""https://github.com/onnx/models/raw/main/vision/classification/resnet/model/resnet50-v1-12.onnx"",
                ""labelsUrl"": ""https://raw.githubusercontent.com/onnx/models/main/vision/classification/resnet/model/imagenet_classes.txt"",
                ""inputShape"": ""[1, 3, 224, 224]"",
                ""outputShape"": ""[1, 1000]"",
                ""supportedFormats"": [""jpg"", ""jpeg"", ""png"", ""bmp""]
            },
            {
                ""name"": ""mobilenet-v2"",
                ""displayName"": ""MobileNet V2"",
                ""description"": ""Lightweight neural network optimized for mobile and embedded devices. Good balance of accuracy and speed."",
                ""size"": ""14MB"",
                ""tags"": ""mobile, lightweight, classification"",
                ""downloadUrl"": ""https://github.com/onnx/models/raw/main/vision/classification/mobilenet/model/mobilenetv2-1.0.onnx"",
                ""labelsUrl"": ""https://raw.githubusercontent.com/onnx/models/main/vision/classification/mobilenet/model/imagenet_classes.txt"",
                ""inputShape"": ""[1, 3, 224, 224]"",
                ""outputShape"": ""[1, 1000]"",
                ""supportedFormats"": [""jpg"", ""jpeg"", ""png"", ""bmp""]
            },
            {
                ""name"": ""efficientnet-lite4"",
                ""displayName"": ""EfficientNet-Lite4"",
                ""description"": ""Efficient neural network architecture optimized for edge devices. Excellent accuracy with minimal computational requirements."",
                ""size"": ""19MB"",
                ""tags"": ""efficient, edge, classification"",
                ""downloadUrl"": ""https://github.com/onnx/models/raw/main/vision/classification/efficientnet-lite4/model/efficientnet-lite4-11.onnx"",
                ""labelsUrl"": ""https://raw.githubusercontent.com/onnx/models/main/vision/classification/efficientnet-lite4/model/imagenet_classes.txt"",
                ""inputShape"": ""[1, 3, 224, 224]"",
                ""outputShape"": ""[1, 1000]"",
                ""supportedFormats"": [""jpg"", ""jpeg"", ""png"", ""bmp""]
            }
        ]";

        try
        {
            _availableModels = JsonSerializer.Deserialize<List<ModelInfo>>(modelsJson) ?? new List<ModelInfo>();
            _loggingService.Log($"Loaded {_availableModels.Count} available models from repository");
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "Parse Available Models JSON");
            _availableModels = new List<ModelInfo>();
        }
    }

    private void LoadDownloadedModels()
    {
        _downloadedModels.Clear();
        
        if (!Directory.Exists(_modelsDirectory))
        {
            Directory.CreateDirectory(_modelsDirectory);
            return;
        }

        var modelFiles = Directory.GetFiles(_modelsDirectory, "*.onnx");
        foreach (var modelFile in modelFiles)
        {
            var modelName = Path.GetFileNameWithoutExtension(modelFile);
            var labelsFile = Path.Combine(_modelsDirectory, $"{modelName}_classes.txt");
            
            var modelInfo = new ModelInfo
            {
                Name = modelName,
                DisplayName = modelName,
                Description = $"Local model: {modelName}",
                Size = $"{new FileInfo(modelFile).Length / 1024 / 1024}MB",
                Tags = "local, downloaded",
                Status = File.Exists(labelsFile) ? "Ready" : "Missing Labels"
            };
            
            _downloadedModels.Add(modelInfo);
        }
        
        _loggingService.Log($"Found {_downloadedModels.Count} downloaded models");
    }

    private void RefreshModelLists()
    {
        var controls = (dynamic)this.Tag;
        var availableListView = controls.AvailableListView;
        var downloadedListView = controls.DownloadedListView;

        // Clear existing items
        availableListView.Items.Clear();
        downloadedListView.Items.Clear();

        // Populate available models
        foreach (var model in _availableModels)
        {
            var item = new ListViewItem(model.DisplayName);
            item.SubItems.Add(model.Description);
            item.SubItems.Add(model.Size);
            item.SubItems.Add(model.Tags);
            item.SubItems.Add(IsModelDownloaded(model.Name) ? "Downloaded" : "Available");
            item.Tag = model;
            availableListView.Items.Add(item);
        }

        // Populate downloaded models
        foreach (var model in _downloadedModels)
        {
            var item = new ListViewItem(model.DisplayName);
            item.SubItems.Add(model.Description);
            item.SubItems.Add(model.Size);
            item.SubItems.Add(model.Tags);
            item.SubItems.Add(model.Status);
            item.Tag = model;
            downloadedListView.Items.Add(item);
        }
    }

    private bool IsModelDownloaded(string modelName)
    {
        var modelFile = Path.Combine(_modelsDirectory, $"{modelName}.onnx");
        return File.Exists(modelFile);
    }

    private async void DownloadButton_Click(object sender, EventArgs e)
    {
        var controls = (dynamic)this.Tag;
        var availableListView = controls.AvailableListView;

        if (availableListView.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a model to download.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedModel = (ModelInfo)availableListView.SelectedItems[0].Tag;
        
        try
        {
            await DownloadModel(selectedModel);
            LoadDownloadedModels();
            RefreshModelLists();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "Download Model");
            MessageBox.Show($"Error downloading model: {ex.Message}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DownloadModel(ModelInfo model)
    {
        _loggingService.Log($"Starting download of model: {model.Name}");

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(10);

        // Download model file
        var modelPath = Path.Combine(_modelsDirectory, $"{model.Name}.onnx");
        var modelResponse = await client.GetAsync(model.DownloadUrl);
        modelResponse.EnsureSuccessStatusCode();
        
        using (var modelStream = await modelResponse.Content.ReadAsStreamAsync())
        using (var fileStream = File.Create(modelPath))
        {
            await modelStream.CopyToAsync(fileStream);
        }

        // Download labels file
        var labelsPath = Path.Combine(_modelsDirectory, $"{model.Name}_classes.txt");
        var labelsResponse = await client.GetAsync(model.LabelsUrl);
        labelsResponse.EnsureSuccessStatusCode();
        
        using (var labelsStream = await labelsResponse.Content.ReadAsStreamAsync())
        using (var fileStream = File.Create(labelsPath))
        {
            await labelsStream.CopyToAsync(fileStream);
        }

        _loggingService.Log($"Successfully downloaded model: {model.Name}");
        MessageBox.Show($"Successfully downloaded {model.DisplayName}!", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RefreshButton_Click(object sender, EventArgs e)
    {
        LoadModels();
    }

    private void DeleteButton_Click(object sender, EventArgs e)
    {
        var controls = (dynamic)this.Tag;
        var downloadedListView = controls.DownloadedListView;

        if (downloadedListView.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a model to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedModel = (ModelInfo)downloadedListView.SelectedItems[0].Tag;
        
        if (MessageBox.Show($"Are you sure you want to delete {selectedModel.DisplayName}?", "Confirm Delete", 
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            try
            {
                DeleteModel(selectedModel);
                LoadDownloadedModels();
                RefreshModelLists();
            }
            catch (Exception ex)
            {
                _loggingService.LogException(ex, "Delete Model");
                MessageBox.Show($"Error deleting model: {ex.Message}", "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void DeleteModel(ModelInfo model)
    {
        var modelFile = Path.Combine(_modelsDirectory, $"{model.Name}.onnx");
        var labelsFile = Path.Combine(_modelsDirectory, $"{model.Name}_classes.txt");

        if (File.Exists(modelFile))
        {
            File.Delete(modelFile);
        }

        if (File.Exists(labelsFile))
        {
            File.Delete(labelsFile);
        }

        _loggingService.Log($"Deleted model: {model.Name}");
        MessageBox.Show($"Successfully deleted {model.DisplayName}!", "Delete Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public class ModelInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string LabelsUrl { get; set; } = string.Empty;
        public string InputShape { get; set; } = string.Empty;
        public string OutputShape { get; set; } = string.Empty;
        public string[] SupportedFormats { get; set; } = Array.Empty<string>();
        public string Status { get; set; } = string.Empty;
    }
} 