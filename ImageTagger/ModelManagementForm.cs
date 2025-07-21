using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using ImageTagger.Infrastructure;
using ImageTagger.Services;

namespace ImageTagger;

public partial class ModelManagementForm : Form
{
    private readonly IModelManager _modelManager;
    private readonly ModelDownloaderService _modelDownloader;
    private readonly HuggingFaceModelService _hfService;
    private readonly MicrosoftModelZooService _msModelZooService;
    private readonly ModelConversionService _conversionService;
    private readonly ILoggingService _loggingService;
    private ModelRegistry? _currentRegistry;
    private List<ModelInfo> _allRepositoryModels = new();
    private List<ModelInfo> _filteredRepositoryModels = new();
    private bool _isScanning = false;

    public ModelManagementForm(ILoggingService loggingService)
    {
        InitializeComponent();
        _loggingService = loggingService;
        _modelManager = new ModelManager(loggingService);
        _modelDownloader = new ModelDownloaderService(loggingService);
        _hfService = new HuggingFaceModelService(loggingService);
        _msModelZooService = new MicrosoftModelZooService(loggingService);
        _conversionService = new ModelConversionService(loggingService);
        // Add event handlers for new buttons
        buttonDeleteAllModels!.Click += buttonDeleteAllModels_Click;
        buttonGenerateLabels!.Click += buttonGenerateLabels_Click;
        // Move async loading to Form.Load event
        this.Load += ModelManagementForm_Load;
    }

    private async void ModelManagementForm_Load(object? sender, EventArgs e)
    {
        await LoadModelsAsync();
    }

    private async Task LoadModelsAsync()
    {
        try
        {
            Cursor = Cursors.WaitCursor;

            // Use await for async methods
            _currentRegistry = await _modelManager.LoadModelRegistryAsync("models/model_registry.json");

            // Remove models from registry that are missing on disk
            if (_currentRegistry != null)
            {
                var modelsToRemove = _currentRegistry.Models.Where(m => !File.Exists(m.ModelPath) || !File.Exists(m.LabelsPath)).ToList();
                foreach (var m in modelsToRemove)
                {
                    _currentRegistry.Models.Remove(m);
                }
                // Save registry if any were removed
                if (modelsToRemove.Count > 0)
                    await _modelManager.SaveModelRegistryAsync(_currentRegistry, "models/model_registry.json");
            }

            // Build available models list from disk
            var availableModels = new List<ModelInfo>();
            var modelsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");
            if (Directory.Exists(modelsDir))
            {
                foreach (var dir in Directory.GetDirectories(modelsDir))
                {
                    var modelFiles = Directory.GetFiles(dir, "*.onnx").Concat(Directory.GetFiles(dir, "*.pt")).ToList();
                    var labelsFiles = Directory.GetFiles(dir, "labels.txt").ToList();
                    if (modelFiles.Count > 0 && labelsFiles.Count > 0)
                    {
                        var modelInfo = new ModelInfo
                        {
                            Name = Path.GetFileName(dir),
                            DisplayName = Path.GetFileName(dir),
                            ModelPath = modelFiles[0],
                            LabelsPath = labelsFiles[0],
                            // Set other properties as needed
                        };
                        availableModels.Add(modelInfo);
                    }
                }
            }

            // Update UI
            UpdateModelList();
            UpdateAvailableModelsList(availableModels);

            _loggingService.Log("Model management form loaded successfully");
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "LoadModelsAsync");
            MessageBox.Show($"Failed to load models: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private bool IsModelUsable(ModelInfo model)
    {
        // Model is usable if it has a model file and a valid, non-generic labels file
        if (!File.Exists(model.ModelPath) || !File.Exists(model.LabelsPath))
            return false;
        var labels = File.ReadAllLines(model.LabelsPath);
        if (labels.Length == 0)
            return false;
        // Check for generic fallback labels (class_0000, etc.)
        if (labels.Length == 1000 && labels[0].StartsWith("class_"))
            return false;
        return true;
    }

    private void UpdateModelList()
    {
        if (_currentRegistry == null) return;

        listViewInstalledModels?.Items.Clear();
        
        foreach (var model in _currentRegistry.Models.OrderByDescending(m => m.Priority))
        {
            var item = new ListViewItem(model.DisplayName);
            item.SubItems.Add(model.Name);
            item.SubItems.Add(model.IsEnabled ? "Enabled" : "Disabled");
            item.SubItems.Add(model.Priority.ToString());
            item.SubItems.Add(File.Exists(model.ModelPath) ? "\u2713" : "\u2717");
            item.SubItems.Add(File.Exists(model.LabelsPath) ? "\u2713" : "\u2717");
            // Add model type and conversion status columns
            item.SubItems.Add(model.ModelType.ToString());
            item.SubItems.Add(model.ConversionStatus.ToString());
            item.Tag = model;
            // Gray out if not usable
            if (!IsModelUsable(model))
            {
                item.ForeColor = Color.Gray;
                item.BackColor = Color.LightGray;
                item.ToolTipText = "Model is not usable: missing or invalid labels file.";
            }
            listViewInstalledModels?.Items.Add(item);
        }

        // Update default model selection
        if (!string.IsNullOrEmpty(_currentRegistry.DefaultModelName))
        {
            comboBoxDefaultModel?.Text = _currentRegistry.DefaultModelName;
        }
    }

    private void UpdateAvailableModelsList(List<ModelInfo> availableModels)
    {
        listViewAvailableModels?.Items.Clear();
        
        foreach (var model in availableModels)
        {
            var item = new ListViewItem(model.DisplayName);
            item.SubItems.Add(model.Name);
            item.SubItems.Add(model.Description);
            item.SubItems.Add(model.License);
            item.Tag = model;
            
            listViewAvailableModels?.Items.Add(item);
        }
    }

    private void UpdateRepositoryModelsList()
    {
        listViewRepositoryModels?.Items.Clear();
        
        foreach (var model in _filteredRepositoryModels)
        {
            var item = new ListViewItem(model.DisplayName);
            item.SubItems.Add(model.AdditionalProperties.GetValueOrDefault("downloads", 0).ToString());
            item.SubItems.Add(model.AdditionalProperties.GetValueOrDefault("likes", 0).ToString());
            item.SubItems.Add(model.License ?? "Unknown");
            item.SubItems.Add(model.AdditionalProperties.GetValueOrDefault("model_type", "unknown").ToString());
            item.SubItems.Add(model.Priority.ToString());
            item.SubItems.Add(model.Description ?? "");
            item.Tag = model;
            // Gray out if not usable
            if (!IsModelUsable(model))
            {
                item.ForeColor = Color.Gray;
                item.BackColor = Color.LightGray;
                item.ToolTipText = "Model is not usable: missing or invalid labels file.";
            }
            listViewRepositoryModels?.Items.Add(item);
        }

        labelRepositoryStatus?.Text = $"Models: {_filteredRepositoryModels.Count} found";
    }

    private async void buttonScanRepository_Click(object? sender, EventArgs e)
    {
        if (_isScanning) return;

        _isScanning = true;
        buttonScanRepository?.Enabled = false;
        progressBarRepository?.Visible = true;
        labelRepositoryStatus?.Text = "Scanning Hugging Face repository...";

        try
        {
            var filterOptions = GetFilterOptions();
            _allRepositoryModels = await _hfService.LoadEntireRepositoryAsync(filterOptions);
            
            labelRepositoryStatus?.Text = $"Found {_allRepositoryModels.Count} models";
            ApplyRepositoryFilters();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "Scan repository");
            MessageBox.Show($"Failed to scan repository: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelRepositoryStatus?.Text = "Scan failed";
        }
        finally
        {
            progressBarRepository?.Visible = false;
            buttonScanRepository?.Enabled = true;
            _isScanning = false;
        }
    }

    private ModelFilterOptions GetFilterOptions()
    {
        int minDownloads = trackBarMinDownloads != null ? trackBarMinDownloads.Value : 100;
        int maxSize = trackBarMaxSize != null ? trackBarMaxSize.Value : 500;
        int minLikes = trackBarMinLikes != null ? trackBarMinLikes.Value : 0;
        int maxModels = trackBarMaxModels != null ? trackBarMaxModels.Value : 100;
        bool excludeArchived = checkBoxExcludeArchived != null ? checkBoxExcludeArchived.Checked : true;
        bool excludePrivate = checkBoxExcludePrivate != null ? checkBoxExcludePrivate.Checked : true;
        bool onlyVerified = checkBoxOnlyVerified != null ? checkBoxOnlyVerified.Checked : false;
        bool preferImageNet = checkBoxPreferImageNet != null ? checkBoxPreferImageNet.Checked : true;
        string[] searchTerms = (textBoxSearchTerms != null && textBoxSearchTerms.Text != null) ? textBoxSearchTerms.Text.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray() : new string[0];
        var licenseText = comboBoxLicenses != null && comboBoxLicenses.SelectedItem != null ? comboBoxLicenses.SelectedItem.ToString() : "Apache 2.0, MIT, BSD";
        var licenses = licenseText switch
        {
            "All Licenses" => new string[0],
            "Apache 2.0" => new[] { "apache-2.0" },
            "MIT" => new[] { "mit" },
            "BSD" => new[] { "bsd" },
            "GPL" => new[] { "gpl" },
            "Apache 2.0, MIT, BSD" => new[] { "apache-2.0", "mit", "bsd" },
            _ => new string[0]
        };
        return new ModelFilterOptions
        {
            MinDownloads = minDownloads,
            MaxModelSizeMB = maxSize,
            MinLikes = minLikes,
            MaxModels = maxModels,
            ExcludeArchived = excludeArchived,
            ExcludePrivate = excludePrivate,
            OnlyVerified = onlyVerified,
            PreferImageNetLabels = preferImageNet,
            Licenses = licenses,
            SearchTerms = searchTerms,
            SupportedFormats = new[] { "onnx" },
            TaskCategories = new[] { "image-classification", "computer-vision" },
            SortBy = "downloads",
            SortDirection = "desc"
        };
    }

    private void ApplyRepositoryFilters()
    {
        // Apply additional client-side filters
        _filteredRepositoryModels = _allRepositoryModels.Where(model =>
        {
            var downloads = model.AdditionalProperties.GetValueOrDefault("downloads", 0);
            var likes = model.AdditionalProperties.GetValueOrDefault("likes", 0);
            var license = model.License?.ToLowerInvariant() ?? "";

            // Check downloads
            if (downloads is int d && d < (trackBarMinDownloads != null ? trackBarMinDownloads.Value : 100)) return false;

            // Check likes
            if (likes is int l && l < (trackBarMinLikes != null ? trackBarMinLikes.Value : 0)) return false;

            // Check license
            var licenseText = comboBoxLicenses?.SelectedItem?.ToString() ?? "Apache 2.0, MIT, BSD";
            if (licenseText != "All Licenses" && !string.IsNullOrEmpty(license))
            {
                var allowedLicenses = licenseText switch
                {
                    "Apache 2.0" => new[] { "apache-2.0" },
                    "MIT" => new[] { "mit" },
                    "BSD" => new[] { "bsd" },
                    "GPL" => new[] { "gpl" },
                    "Apache 2.0, MIT, BSD" => new[] { "apache-2.0", "mit", "bsd" },
                    _ => new string[0]
                };
                
                if (!allowedLicenses.Any(l => license.Contains(l))) return false;
            }

            return true;
        }).ToList();

        UpdateRepositoryModelsList();
    }

        private void ClearRepositoryFilters()
    {
        if (trackBarMinDownloads != null) trackBarMinDownloads.Value = 100;
        if (trackBarMaxSize != null) trackBarMaxSize.Value = 500;
        if (trackBarMinLikes != null) trackBarMinLikes.Value = 0;
        if (trackBarMaxModels != null) trackBarMaxModels.Value = 100;
        if (comboBoxLicenses != null) comboBoxLicenses.SelectedIndex = 5; // "Apache 2.0, MIT, BSD"
        if (textBoxSearchTerms != null) textBoxSearchTerms.Text = "vision,image,classification";
        if (checkBoxExcludeArchived != null) checkBoxExcludeArchived.Checked = true;
        if (checkBoxExcludePrivate != null) checkBoxExcludePrivate.Checked = true;
        if (checkBoxOnlyVerified != null) checkBoxOnlyVerified.Checked = false;
        if (checkBoxPreferImageNet != null) checkBoxPreferImageNet.Checked = true;
        
        ApplyRepositoryFilters();
    }

    private async void buttonDownloadRepositoryModel_Click(object? sender, EventArgs e)
    {
        if (listViewRepositoryModels?.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select at least one model to download.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedModels = listViewRepositoryModels?.SelectedItems.Cast<ListViewItem>()
            .Select(item => item.Tag as ModelInfo)
            .Where(model => model != null)
            .ToList();

        var result = MessageBox.Show(
            $"Download {(selectedModels != null ? selectedModels.Count : 0)} selected models? This may take some time.",
            "Confirm Download",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        progressBarRepository?.Visible = true;
        progressBarRepository?.Style = ProgressBarStyle.Continuous;
        progressBarRepository?.Maximum = (selectedModels != null ? selectedModels.Count : 0);

        try
        {
            var successCount = 0;
            for (int i = 0; i < (selectedModels != null ? selectedModels.Count : 0); i++)
            {
                var model = selectedModels?[i];
                labelRepositoryStatus?.Text = $"Downloading {model?.DisplayName}...";
                progressBarRepository?.Value = i;

                try
                {
                    var huggingfaceId = model?.AdditionalProperties.GetValueOrDefault("huggingface_id", "").ToString();
                    if (!string.IsNullOrEmpty(huggingfaceId))
                    {
                        var success = await _hfService.DownloadModelAsync(huggingfaceId);
                        if (success)
                        {
                            successCount++;
                            _loggingService.Log($"Successfully downloaded model: {model?.DisplayName}");
                            // Add to registry
                            var modelPath = Path.Combine("models", huggingfaceId.Replace("/", "-"));
                            var modelInfo = await _modelDownloader.CreateModelInfoFromDownloadedAsync(model?.Name ?? "", modelPath);
                            if (_currentRegistry != null)
                                _currentRegistry.Models.Add(modelInfo);
                            if (_currentRegistry != null)
                                await _modelManager.SaveModelRegistryAsync(_currentRegistry, "models/model_registry.json");
                            // Prompt for ONNX conversion if needed
                            var isOnnx = (model != null && model.Name != null && model.Name.ToLowerInvariant().EndsWith(".onnx")) || (model != null && model.AdditionalProperties.GetValueOrDefault("framework", "").ToString().ToLowerInvariant() == "onnx");
                            if (!isOnnx)
                            {
                                var convertResult = MessageBox.Show($"Model '{model?.DisplayName}' is not in ONNX format. Would you like to convert it now?", "Convert to ONNX", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                if (convertResult == DialogResult.Yes)
                                {
                                    // Select the model in the list and trigger conversion
                                    var items = listViewRepositoryModels != null ? listViewRepositoryModels.Items : null;
                                    if (items != null)
                                    {
                                        foreach (ListViewItem item in items)
                                        {
                                            if (item.Tag is ModelInfo mi && mi.Name == model?.Name)
                                            {
                                                item.Selected = true;
                                                break;
                                            }
                                        }
                                    }
                                    buttonConvertToOnnx?.PerformClick();
                                }
                            }
                        }
                        else
                        {
                            _loggingService.Log($"Failed to download model: {model?.DisplayName}", LogLevel.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogException(ex, $"Download model {model?.DisplayName}");
                }
                await Task.Delay(500);
            }
            labelRepositoryStatus?.Text = $"Download complete: {successCount}/{(selectedModels != null ? selectedModels.Count : 0)} models downloaded successfully";
            MessageBox.Show($"Download complete!\n{successCount} out of {(selectedModels != null ? selectedModels.Count : 0)} models downloaded successfully.", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadModelsAsync();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "Download selected models");
            MessageBox.Show($"Download failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelRepositoryStatus?.Text = "Download failed";
        }
        finally
        {
            progressBarRepository?.Visible = false;
        }
    }

    private async void buttonDownloadModel_Click(object? sender, EventArgs e)
    {
        if (listViewAvailableModels?.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a model to download.", "No Model Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedModel = (ModelInfo)(listViewAvailableModels != null && listViewAvailableModels.SelectedItems.Count > 0 ? listViewAvailableModels.SelectedItems[0].Tag! : new ModelInfo());
        
        try
        {
            Cursor = Cursors.WaitCursor;
            buttonDownloadModel?.Enabled = false;
            
            _loggingService.Log($"Starting download of model: {selectedModel.Name}");
            
            var success = await _modelDownloader.DownloadModelFromRepositoryAsync(selectedModel.Name);
            
            if (success)
            {
                // Validate the downloaded model
                // For Hugging Face models, get the actual download path from the Hugging Face ID
                string modelPath;
                if (selectedModel.AdditionalProperties?.ContainsKey("huggingface_id") == true)
                {
                    var huggingfaceId = selectedModel.AdditionalProperties["huggingface_id"].ToString();
                    modelPath = Path.Combine("models", huggingfaceId!.Replace("/", "-"));
                }
                else
                {
                    modelPath = Path.Combine("models", selectedModel.Name.Replace("/", "-"));
                }
                var isValid = await _modelDownloader.ValidateDownloadedModelAsync(selectedModel.Name, modelPath);
                
                if (isValid)
                {
                    // Add to registry
                    var modelInfo = await _modelDownloader.CreateModelInfoFromDownloadedAsync(selectedModel.Name, modelPath);
                    _currentRegistry?.Models.Add(modelInfo);
                    if (_currentRegistry != null)
                        await _modelManager.SaveModelRegistryAsync(_currentRegistry, "models/model_registry.json");
                    
                    UpdateModelList();
                    MessageBox.Show($"Model {selectedModel.DisplayName} downloaded and added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Model {selectedModel.DisplayName} downloaded but validation failed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show($"Failed to download model {selectedModel.DisplayName}.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Download model {selectedModel.Name}");
            MessageBox.Show($"Error downloading model: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
            buttonDownloadModel?.Enabled = true;
        }
    }

    private async void buttonEnableDisable_Click(object? sender, EventArgs e)
    {
        if (listViewInstalledModels != null && listViewInstalledModels.SelectedItems.Count > 0)
        {
            var selectedModel = (ModelInfo)listViewInstalledModels.SelectedItems[0].Tag!;
            var newState = !selectedModel.IsEnabled;
            try
            {
                var success = await _modelManager.EnableModelAsync(selectedModel.Name, newState);
                if (success)
                {
                    selectedModel.IsEnabled = newState;
                    UpdateModelList();
                    _loggingService.Log($"Model {selectedModel.Name} {(newState ? "enabled" : "disabled")}");
                }
                else
                {
                    MessageBox.Show($"Failed to {(newState ? "enable" : "disable")} model {selectedModel.DisplayName}.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException(ex, $"Enable/disable model {selectedModel.Name}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else
        {
            MessageBox.Show("Please select a model to enable/disable.", "No Model Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
    }

    private async void comboBoxDefaultModel_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(comboBoxDefaultModel?.Text) || _currentRegistry == null) return;
        
        try
        {
            await _modelManager.SetDefaultModelAsync(comboBoxDefaultModel.Text);
            _currentRegistry.DefaultModelName = comboBoxDefaultModel.Text;
            _loggingService.Log($"Default model set to: {comboBoxDefaultModel.Text}");
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "SetDefaultModel");
            MessageBox.Show($"Failed to set default model: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void buttonValidateModel_Click(object? sender, EventArgs e)
    {
        if (listViewInstalledModels != null && listViewInstalledModels.SelectedItems.Count > 0)
        {
            var selectedModel = (ModelInfo)listViewInstalledModels.SelectedItems[0].Tag!;
            try
            {
                Cursor = Cursors.WaitCursor;
                buttonValidateModel?.Enabled = false;
                var isValid = await _modelManager.ValidateModelAsync(selectedModel);
                if (isValid)
                {
                    MessageBox.Show($"Model {selectedModel.DisplayName} is valid and ready to use.", "Validation Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Model {selectedModel.DisplayName} validation failed. Check the log for details.", "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException(ex, $"Validate model {selectedModel.Name}");
                MessageBox.Show($"Error validating model: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                buttonValidateModel?.Enabled = true;
            }
        }
        else
        {
            MessageBox.Show("Please select a model to validate.", "No Model Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
    }

    private void buttonRefresh_Click(object? sender, EventArgs e)
    {
        LoadModelsAsync();
    }

    private void buttonClose_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void listViewInstalledModels_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var hasSelection = listViewInstalledModels != null && listViewInstalledModels.SelectedItems.Count > 0;
        buttonEnableDisable?.Enabled = hasSelection;
        buttonValidateModel?.Enabled = hasSelection;
        
        if (hasSelection)
        {
            var selectedModel = (ModelInfo)listViewInstalledModels!.SelectedItems[0].Tag!;
            // Disable actions if model is not usable
            if (!IsModelUsable(selectedModel))
            {
                buttonEnableDisable!.Enabled = false;
                buttonValidateModel!.Enabled = false;
                return;
            }
            buttonEnableDisable!.Text = selectedModel.IsEnabled ? "Disable" : "Enable";
        }
    }

    private void listViewAvailableModels_SelectedIndexChanged(object? sender, EventArgs e)
    {
        buttonDownloadModel?.Enabled = listViewAvailableModels?.SelectedItems.Count > 0;
    }

    private List<ModelInfo> _allModelZooModels = new();
    private bool _isScanningModelZoo = false;

    private async void buttonScanModelZoo_Click(object? sender, EventArgs e)
    {
        if (_isScanningModelZoo) return;

        _isScanningModelZoo = true;
        buttonScanModelZoo?.Enabled = false;
        labelModelZooStatus?.Text = "Scanning Microsoft Model Zoo...";

        try
        {
            _allModelZooModels = await _msModelZooService.GetAvailableModelsAsync();
            UpdateModelZooList();
            labelModelZooStatus?.Text = $"Found {_allModelZooModels.Count} models";
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "Scan Microsoft Model Zoo");
            MessageBox.Show($"Failed to scan Microsoft Model Zoo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelModelZooStatus?.Text = "Scan failed";
        }
        finally
        {
            buttonScanModelZoo?.Enabled = true;
            _isScanningModelZoo = false;
        }
    }

    private void UpdateModelZooList()
    {
        listViewModelZoo?.Items.Clear();
        
        foreach (var model in _allModelZooModels.OrderByDescending(m => m.Priority))
        {
            var item = new ListViewItem(model.DisplayName);
            item.SubItems.Add(model.AdditionalProperties.GetValueOrDefault("framework", "").ToString());
            item.SubItems.Add(model.AdditionalProperties.GetValueOrDefault("dataset", "").ToString());
            item.SubItems.Add(model.AdditionalProperties.GetValueOrDefault("accuracy", "").ToString());
            item.SubItems.Add(model.AdditionalProperties.GetValueOrDefault("model_size", "").ToString());
            item.SubItems.Add(model.License ?? "Unknown");
            item.SubItems.Add(model.Priority.ToString());
            item.SubItems.Add(model.Description ?? "");
            item.Tag = model;
            
            listViewModelZoo?.Items.Add(item);
        }
    }

    private async void buttonDownloadModelZoo_Click(object? sender, EventArgs e)
    {
        if (listViewModelZoo?.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a model to download.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedModel = listViewModelZoo?.SelectedItems[0].Tag as ModelInfo;
        if (selectedModel == null) return;

        var modelId = selectedModel.AdditionalProperties.GetValueOrDefault("model_zoo_id", "").ToString();
        if (string.IsNullOrEmpty(modelId))
        {
            MessageBox.Show("Invalid model selection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            buttonDownloadModelZoo?.Enabled = false;
            labelModelZooStatus?.Text = $"Downloading {selectedModel.DisplayName}...";

            var success = await _msModelZooService.DownloadModelAsync(modelId);
            
            if (success)
            {
                MessageBox.Show($"Successfully downloaded {selectedModel.DisplayName}", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                labelModelZooStatus?.Text = $"Downloaded {selectedModel.DisplayName}";
                
                // Refresh installed models list
                LoadModelsAsync();
            }
            else
            {
                MessageBox.Show($"Failed to download {selectedModel.DisplayName}", "Download Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                labelModelZooStatus?.Text = "Download failed";
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Download Microsoft Model Zoo model {modelId}");
            MessageBox.Show($"Error downloading model: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelModelZooStatus?.Text = "Download error";
        }
        finally
        {
            Cursor = Cursors.Default;
            buttonDownloadModelZoo?.Enabled = true;
        }
    }

    private bool IsPythonAvailable()
    {
        var possiblePaths = new[]
        {
            "python",
            "python3",
            "python.exe",
            "python3.exe"
        };
        foreach (var path in possiblePaths)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var process = new System.Diagnostics.Process { StartInfo = startInfo };
                process.Start();
                process.WaitForExit(2000);
                if (process.ExitCode == 0)
                    return true;
            }
            catch { }
        }
        return false;
    }

    private async void buttonConvertToOnnx_Click(object? sender, EventArgs e)
    {
        if (!IsPythonAvailable())
        {
            MessageBox.Show(
                "Python is not installed or not found in your system PATH. Please install Python 3.8+ and ensure it is available in your PATH before converting models.",
                "Python Not Found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }
        if (listViewRepositoryModels?.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a PyTorch model to convert to ONNX.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var selectedModel = listViewRepositoryModels.SelectedItems[0].Tag as ModelInfo;
        if (selectedModel == null) return;
        var modelId = selectedModel.AdditionalProperties.GetValueOrDefault("model_id", "").ToString();
        if (string.IsNullOrEmpty(modelId))
        {
            MessageBox.Show("Invalid model selection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        // Only allow conversion for PyTorch models that are not yet converted
        if (selectedModel.ModelType != ModelType.PyTorch || selectedModel.ConversionStatus != ConversionStatus.NotConverted)
        {
            MessageBox.Show("Only unconverted PyTorch models can be converted.", "Conversion Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        // Check if conversion is supported
        var isSupported = await _conversionService.IsConversionSupportedAsync(modelId);
        if (!isSupported)
        {
            var result = MessageBox.Show(
                $"Model {selectedModel.DisplayName} may not be compatible with ONNX conversion. Continue anyway?",
                "Conversion Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;
        }
        try
        {
            Cursor = Cursors.WaitCursor;
            buttonConvertToOnnx!.Enabled = false;
            labelRepositoryStatus!.Text = $"Converting {selectedModel.DisplayName} to ONNX...";
            // Update status in registry
            selectedModel.ConversionStatus = ConversionStatus.Converting;
            if (_currentRegistry != null)
                await _modelManager.SaveModelRegistryAsync(_currentRegistry, "models/model_registry.json");
            UpdateModelList();
            // Call the Python conversion script and capture output/errors
            var success = false;
            string output = string.Empty;
            string error = string.Empty;
            try
            {
                // Use the same logic as ModelConversionService, but capture output
                var scriptPath = await _conversionService.ConvertPyTorchToOnnxAsync(modelId, "", "");
                // If ConvertPyTorchToOnnxAsync returns true, treat as success
                success = scriptPath;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            // Update model type and conversion status after conversion
            if (success)
            {
                selectedModel.ModelType = ModelType.Onnx;
                selectedModel.ConversionStatus = ConversionStatus.Converted;
                MessageBox.Show($"Successfully converted {selectedModel.DisplayName} to ONNX format", "Conversion Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                labelRepositoryStatus.Text = $"Converted {selectedModel.DisplayName} to ONNX";
            }
            else
            {
                selectedModel.ConversionStatus = ConversionStatus.Failed;
                MessageBox.Show($"Failed to convert {selectedModel.DisplayName} to ONNX format.\n{error}", "Conversion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                labelRepositoryStatus.Text = "Conversion failed";
            }
            if (_currentRegistry != null)
                await _modelManager.SaveModelRegistryAsync(_currentRegistry, "models/model_registry.json");
            UpdateModelList();
        }
        catch (Exception ex)
        {
            selectedModel.ConversionStatus = ConversionStatus.Failed;
            if (_currentRegistry != null)
                await _modelManager.SaveModelRegistryAsync(_currentRegistry, "models/model_registry.json");
            UpdateModelList();
            _loggingService.LogException(ex, $"Convert to ONNX for {modelId}");
            MessageBox.Show($"Error converting model: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelRepositoryStatus!.Text = "Conversion error";
        }
        finally
        {
            Cursor = Cursors.Default;
            buttonConvertToOnnx!.Enabled = true;
        }
    }

    private void buttonDeleteAllModels_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to delete ALL models and their files? This cannot be undone.",
            "Confirm Delete All Models",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;
        try
        {
            var modelsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");
            if (Directory.Exists(modelsDir))
            {
                foreach (var dir in Directory.GetDirectories(modelsDir))
                {
                    if (Path.GetFileName(dir).Equals("conversion_scripts", StringComparison.OrdinalIgnoreCase))
                        continue;
                    Directory.Delete(dir, true);
                }
                foreach (var file in Directory.GetFiles(modelsDir))
                {
                    if (Path.GetFileName(file).Equals("model_registry.json", StringComparison.OrdinalIgnoreCase))
                        continue;
                    File.Delete(file);
                }
            }
            MessageBox.Show("All models deleted successfully.", "Delete Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadModelsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting models: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void buttonGenerateLabels_Click(object? sender, EventArgs e)
    {
        if (listViewRepositoryModels?.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a PyTorch model to generate labels for.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var selectedModel = listViewRepositoryModels.SelectedItems[0].Tag as ModelInfo;
        if (selectedModel == null || selectedModel.ModelType != ModelType.PyTorch)
        {
            MessageBox.Show("Only PyTorch models can have labels generated.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var modelDir = Path.Combine("models", selectedModel.Name.Replace("/", "-"));
        var labelsPath = Path.Combine(modelDir, "labels.txt");
        try
        {
            // Generate basic labels (1000 generic classes)
            var labels = Enumerable.Range(0, 1000).Select(i => $"class_{i:D4}").ToList();
            await File.WriteAllLinesAsync(labelsPath, labels);
            MessageBox.Show($"Labels file generated at {labelsPath}", "Labels Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadModelsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating labels: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void listViewRepositoryModels_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (listViewRepositoryModels?.SelectedItems.Count == 0)
        {
            buttonConvertToOnnx?.Enabled = false;
            buttonGenerateLabels?.Enabled = false;
            return;
        }
        var selectedModel = listViewRepositoryModels.SelectedItems[0].Tag as ModelInfo;
        if (selectedModel == null)
        {
            buttonConvertToOnnx?.Enabled = false;
            buttonGenerateLabels?.Enabled = false;
            return;
        }
        // Disable actions if model is not usable
        if (!IsModelUsable(selectedModel))
        {
            buttonConvertToOnnx!.Enabled = false;
            buttonGenerateLabels!.Enabled = false;
            return;
        }
        // Enable only if PyTorch and not yet converted
        buttonConvertToOnnx!.Enabled = (selectedModel.ModelType == ModelType.PyTorch && selectedModel.ConversionStatus == ConversionStatus.NotConverted);
        // Enable Generate Labels if PyTorch and labels file is missing
        var modelDir = Path.Combine("models", selectedModel.Name.Replace("/", "-"));
        var labelsPath = Path.Combine(modelDir, "labels.txt");
        buttonGenerateLabels!.Enabled = (selectedModel.ModelType == ModelType.PyTorch && !File.Exists(labelsPath));
    }
} 