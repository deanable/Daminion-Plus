// This file is auto-generated for WinForms Designer compatibility.
// All UI control declarations and the InitializeComponent() method are placed here.

namespace ImageTagger
{
    partial class ModelManagementForm
    {
        // Control declarations (extracted from ModelManagementForm.cs)
        private System.Windows.Forms.ListView? listViewInstalledModels;
        private System.Windows.Forms.ListView? listViewAvailableModels;
        private System.Windows.Forms.ListView? listViewRepositoryModels;
        private System.Windows.Forms.Button? buttonDownloadModel;
        private System.Windows.Forms.Button? buttonEnableDisable;
        private System.Windows.Forms.Button? buttonValidateModel;
        private System.Windows.Forms.Button? buttonRefresh;
        private System.Windows.Forms.Button? buttonClose;
        private System.Windows.Forms.Button? buttonScanRepository;
        private System.Windows.Forms.Button? buttonDownloadRepositoryModel;
        private System.Windows.Forms.Button? buttonConvertToOnnx;
        private System.Windows.Forms.Button? buttonApplyFilters;
        private System.Windows.Forms.Button? buttonClearFilters;
        private System.Windows.Forms.ComboBox? comboBoxDefaultModel;
        private System.Windows.Forms.ProgressBar? progressBarRepository;
        private System.Windows.Forms.Label? label1;
        private System.Windows.Forms.Label? label2;
        private System.Windows.Forms.Label? label3;
        private System.Windows.Forms.Label? labelRepositoryStatus;
        private System.Windows.Forms.Label? labelRepositoryFilters;
        private System.Windows.Forms.TrackBar? trackBarMinDownloads;
        private System.Windows.Forms.TrackBar? trackBarMaxSize;
        private System.Windows.Forms.TrackBar? trackBarMinLikes;
        private System.Windows.Forms.TrackBar? trackBarMaxModels;
        private System.Windows.Forms.ComboBox? comboBoxLicenses;
        private System.Windows.Forms.TextBox? textBoxSearchTerms;
        private System.Windows.Forms.Label? labelMinDownloadsValue;
        private System.Windows.Forms.Label? labelMaxSizeValue;
        private System.Windows.Forms.Label? labelMinLikesValue;
        private System.Windows.Forms.Label? labelMaxModelsValue;
        private System.Windows.Forms.CheckBox? checkBoxExcludeArchived;
        private System.Windows.Forms.CheckBox? checkBoxExcludePrivate;
        private System.Windows.Forms.CheckBox? checkBoxOnlyVerified;
        private System.Windows.Forms.CheckBox? checkBoxPreferImageNet;
        private System.Windows.Forms.TabControl? tabControl;
        private System.Windows.Forms.TabPage? tabPageInstalled;
        private System.Windows.Forms.TabPage? tabPageRepository;
        private System.Windows.Forms.TabPage? tabPageModelZoo;
        private System.Windows.Forms.ListView? listViewModelZoo;
        private System.Windows.Forms.Button? buttonScanModelZoo;
        private System.Windows.Forms.Button? buttonDownloadModelZoo;
        private System.Windows.Forms.Label? labelModelZoo;
        private System.Windows.Forms.Label? labelModelZooStatus;
        private System.Windows.Forms.Button? buttonDeleteAllModels;
        private System.Windows.Forms.Button? buttonGenerateLabels;

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // Main TabControl
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPageInstalled = new System.Windows.Forms.TabPage();
            this.tabPageRepository = new System.Windows.Forms.TabPage();
            this.tabPageModelZoo = new System.Windows.Forms.TabPage();
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Controls.Add(this.tabPageInstalled);
            this.tabControl.Controls.Add(this.tabPageRepository);
            this.tabControl.Controls.Add(this.tabPageModelZoo);

            // === Installed Models Tab ===
            this.listViewInstalledModels = new System.Windows.Forms.ListView();
            this.listViewInstalledModels.Dock = System.Windows.Forms.DockStyle.Top;
            this.listViewInstalledModels.Height = 350;
            this.listViewInstalledModels.FullRowSelect = true;
            this.listViewInstalledModels.GridLines = true;
            this.listViewInstalledModels.View = System.Windows.Forms.View.Details;
            this.listViewInstalledModels.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                new System.Windows.Forms.ColumnHeader() { Text = "Display Name", Width = 150 },
                new System.Windows.Forms.ColumnHeader() { Text = "Name", Width = 120 },
                new System.Windows.Forms.ColumnHeader() { Text = "Status", Width = 80 },
                new System.Windows.Forms.ColumnHeader() { Text = "Priority", Width = 60 },
                new System.Windows.Forms.ColumnHeader() { Text = "Model File", Width = 80 },
                new System.Windows.Forms.ColumnHeader() { Text = "Labels File", Width = 80 },
                new System.Windows.Forms.ColumnHeader() { Text = "Type", Width = 80 },
                new System.Windows.Forms.ColumnHeader() { Text = "Conversion", Width = 100 }
            });

            // Installed Models Action Buttons
            this.buttonEnableDisable = new System.Windows.Forms.Button();
            this.buttonEnableDisable.Text = "Enable/Disable";
            this.buttonEnableDisable.Width = 120;
            this.buttonValidateModel = new System.Windows.Forms.Button();
            this.buttonValidateModel.Text = "Validate";
            this.buttonValidateModel.Width = 100;
            this.buttonDeleteAllModels = new System.Windows.Forms.Button();
            this.buttonDeleteAllModels.Text = "Delete All";
            this.buttonDeleteAllModels.Width = 100;
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.buttonRefresh.Text = "Refresh";
            this.buttonRefresh.Width = 100;
            this.buttonClose = new System.Windows.Forms.Button();
            this.buttonClose.Text = "Close";
            this.buttonClose.Width = 100;
            // Arrange buttons in a horizontal panel
            var panelInstalledButtons = new System.Windows.Forms.FlowLayoutPanel();
            panelInstalledButtons.Dock = System.Windows.Forms.DockStyle.Top;
            panelInstalledButtons.Height = 40;
            panelInstalledButtons.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.buttonEnableDisable, this.buttonValidateModel, this.buttonDeleteAllModels, this.buttonRefresh, this.buttonClose
            });
            this.tabPageInstalled.Controls.Add(panelInstalledButtons);
            this.tabPageInstalled.Controls.Add(this.listViewInstalledModels);

            // === Repository Models Tab ===
            // Filters panel
            var panelFilters = new System.Windows.Forms.FlowLayoutPanel();
            panelFilters.Dock = System.Windows.Forms.DockStyle.Top;
            panelFilters.Height = 80;
            panelFilters.AutoScroll = true;
            this.trackBarMinDownloads = new System.Windows.Forms.TrackBar();
            this.trackBarMinDownloads.Width = 120;
            this.trackBarMinDownloads.Minimum = 0;
            this.trackBarMinDownloads.Maximum = 10000;
            this.trackBarMinDownloads.TickFrequency = 1000;
            this.trackBarMinDownloads.Value = 100;
            this.trackBarMaxSize = new System.Windows.Forms.TrackBar();
            this.trackBarMaxSize.Width = 120;
            this.trackBarMaxSize.Minimum = 0;
            this.trackBarMaxSize.Maximum = 1000;
            this.trackBarMaxSize.TickFrequency = 100;
            this.trackBarMaxSize.Value = 500;
            this.trackBarMinLikes = new System.Windows.Forms.TrackBar();
            this.trackBarMinLikes.Width = 120;
            this.trackBarMinLikes.Minimum = 0;
            this.trackBarMinLikes.Maximum = 1000;
            this.trackBarMinLikes.TickFrequency = 100;
            this.trackBarMinLikes.Value = 0;
            this.trackBarMaxModels = new System.Windows.Forms.TrackBar();
            this.trackBarMaxModels.Width = 120;
            this.trackBarMaxModels.Minimum = 10;
            this.trackBarMaxModels.Maximum = 500;
            this.trackBarMaxModels.TickFrequency = 50;
            this.trackBarMaxModels.Value = 100;
            this.comboBoxLicenses = new System.Windows.Forms.ComboBox();
            this.comboBoxLicenses.Width = 120;
            this.comboBoxLicenses.Items.AddRange(new object[] { "All Licenses", "Apache 2.0", "MIT", "BSD", "GPL", "Apache 2.0, MIT, BSD" });
            this.comboBoxLicenses.SelectedIndex = 0;
            this.textBoxSearchTerms = new System.Windows.Forms.TextBox();
            this.textBoxSearchTerms.Width = 120;
            this.textBoxSearchTerms.PlaceholderText = "Search terms";
            this.checkBoxExcludeArchived = new System.Windows.Forms.CheckBox();
            this.checkBoxExcludeArchived.Text = "Exclude Archived";
            this.checkBoxExcludePrivate = new System.Windows.Forms.CheckBox();
            this.checkBoxExcludePrivate.Text = "Exclude Private";
            this.checkBoxOnlyVerified = new System.Windows.Forms.CheckBox();
            this.checkBoxOnlyVerified.Text = "Only Verified";
            this.checkBoxPreferImageNet = new System.Windows.Forms.CheckBox();
            this.checkBoxPreferImageNet.Text = "Prefer ImageNet";
            panelFilters.Controls.AddRange(new System.Windows.Forms.Control[] {
                new System.Windows.Forms.Label() { Text = "Min Downloads" }, this.trackBarMinDownloads,
                new System.Windows.Forms.Label() { Text = "Max Size (MB)" }, this.trackBarMaxSize,
                new System.Windows.Forms.Label() { Text = "Min Likes" }, this.trackBarMinLikes,
                new System.Windows.Forms.Label() { Text = "Max Models" }, this.trackBarMaxModels,
                new System.Windows.Forms.Label() { Text = "Licenses" }, this.comboBoxLicenses,
                new System.Windows.Forms.Label() { Text = "Search" }, this.textBoxSearchTerms,
                this.checkBoxExcludeArchived, this.checkBoxExcludePrivate, this.checkBoxOnlyVerified, this.checkBoxPreferImageNet
            });

            // Repository Models ListView
            this.listViewRepositoryModels = new System.Windows.Forms.ListView();
            this.listViewRepositoryModels.Dock = System.Windows.Forms.DockStyle.Top;
            this.listViewRepositoryModels.Height = 300;
            this.listViewRepositoryModels.FullRowSelect = true;
            this.listViewRepositoryModels.GridLines = true;
            this.listViewRepositoryModels.View = System.Windows.Forms.View.Details;
            this.listViewRepositoryModels.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                new System.Windows.Forms.ColumnHeader() { Text = "Model", Width = 200 },
                new System.Windows.Forms.ColumnHeader() { Text = "Downloads", Width = 80 },
                new System.Windows.Forms.ColumnHeader() { Text = "Likes", Width = 60 },
                new System.Windows.Forms.ColumnHeader() { Text = "License", Width = 100 },
                new System.Windows.Forms.ColumnHeader() { Text = "Type", Width = 80 },
                new System.Windows.Forms.ColumnHeader() { Text = "Priority", Width = 60 },
                new System.Windows.Forms.ColumnHeader() { Text = "Description", Width = 320 }
            });

            // Repository Models Action Buttons
            this.buttonScanRepository = new System.Windows.Forms.Button();
            this.buttonScanRepository.Text = "Scan Repository";
            this.buttonScanRepository.Width = 120;
            this.buttonDownloadRepositoryModel = new System.Windows.Forms.Button();
            this.buttonDownloadRepositoryModel.Text = "Download Selected";
            this.buttonDownloadRepositoryModel.Width = 120;
            this.buttonConvertToOnnx = new System.Windows.Forms.Button();
            this.buttonConvertToOnnx.Text = "Convert to ONNX";
            this.buttonConvertToOnnx.Width = 120;
            this.buttonGenerateLabels = new System.Windows.Forms.Button();
            this.buttonGenerateLabels.Text = "Generate Labels";
            this.buttonGenerateLabels.Width = 120;
            this.buttonApplyFilters = new System.Windows.Forms.Button();
            this.buttonApplyFilters.Text = "Apply Filters";
            this.buttonApplyFilters.Width = 100;
            this.buttonClearFilters = new System.Windows.Forms.Button();
            this.buttonClearFilters.Text = "Clear Filters";
            this.buttonClearFilters.Width = 100;
            // Arrange buttons in a horizontal panel
            var panelRepoButtons = new System.Windows.Forms.FlowLayoutPanel();
            panelRepoButtons.Dock = System.Windows.Forms.DockStyle.Top;
            panelRepoButtons.Height = 40;
            panelRepoButtons.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.buttonScanRepository, this.buttonDownloadRepositoryModel, this.buttonConvertToOnnx, this.buttonGenerateLabels, this.buttonApplyFilters, this.buttonClearFilters
            });
            this.tabPageRepository.Controls.Add(panelRepoButtons);
            this.tabPageRepository.Controls.Add(this.listViewRepositoryModels);
            this.tabPageRepository.Controls.Add(panelFilters);

            // === Model Zoo Tab ===
            this.listViewModelZoo = new System.Windows.Forms.ListView();
            this.listViewModelZoo.Dock = System.Windows.Forms.DockStyle.Top;
            this.listViewModelZoo.Height = 350;
            this.listViewModelZoo.FullRowSelect = true;
            this.listViewModelZoo.GridLines = true;
            this.listViewModelZoo.View = System.Windows.Forms.View.Details;
            this.listViewModelZoo.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                new System.Windows.Forms.ColumnHeader() { Text = "Model", Width = 200 },
                new System.Windows.Forms.ColumnHeader() { Text = "Source", Width = 100 },
                new System.Windows.Forms.ColumnHeader() { Text = "Framework", Width = 80 },
                new System.Windows.Forms.ColumnHeader() { Text = "Dataset", Width = 100 },
                new System.Windows.Forms.ColumnHeader() { Text = "Accuracy", Width = 120 },
                new System.Windows.Forms.ColumnHeader() { Text = "Size", Width = 80 },
                new System.Windows.Forms.ColumnHeader() { Text = "License", Width = 100 },
                new System.Windows.Forms.ColumnHeader() { Text = "Priority", Width = 60 },
                new System.Windows.Forms.ColumnHeader() { Text = "Description", Width = 300 }
            });
            // Model Zoo Action Buttons
            this.buttonScanModelZoo = new System.Windows.Forms.Button();
            this.buttonScanModelZoo.Text = "Scan Model Zoo";
            this.buttonScanModelZoo.Width = 120;
            this.buttonDownloadModelZoo = new System.Windows.Forms.Button();
            this.buttonDownloadModelZoo.Text = "Download Selected";
            this.buttonDownloadModelZoo.Width = 120;
            var panelZooButtons = new System.Windows.Forms.FlowLayoutPanel();
            panelZooButtons.Dock = System.Windows.Forms.DockStyle.Top;
            panelZooButtons.Height = 40;
            panelZooButtons.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.buttonScanModelZoo, this.buttonDownloadModelZoo
            });
            this.tabPageModelZoo.Controls.Add(panelZooButtons);
            this.tabPageModelZoo.Controls.Add(this.listViewModelZoo);

            // Add the main tab control to the form
            this.Controls.Add(this.tabControl);
            this.Text = "Model Management";
            this.Size = new System.Drawing.Size(1200, 800);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
        }
    }
} 