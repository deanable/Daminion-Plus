using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;
using ImageTagger.Infrastructure;
using ImageTagger.Services;

namespace ImageTagger;

public partial class ModelManagementForm : Form
{
    private readonly IModelManager _modelManager;
    private readonly ModelDownloaderService _modelDownloader;
    private readonly ILoggingService _loggingService;
    private ModelRegistry? _currentRegistry;

    public ModelManagementForm(ILoggingService loggingService)
    {
        InitializeComponent();
        _loggingService = loggingService;
        _modelManager = new ModelManager(loggingService);
        _modelDownloader = new ModelDownloaderService(loggingService);
        
        LoadModelsAsync();
    }

    private async void LoadModelsAsync()
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            
            // Load current registry
            _currentRegistry = await _modelManager.LoadModelRegistryAsync("models/model_registry.json");
            
            // Load available models from repository
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
                var modelPath = Path.Combine("models", selectedModel.Name);
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
        this.buttonDownloadModel = new Button();
        this.buttonEnableDisable = new Button();
        this.buttonValidateModel = new Button();
        this.buttonRefresh = new Button();
        this.buttonClose = new Button();
        this.comboBoxDefaultModel = new ComboBox();
        this.label1 = new Label();
        this.label2 = new Label();
        this.label3 = new Label();
        this.SuspendLayout();
        
        // Form
        this.Text = "Model Management";
        this.Size = new Size(1000, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        
        // Labels
        this.label1.Text = "Installed Models:";
        this.label1.Location = new Point(12, 9);
        this.label1.Size = new Size(100, 20);
        
        this.label2.Text = "Available Models (from ONNX Repository):";
        this.label2.Location = new Point(12, 250);
        this.label2.Size = new Size(250, 20);
        
        this.label3.Text = "Default Model:";
        this.label3.Location = new Point(12, 500);
        this.label3.Size = new Size(100, 20);
        
        // Installed Models ListView
        this.listViewInstalledModels.Location = new Point(12, 35);
        this.listViewInstalledModels.Size = new Size(960, 200);
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
        this.listViewAvailableModels.Size = new Size(960, 200);
        this.listViewAvailableModels.View = View.Details;
        this.listViewAvailableModels.FullRowSelect = true;
        this.listViewAvailableModels.GridLines = true;
        this.listViewAvailableModels.Columns.Add("Display Name", 150);
        this.listViewAvailableModels.Columns.Add("Name", 120);
        this.listViewAvailableModels.Columns.Add("Description", 400);
        this.listViewAvailableModels.Columns.Add("License", 80);
        this.listViewAvailableModels.SelectedIndexChanged += listViewAvailableModels_SelectedIndexChanged;
        
        // Default Model ComboBox
        this.comboBoxDefaultModel.Location = new Point(120, 500);
        this.comboBoxDefaultModel.Size = new Size(200, 25);
        this.comboBoxDefaultModel.DropDownStyle = ComboBoxStyle.DropDownList;
        this.comboBoxDefaultModel.SelectedIndexChanged += comboBoxDefaultModel_SelectedIndexChanged;
        
        // Buttons
        this.buttonDownloadModel.Text = "Download Selected Model";
        this.buttonDownloadModel.Location = new Point(12, 485);
        this.buttonDownloadModel.Size = new Size(150, 30);
        this.buttonDownloadModel.Enabled = false;
        this.buttonDownloadModel.Click += buttonDownloadModel_Click;
        
        this.buttonEnableDisable.Text = "Enable/Disable";
        this.buttonEnableDisable.Location = new Point(180, 485);
        this.buttonEnableDisable.Size = new Size(100, 30);
        this.buttonEnableDisable.Enabled = false;
        this.buttonEnableDisable.Click += buttonEnableDisable_Click;
        
        this.buttonValidateModel.Text = "Validate Model";
        this.buttonValidateModel.Location = new Point(300, 485);
        this.buttonValidateModel.Size = new Size(100, 30);
        this.buttonValidateModel.Enabled = false;
        this.buttonValidateModel.Click += buttonValidateModel_Click;
        
        this.buttonRefresh.Text = "Refresh";
        this.buttonRefresh.Location = new Point(420, 485);
        this.buttonRefresh.Size = new Size(80, 30);
        this.buttonRefresh.Click += buttonRefresh_Click;
        
        this.buttonClose.Text = "Close";
        this.buttonClose.Location = new Point(880, 485);
        this.buttonClose.Size = new Size(80, 30);
        this.buttonClose.Click += buttonClose_Click;
        
        // Add controls
        this.Controls.AddRange(new Control[] {
            this.label1, this.label2, this.label3,
            this.listViewInstalledModels, this.listViewAvailableModels,
            this.comboBoxDefaultModel,
            this.buttonDownloadModel, this.buttonEnableDisable, this.buttonValidateModel,
            this.buttonRefresh, this.buttonClose
        });
        
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private ListView listViewInstalledModels;
    private ListView listViewAvailableModels;
    private Button buttonDownloadModel;
    private Button buttonEnableDisable;
    private Button buttonValidateModel;
    private Button buttonRefresh;
    private Button buttonClose;
    private ComboBox comboBoxDefaultModel;
    private Label label1;
    private Label label2;
    private Label label3;
} 