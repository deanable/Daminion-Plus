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
        
        LoadModelsAsync();
    }

    private async void LoadModelsAsync()
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            
            // Load current registry
            _currentRegistry = await _modelManager.LoadModelRegistryAsync("models/model_registry.json");
            
            // Load available models from repository (basic list for now)
            var availableModels = await _modelDownloader.GetAvailableModelsFromRepositoryAsync();
            
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

    private void UpdateModelList()
    {
        if (_currentRegistry == null) return;

        listViewInstalledModels.Items.Clear();
        
        foreach (var model in _currentRegistry.Models.OrderByDescending(m => m.Priority))
        {
            var item = new ListViewItem(model.DisplayName);
            item.SubItems.Add(model.Name);
            item.SubItems.Add(model.IsEnabled ? "Enabled" : "Disabled");
            item.SubItems.Add(model.Priority.ToString());
            item.SubItems.Add(File.Exists(model.ModelPath) ? "✓" : "✗");
            item.SubItems.Add(File.Exists(model.LabelsPath) ? "✓" : "✗");
            item.Tag = model;
            
            listViewInstalledModels.Items.Add(item);
        }

        // Update default model selection
        if (!string.IsNullOrEmpty(_currentRegistry.DefaultModelName))
        {
            comboBoxDefaultModel.Text = _currentRegistry.DefaultModelName;
        }
    }

    private void UpdateAvailableModelsList(List<ModelInfo> availableModels)
    {
        listViewAvailableModels.Items.Clear();
        
        foreach (var model in availableModels)
        {
            var item = new ListViewItem(model.DisplayName);
            item.SubItems.Add(model.Name);
            item.SubItems.Add(model.Description);
            item.SubItems.Add(model.License);
            item.Tag = model;
            
            listViewAvailableModels.Items.Add(item);
        }
    }

    private void UpdateRepositoryModelsList()
    {
        listViewRepositoryModels.Items.Clear();
        
        foreach (var model in _filteredRepositoryModels)
        {
            var item = new ListViewItem(model.DisplayName);
            item.SubItems.Add(model.AdditionalProperties.GetValueOrDefault("downloads", 0).ToString());
            item.SubItems.Add(model.AdditionalProperties.GetValueOrDefault("likes", 0).ToString());
            item.SubItems.Add(model.License ?? "Unknown");
            item.SubItems.Add(model.Priority.ToString());
            item.SubItems.Add(model.Description ?? "");
            item.Tag = model;
            
            listViewRepositoryModels.Items.Add(item);
        }

        labelRepositoryStatus.Text = $"Models: {_filteredRepositoryModels.Count} found";
    }

    private async void buttonScanRepository_Click(object sender, EventArgs e)
    {
        if (_isScanning) return;

        _isScanning = true;
        buttonScanRepository.Enabled = false;
        progressBarRepository.Visible = true;
        labelRepositoryStatus.Text = "Scanning Hugging Face repository...";

        try
        {
            var filterOptions = GetFilterOptions();
            _allRepositoryModels = await _hfService.LoadEntireRepositoryAsync(filterOptions);
            
            labelRepositoryStatus.Text = $"Found {_allRepositoryModels.Count} models";
            ApplyRepositoryFilters();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "Scan repository");
            MessageBox.Show($"Failed to scan repository: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelRepositoryStatus.Text = "Scan failed";
        }
        finally
        {
            progressBarRepository.Visible = false;
            buttonScanRepository.Enabled = true;
            _isScanning = false;
        }
    }

    private ModelFilterOptions GetFilterOptions()
    {
        // Get license selection from combobox
        var licenseText = comboBoxLicenses.SelectedItem?.ToString() ?? "Apache 2.0, MIT, BSD";
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
            MinDownloads = trackBarMinDownloads.Value,
            MaxModelSizeMB = trackBarMaxSize.Value,
            MinLikes = trackBarMinLikes.Value,
            MaxModels = trackBarMaxModels.Value,
            ExcludeArchived = checkBoxExcludeArchived.Checked,
            ExcludePrivate = checkBoxExcludePrivate.Checked,
            OnlyVerified = checkBoxOnlyVerified.Checked,
            PreferImageNetLabels = checkBoxPreferImageNet.Checked,
            Licenses = licenses,
            SearchTerms = textBoxSearchTerms.Text?.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray(),
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
            if (downloads is int d && d < trackBarMinDownloads.Value) return false;

            // Check likes
            if (likes is int l && l < trackBarMinLikes.Value) return false;

            // Check license
            var licenseText = comboBoxLicenses.SelectedItem?.ToString() ?? "Apache 2.0, MIT, BSD";
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
        trackBarMinDownloads.Value = 100;
        trackBarMaxSize.Value = 500;
        trackBarMinLikes.Value = 0;
        trackBarMaxModels.Value = 100;
        comboBoxLicenses.SelectedIndex = 5; // "Apache 2.0, MIT, BSD"
        textBoxSearchTerms.Text = "vision,image,classification";
        checkBoxExcludeArchived.Checked = true;
        checkBoxExcludePrivate.Checked = true;
        checkBoxOnlyVerified.Checked = false;
        checkBoxPreferImageNet.Checked = true;
        
        ApplyRepositoryFilters();
    }

    private async void buttonDownloadRepositoryModel_Click(object sender, EventArgs e)
    {
        if (listViewRepositoryModels.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select at least one model to download.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedModels = listViewRepositoryModels.SelectedItems.Cast<ListViewItem>()
            .Select(item => item.Tag as ModelInfo)
            .Where(model => model != null)
            .ToList();

        var result = MessageBox.Show(
            $"Download {selectedModels.Count} selected models? This may take some time.",
            "Confirm Download",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        progressBarRepository.Visible = true;
        progressBarRepository.Style = ProgressBarStyle.Continuous;
        progressBarRepository.Maximum = selectedModels.Count;

        try
        {
            var successCount = 0;
            for (int i = 0; i < selectedModels.Count; i++)
            {
                var model = selectedModels[i];
                labelRepositoryStatus.Text = $"Downloading {model!.DisplayName}...";
                progressBarRepository.Value = i;

                try
                {
                    var huggingfaceId = model.AdditionalProperties.GetValueOrDefault("huggingface_id", "").ToString();
                    if (!string.IsNullOrEmpty(huggingfaceId))
                    {
                        var success = await _hfService.DownloadModelAsync(huggingfaceId);
                        if (success)
                        {
                            successCount++;
                            _loggingService.Log($"Successfully downloaded model: {model.DisplayName}");
                            
                            // Add to registry
                            var modelPath = Path.Combine("models", huggingfaceId.Replace("/", "-"));
                            var modelInfo = await _modelDownloader.CreateModelInfoFromDownloadedAsync(model.Name, modelPath);
                            _currentRegistry!.Models.Add(modelInfo);
                            await _modelManager.SaveModelRegistryAsync(_currentRegistry, "models/model_registry.json");
                        }
                        else
                        {
                            _loggingService.Log($"Failed to download model: {model.DisplayName}", LogLevel.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogException(ex, $"Download model {model.DisplayName}");
                }

                // Add delay to be respectful to the API
                await Task.Delay(500);
            }

            labelRepositoryStatus.Text = $"Download complete: {successCount}/{selectedModels.Count} models downloaded successfully";
            MessageBox.Show($"Download complete!\n{successCount} out of {selectedModels.Count} models downloaded successfully.", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // Refresh the installed models list
            UpdateModelList();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "Download selected models");
            MessageBox.Show($"Download failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelRepositoryStatus.Text = "Download failed";
        }
        finally
        {
            progressBarRepository.Visible = false;
        }
    }

    private async void buttonDownloadModel_Click(object sender, EventArgs e)
    {
        if (listViewAvailableModels.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a model to download.", "No Model Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedModel = (ModelInfo)listViewAvailableModels.SelectedItems[0].Tag;
        
        try
        {
            Cursor = Cursors.WaitCursor;
            buttonDownloadModel.Enabled = false;
            
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
                    _currentRegistry!.Models.Add(modelInfo);
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
            buttonDownloadModel.Enabled = true;
        }
    }

    private async void buttonEnableDisable_Click(object sender, EventArgs e)
    {
        if (listViewInstalledModels.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a model to enable/disable.", "No Model Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedModel = (ModelInfo)listViewInstalledModels.SelectedItems[0].Tag;
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

    private async void comboBoxDefaultModel_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(comboBoxDefaultModel.Text) || _currentRegistry == null) return;
        
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

    private async void buttonValidateModel_Click(object sender, EventArgs e)
    {
        if (listViewInstalledModels.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a model to validate.", "No Model Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedModel = (ModelInfo)listViewInstalledModels.SelectedItems[0].Tag;
        
        try
        {
            Cursor = Cursors.WaitCursor;
            buttonValidateModel.Enabled = false;
            
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
            buttonValidateModel.Enabled = true;
        }
    }

    private void buttonRefresh_Click(object sender, EventArgs e)
    {
        LoadModelsAsync();
    }

    private void buttonClose_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void listViewInstalledModels_SelectedIndexChanged(object sender, EventArgs e)
    {
        var hasSelection = listViewInstalledModels.SelectedItems.Count > 0;
        buttonEnableDisable.Enabled = hasSelection;
        buttonValidateModel.Enabled = hasSelection;
        
        if (hasSelection)
        {
            var selectedModel = (ModelInfo)listViewInstalledModels.SelectedItems[0].Tag;
            buttonEnableDisable.Text = selectedModel.IsEnabled ? "Disable" : "Enable";
        }
    }

    private void listViewAvailableModels_SelectedIndexChanged(object sender, EventArgs e)
    {
        buttonDownloadModel.Enabled = listViewAvailableModels.SelectedItems.Count > 0;
    }

    private void InitializeComponent()
    {
        this.listViewInstalledModels = new ListView();
        this.listViewAvailableModels = new ListView();
        this.listViewRepositoryModels = new ListView();
        this.buttonDownloadModel = new Button();
        this.buttonEnableDisable = new Button();
        this.buttonValidateModel = new Button();
        this.buttonRefresh = new Button();
        this.buttonClose = new Button();
        this.buttonScanRepository = new Button();
        this.buttonDownloadRepositoryModel = new Button();
        this.buttonApplyFilters = new Button();
        this.buttonClearFilters = new Button();
        this.comboBoxDefaultModel = new ComboBox();
        this.progressBarRepository = new ProgressBar();
        this.label1 = new Label();
        this.label2 = new Label();
        this.label3 = new Label();
        this.labelRepositoryStatus = new Label();
        this.labelRepositoryFilters = new Label();
        this.trackBarMinDownloads = new TrackBar();
        this.trackBarMaxSize = new TrackBar();
        this.trackBarMinLikes = new TrackBar();
        this.trackBarMaxModels = new TrackBar();
        this.comboBoxLicenses = new ComboBox();
        this.textBoxSearchTerms = new TextBox();
        this.labelMinDownloadsValue = new Label();
        this.labelMaxSizeValue = new Label();
        this.labelMinLikesValue = new Label();
        this.labelMaxModelsValue = new Label();
        this.checkBoxExcludeArchived = new CheckBox();
        this.checkBoxExcludePrivate = new CheckBox();
        this.checkBoxOnlyVerified = new CheckBox();
        this.checkBoxPreferImageNet = new CheckBox();
        this.tabControl = new TabControl();
        this.tabPageInstalled = new TabPage();
        this.tabPageRepository = new TabPage();
        this.SuspendLayout();
        
        // Form
        this.Text = "Model Management";
        this.Size = new Size(1200, 800);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        
        // Tab Control
        this.tabControl.Dock = DockStyle.Fill;
        this.tabControl.Controls.Add(this.tabPageInstalled);
        this.tabControl.Controls.Add(this.tabPageRepository);
        
        // Tab Page 1 - Installed Models
        this.tabPageInstalled.Text = "Installed Models";
        this.tabPageInstalled.Padding = new Padding(10);
        
        // Labels for Installed Models
        this.label1.Text = "Installed Models:";
        this.label1.Location = new Point(12, 9);
        this.label1.Size = new Size(100, 20);
        
        this.label2.Text = "Available Models (from Hugging Face Hub):";
        this.label2.Location = new Point(12, 250);
        this.label2.Size = new Size(280, 20);
        
        this.label3.Text = "Default Model:";
        this.label3.Location = new Point(12, 520);
        this.label3.Size = new Size(100, 20);
        
        // Installed Models ListView
        this.listViewInstalledModels.Location = new Point(12, 35);
        this.listViewInstalledModels.Size = new Size(1140, 200);
        this.listViewInstalledModels.View = View.Details;
        this.listViewInstalledModels.FullRowSelect = true;
        this.listViewInstalledModels.GridLines = true;
        this.listViewInstalledModels.Columns.Add("Display Name", 150);
        this.listViewInstalledModels.Columns.Add("Name", 120);
        this.listViewInstalledModels.Columns.Add("Status", 80);
        this.listViewInstalledModels.Columns.Add("Priority", 60);
        this.listViewInstalledModels.Columns.Add("Model File", 80);
        this.listViewInstalledModels.Columns.Add("Labels File", 80);
        this.listViewInstalledModels.SelectedIndexChanged += listViewInstalledModels_SelectedIndexChanged;
        
        // Available Models ListView
        this.listViewAvailableModels.Location = new Point(12, 275);
        this.listViewAvailableModels.Size = new Size(1140, 200);
        this.listViewAvailableModels.View = View.Details;
        this.listViewAvailableModels.FullRowSelect = true;
        this.listViewAvailableModels.GridLines = true;
        this.listViewAvailableModels.Columns.Add("Display Name", 150);
        this.listViewAvailableModels.Columns.Add("Name", 120);
        this.listViewAvailableModels.Columns.Add("Description", 400);
        this.listViewAvailableModels.Columns.Add("License", 80);
        this.listViewAvailableModels.SelectedIndexChanged += listViewAvailableModels_SelectedIndexChanged;
        
        // Default Model ComboBox
        this.comboBoxDefaultModel.Location = new Point(120, 520);
        this.comboBoxDefaultModel.Size = new Size(200, 25);
        this.comboBoxDefaultModel.DropDownStyle = ComboBoxStyle.DropDownList;
        this.comboBoxDefaultModel.SelectedIndexChanged += comboBoxDefaultModel_SelectedIndexChanged;
        
        // Buttons for Installed Models
        this.buttonDownloadModel.Text = "Download Selected Model";
        this.buttonDownloadModel.Location = new Point(12, 485);
        this.buttonDownloadModel.Size = new Size(160, 30);
        this.buttonDownloadModel.Enabled = false;
        this.buttonDownloadModel.Click += buttonDownloadModel_Click;
        
        this.buttonEnableDisable.Text = "Enable/Disable";
        this.buttonEnableDisable.Location = new Point(190, 485);
        this.buttonEnableDisable.Size = new Size(110, 30);
        this.buttonEnableDisable.Enabled = false;
        this.buttonEnableDisable.Click += buttonEnableDisable_Click;
        
        this.buttonValidateModel.Text = "Validate Model";
        this.buttonValidateModel.Location = new Point(320, 485);
        this.buttonValidateModel.Size = new Size(110, 30);
        this.buttonValidateModel.Enabled = false;
        this.buttonValidateModel.Click += buttonValidateModel_Click;
        
        this.buttonRefresh.Text = "Refresh";
        this.buttonRefresh.Location = new Point(450, 485);
        this.buttonRefresh.Size = new Size(90, 30);
        this.buttonRefresh.Click += buttonRefresh_Click;
        
        this.buttonClose.Text = "Close";
        this.buttonClose.Location = new Point(1080, 485);
        this.buttonClose.Size = new Size(90, 30);
        this.buttonClose.Click += buttonClose_Click;
        
        // Add controls to Installed Models tab
        this.tabPageInstalled.Controls.AddRange(new Control[] {
            this.label1, this.label2, this.label3,
            this.listViewInstalledModels, this.listViewAvailableModels,
            this.comboBoxDefaultModel,
            this.buttonDownloadModel, this.buttonEnableDisable, this.buttonValidateModel,
            this.buttonRefresh, this.buttonClose
        });
        
        // Tab Page 2 - Repository Browser
        this.tabPageRepository.Text = "Repository Browser";
        this.tabPageRepository.Padding = new Padding(10);
        
        // Repository Filters Label
        this.labelRepositoryFilters.Text = "Repository Filters:";
        this.labelRepositoryFilters.Location = new Point(12, 9);
        this.labelRepositoryFilters.Size = new Size(120, 20);
        this.labelRepositoryFilters.Font = new Font(Font.FontFamily, 10, FontStyle.Bold);
        
        // Filter Controls with proper spacing
        CreateTrackBarControl(this.tabPageRepository, "Min Downloads:", trackBarMinDownloads, labelMinDownloadsValue, 100, 1000, 100, 12, 35);
        CreateTrackBarControl(this.tabPageRepository, "Max Size (MB):", trackBarMaxSize, labelMaxSizeValue, 100, 1000, 500, 12, 85);
        CreateTrackBarControl(this.tabPageRepository, "Min Likes:", trackBarMinLikes, labelMinLikesValue, 0, 1000, 0, 12, 135);
        CreateTrackBarControl(this.tabPageRepository, "Max Models:", trackBarMaxModels, labelMaxModelsValue, 10, 500, 100, 12, 185);
        CreateLicenseComboBox(this.tabPageRepository, "Licenses:", comboBoxLicenses, 12, 235);
        CreateSearchTermsControl(this.tabPageRepository, "Search Terms:", textBoxSearchTerms, "vision,image,classification", 12, 285);
        
        // Checkboxes with better spacing
        CreateCheckbox(this.tabPageRepository, "Exclude Archived", "checkBoxExcludeArchived", true, 12, 335);
        CreateCheckbox(this.tabPageRepository, "Exclude Private", "checkBoxExcludePrivate", true, 12, 355);
        CreateCheckbox(this.tabPageRepository, "Only Verified", "checkBoxOnlyVerified", false, 12, 375);
        CreateCheckbox(this.tabPageRepository, "Prefer ImageNet Labels", "checkBoxPreferImageNet", true, 12, 395);
        
        // Filter Buttons with better spacing
        this.buttonApplyFilters.Text = "Apply Filters";
        this.buttonApplyFilters.Location = new Point(12, 425);
        this.buttonApplyFilters.Size = new Size(100, 30);
        this.buttonApplyFilters.BackColor = Color.LightGreen;
        this.buttonApplyFilters.Click += (s, e) => ApplyRepositoryFilters();
        
        this.buttonClearFilters.Text = "Clear Filters";
        this.buttonClearFilters.Location = new Point(120, 425);
        this.buttonClearFilters.Size = new Size(100, 30);
        this.buttonClearFilters.BackColor = Color.LightCoral;
        this.buttonClearFilters.Click += (s, e) => ClearRepositoryFilters();
        
        // Scan Button
        this.buttonScanRepository.Text = "🔍 Scan Repository";
        this.buttonScanRepository.Location = new Point(230, 425);
        this.buttonScanRepository.Size = new Size(140, 30);
        this.buttonScanRepository.BackColor = Color.LightBlue;
        this.buttonScanRepository.Click += buttonScanRepository_Click;
        
        // Progress Bar
        this.progressBarRepository.Location = new Point(380, 425);
        this.progressBarRepository.Size = new Size(200, 30);
        this.progressBarRepository.Visible = false;
        
        // Status Label
        this.labelRepositoryStatus.Text = "Ready to scan repository";
        this.labelRepositoryStatus.Location = new Point(590, 430);
        this.labelRepositoryStatus.Size = new Size(200, 20);
        
        // Repository Models ListView
        this.listViewRepositoryModels.Location = new Point(12, 470);
        this.listViewRepositoryModels.Size = new Size(1140, 230);
        this.listViewRepositoryModels.View = View.Details;
        this.listViewRepositoryModels.FullRowSelect = true;
        this.listViewRepositoryModels.GridLines = true;
        this.listViewRepositoryModels.MultiSelect = true;
        this.listViewRepositoryModels.Columns.Add("Model", 200);
        this.listViewRepositoryModels.Columns.Add("Downloads", 80);
        this.listViewRepositoryModels.Columns.Add("Likes", 60);
        this.listViewRepositoryModels.Columns.Add("License", 100);
        this.listViewRepositoryModels.Columns.Add("Priority", 60);
        this.listViewRepositoryModels.Columns.Add("Description", 400);
        
        // Download Button
        this.buttonDownloadRepositoryModel.Text = "Download Selected";
        this.buttonDownloadRepositoryModel.Location = new Point(12, 710);
        this.buttonDownloadRepositoryModel.Size = new Size(120, 30);
        this.buttonDownloadRepositoryModel.BackColor = Color.LightBlue;
        this.buttonDownloadRepositoryModel.Click += buttonDownloadRepositoryModel_Click;
        
        // Add controls to Repository Browser tab
        this.tabPageRepository.Controls.AddRange(new Control[] {
            this.labelRepositoryFilters, this.labelRepositoryStatus,
            this.trackBarMinDownloads, this.trackBarMaxSize, this.trackBarMinLikes, this.trackBarMaxModels,
            this.comboBoxLicenses, this.textBoxSearchTerms,
            this.labelMinDownloadsValue, this.labelMaxSizeValue, this.labelMinLikesValue, this.labelMaxModelsValue,
            this.checkBoxExcludeArchived, this.checkBoxExcludePrivate, this.checkBoxOnlyVerified, this.checkBoxPreferImageNet,
            this.buttonApplyFilters, this.buttonClearFilters, this.buttonScanRepository,
            this.progressBarRepository, this.listViewRepositoryModels, this.buttonDownloadRepositoryModel
        });
        
        // Add tab control to form
        this.Controls.Add(this.tabControl);
        
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private void CreateTrackBarControl(Control parent, string labelText, TrackBar trackBar, Label valueLabel, int min, int max, int defaultValue, int x, int y)
    {
        // Label
        var label = new Label { Text = labelText, Location = new Point(x, y), Size = new Size(120, 20) };
        parent.Controls.Add(label);

        // TrackBar
        trackBar.Minimum = min;
        trackBar.Maximum = max;
        trackBar.Value = defaultValue;
        trackBar.Location = new Point(x + 130, y);
        trackBar.Size = new Size(200, 45);
        trackBar.TickFrequency = (max - min) / 10;
        trackBar.TickStyle = TickStyle.BottomRight;
        trackBar.ValueChanged += (s, e) => valueLabel.Text = trackBar.Value.ToString();
        parent.Controls.Add(trackBar);

        // Value Label
        valueLabel.Text = defaultValue.ToString();
        valueLabel.Location = new Point(x + 340, y + 10);
        valueLabel.Size = new Size(50, 20);
        valueLabel.Font = new Font(Font.FontFamily, 9, FontStyle.Bold);
        parent.Controls.Add(valueLabel);
    }

    private void CreateLicenseComboBox(Control parent, string labelText, ComboBox comboBox, int x, int y)
    {
        // Label
        var label = new Label { Text = labelText, Location = new Point(x, y), Size = new Size(120, 20) };
        parent.Controls.Add(label);

        // ComboBox
        comboBox.Location = new Point(x + 130, y);
        comboBox.Size = new Size(200, 25);
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Items.AddRange(new object[] {
            "All Licenses",
            "Apache 2.0",
            "MIT",
            "BSD",
            "GPL",
            "Apache 2.0, MIT, BSD"
        });
        comboBox.SelectedIndex = 5; // Default to "Apache 2.0, MIT, BSD"
        parent.Controls.Add(comboBox);
    }

    private void CreateSearchTermsControl(Control parent, string labelText, TextBox textBox, string defaultValue, int x, int y)
    {
        // Label
        var label = new Label { Text = labelText, Location = new Point(x, y), Size = new Size(120, 20) };
        parent.Controls.Add(label);

        // TextBox
        textBox.Text = defaultValue;
        textBox.Location = new Point(x + 130, y);
        textBox.Size = new Size(200, 20);
        textBox.PlaceholderText = "Enter search terms separated by commas";
        parent.Controls.Add(textBox);
    }

    private void CreateCheckbox(Control parent, string text, string controlName, bool defaultValue, int x, int y)
    {
        var checkbox = new CheckBox
        {
            Text = text,
            Name = controlName,
            Checked = defaultValue,
            Location = new Point(x, y),
            Size = new Size(150, 20)
        };
        parent.Controls.Add(checkbox);
    }

    private ListView listViewInstalledModels;
    private ListView listViewAvailableModels;
    private ListView listViewRepositoryModels;
    private Button buttonDownloadModel;
    private Button buttonEnableDisable;
    private Button buttonValidateModel;
    private Button buttonRefresh;
    private Button buttonClose;
    private Button buttonScanRepository;
    private Button buttonDownloadRepositoryModel;
    private Button buttonApplyFilters;
    private Button buttonClearFilters;
    private ComboBox comboBoxDefaultModel;
    private ProgressBar progressBarRepository;
    private Label label1;
    private Label label2;
    private Label label3;
    private Label labelRepositoryStatus;
    private Label labelRepositoryFilters;
    private TrackBar trackBarMinDownloads;
    private TrackBar trackBarMaxSize;
    private TrackBar trackBarMinLikes;
    private TrackBar trackBarMaxModels;
    private ComboBox comboBoxLicenses;
    private TextBox textBoxSearchTerms;
    private Label labelMinDownloadsValue;
    private Label labelMaxSizeValue;
    private Label labelMinLikesValue;
    private Label labelMaxModelsValue;
    private CheckBox checkBoxExcludeArchived;
    private CheckBox checkBoxExcludePrivate;
    private CheckBox checkBoxOnlyVerified;
    private CheckBox checkBoxPreferImageNet;
    private TabControl tabControl;
    private TabPage tabPageInstalled;
    private TabPage tabPageRepository;
} 