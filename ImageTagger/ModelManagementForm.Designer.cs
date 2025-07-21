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
            this.buttonConvertToOnnx = new Button();
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
            this.tabPageModelZoo = new TabPage();
            this.listViewModelZoo = new ListView();
            this.buttonScanModelZoo = new Button();
            this.buttonDownloadModelZoo = new Button();
            this.labelModelZoo = new Label();
            this.labelModelZooStatus = new Label();
            this.buttonDeleteAllModels = new Button();
            this.buttonGenerateLabels = new Button();
            this.SuspendLayout();
            // 
            // listViewInstalledModels
            // 
            this.listViewInstalledModels.Location = new System.Drawing.Point(12, 12);
            this.listViewInstalledModels.Name = "listViewInstalledModels";
            this.listViewInstalledModels.Size = new System.Drawing.Size(200, 150);
            this.listViewInstalledModels.TabIndex = 0;
            this.listViewInstalledModels.UseCompatibleStateImageBehavior = false;
            this.listViewInstalledModels.View = System.Windows.Forms.View.Details;
            // 
            // listViewAvailableModels
            // 
            this.listViewAvailableModels.Location = new System.Drawing.Point(218, 12);
            this.listViewAvailableModels.Name = "listViewAvailableModels";
            this.listViewAvailableModels.Size = new System.Drawing.Size(200, 150);
            this.listViewAvailableModels.TabIndex = 1;
            this.listViewAvailableModels.UseCompatibleStateImageBehavior = false;
            this.listViewAvailableModels.View = System.Windows.Forms.View.Details;
            // 
            // listViewRepositoryModels
            // 
            this.listViewRepositoryModels.Location = new System.Drawing.Point(424, 12);
            this.listViewRepositoryModels.Name = "listViewRepositoryModels";
            this.listViewRepositoryModels.Size = new System.Drawing.Size(200, 150);
            this.listViewRepositoryModels.TabIndex = 2;
            this.listViewRepositoryModels.UseCompatibleStateImageBehavior = false;
            this.listViewRepositoryModels.View = System.Windows.Forms.View.Details;
            // 
            // buttonDownloadModel
            // 
            this.buttonDownloadModel.Location = new System.Drawing.Point(12, 168);
            this.buttonDownloadModel.Name = "buttonDownloadModel";
            this.buttonDownloadModel.Size = new System.Drawing.Size(75, 23);
            this.buttonDownloadModel.TabIndex = 3;
            this.buttonDownloadModel.Text = "Download";
            this.buttonDownloadModel.UseVisualStyleBackColor = true;
            // 
            // buttonEnableDisable
            // 
            this.buttonEnableDisable.Location = new System.Drawing.Point(93, 168);
            this.buttonEnableDisable.Name = "buttonEnableDisable";
            this.buttonEnableDisable.Size = new System.Drawing.Size(75, 23);
            this.buttonEnableDisable.TabIndex = 4;
            this.buttonEnableDisable.Text = "Enable/Disable";
            this.buttonEnableDisable.UseVisualStyleBackColor = true;
            // 
            // buttonValidateModel
            // 
            this.buttonValidateModel.Location = new System.Drawing.Point(174, 168);
            this.buttonValidateModel.Name = "buttonValidateModel";
            this.buttonValidateModel.Size = new System.Drawing.Size(75, 23);
            this.buttonValidateModel.TabIndex = 5;
            this.buttonValidateModel.Text = "Validate";
            this.buttonValidateModel.UseVisualStyleBackColor = true;
            // 
            // buttonRefresh
            // 
            this.buttonRefresh.Location = new System.Drawing.Point(255, 168);
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.Size = new System.Drawing.Size(75, 23);
            this.buttonRefresh.TabIndex = 6;
            this.buttonRefresh.Text = "Refresh";
            this.buttonRefresh.UseVisualStyleBackColor = true;
            // 
            // buttonClose
            // 
            this.buttonClose.Location = new System.Drawing.Point(336, 168);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(75, 23);
            this.buttonClose.TabIndex = 7;
            this.buttonClose.Text = "Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            // 
            // buttonScanRepository
            // 
            this.buttonScanRepository.Location = new System.Drawing.Point(417, 168);
            this.buttonScanRepository.Name = "buttonScanRepository";
            this.buttonScanRepository.Size = new System.Drawing.Size(107, 23);
            this.buttonScanRepository.TabIndex = 8;
            this.buttonScanRepository.Text = "Scan Repository";
            this.buttonScanRepository.UseVisualStyleBackColor = true;
            // 
            // buttonDownloadRepositoryModel
            // 
            this.buttonDownloadRepositoryModel.Location = new System.Drawing.Point(530, 168);
            this.buttonDownloadRepositoryModel.Name = "buttonDownloadRepositoryModel";
            this.buttonDownloadRepositoryModel.Size = new System.Drawing.Size(104, 23);
            this.buttonDownloadRepositoryModel.TabIndex = 9;
            this.buttonDownloadRepositoryModel.Text = "Download Repository";
            this.buttonDownloadRepositoryModel.UseVisualStyleBackColor = true;
            // 
            // buttonConvertToOnnx
            // 
            this.buttonConvertToOnnx.Location = new System.Drawing.Point(640, 168);
            this.buttonConvertToOnnx.Name = "buttonConvertToOnnx";
            this.buttonConvertToOnnx.Size = new System.Drawing.Size(104, 23);
            this.buttonConvertToOnnx.TabIndex = 10;
            this.buttonConvertToOnnx.Text = "Convert to ONNX";
            this.buttonConvertToOnnx.UseVisualStyleBackColor = true;
            // 
            // buttonApplyFilters
            // 
            this.buttonApplyFilters.Location = new System.Drawing.Point(750, 168);
            this.buttonApplyFilters.Name = "buttonApplyFilters";
            this.buttonApplyFilters.Size = new System.Drawing.Size(75, 23);
            this.buttonApplyFilters.TabIndex = 11;
            this.buttonApplyFilters.Text = "Apply Filters";
            this.buttonApplyFilters.UseVisualStyleBackColor = true;
            // 
            // buttonClearFilters
            // 
            this.buttonClearFilters.Location = new System.Drawing.Point(831, 168);
            this.buttonClearFilters.Name = "buttonClearFilters";
            this.buttonClearFilters.Size = new System.Drawing.Size(75, 23);
            this.buttonClearFilters.TabIndex = 12;
            this.buttonClearFilters.Text = "Clear Filters";
            this.buttonClearFilters.UseVisualStyleBackColor = true;
            // 
            // comboBoxDefaultModel
            // 
            this.comboBoxDefaultModel.FormattingEnabled = true;
            this.comboBoxDefaultModel.Location = new System.Drawing.Point(12, 197);
            this.comboBoxDefaultModel.Name = "comboBoxDefaultModel";
            this.comboBoxDefaultModel.Size = new System.Drawing.Size(121, 21);
            this.comboBoxDefaultModel.TabIndex = 13;
            // 
            // progressBarRepository
            // 
            this.progressBarRepository.Location = new System.Drawing.Point(139, 197);
            this.progressBarRepository.Name = "progressBarRepository";
            this.progressBarRepository.Size = new System.Drawing.Size(100, 23);
            this.progressBarRepository.TabIndex = 14;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 226);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 15;
            this.label1.Text = "Label 1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 242);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 16;
            this.label2.Text = "Label 2";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 258);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 17;
            this.label3.Text = "Label 3";
            // 
            // labelRepositoryStatus
            // 
            this.labelRepositoryStatus.AutoSize = true;
            this.labelRepositoryStatus.Location = new System.Drawing.Point(12, 274);
            this.labelRepositoryStatus.Name = "labelRepositoryStatus";
            this.labelRepositoryStatus.Size = new System.Drawing.Size(35, 13);
            this.labelRepositoryStatus.TabIndex = 18;
            this.labelRepositoryStatus.Text = "Status";
            // 
            // labelRepositoryFilters
            // 
            this.labelRepositoryFilters.AutoSize = true;
            this.labelRepositoryFilters.Location = new System.Drawing.Point(12, 290);
            this.labelRepositoryFilters.Name = "labelRepositoryFilters";
            this.labelRepositoryFilters.Size = new System.Drawing.Size(35, 13);
            this.labelRepositoryFilters.TabIndex = 19;
            this.labelRepositoryFilters.Text = "Filters";
            // 
            // trackBarMinDownloads
            // 
            this.trackBarMinDownloads.Location = new System.Drawing.Point(12, 306);
            this.trackBarMinDownloads.Name = "trackBarMinDownloads";
            this.trackBarMinDownloads.Size = new System.Drawing.Size(100, 45);
            this.trackBarMinDownloads.TabIndex = 20;
            // 
            // trackBarMaxSize
            // 
            this.trackBarMaxSize.Location = new System.Drawing.Point(118, 306);
            this.trackBarMaxSize.Name = "trackBarMaxSize";
            this.trackBarMaxSize.Size = new System.Drawing.Size(100, 45);
            this.trackBarMaxSize.TabIndex = 21;
            // 
            // trackBarMinLikes
            // 
            this.trackBarMinLikes.Location = new System.Drawing.Point(224, 306);
            this.trackBarMinLikes.Name = "trackBarMinLikes";
            this.trackBarMinLikes.Size = new System.Drawing.Size(100, 45);
            this.trackBarMinLikes.TabIndex = 22;
            // 
            // trackBarMaxModels
            // 
            this.trackBarMaxModels.Location = new System.Drawing.Point(330, 306);
            this.trackBarMaxModels.Name = "trackBarMaxModels";
            this.trackBarMaxModels.Size = new System.Drawing.Size(100, 45);
            this.trackBarMaxModels.TabIndex = 23;
            // 
            // comboBoxLicenses
            // 
            this.comboBoxLicenses.FormattingEnabled = true;
            this.comboBoxLicenses.Location = new System.Drawing.Point(12, 357);
            this.comboBoxLicenses.Name = "comboBoxLicenses";
            this.comboBoxLicenses.Size = new System.Drawing.Size(121, 21);
            this.comboBoxLicenses.TabIndex = 24;
            // 
            // textBoxSearchTerms
            // 
            this.textBoxSearchTerms.Location = new System.Drawing.Point(139, 357);
            this.textBoxSearchTerms.Name = "textBoxSearchTerms";
            this.textBoxSearchTerms.Size = new System.Drawing.Size(100, 20);
            this.textBoxSearchTerms.TabIndex = 25;
            // 
            // labelMinDownloadsValue
            // 
            this.labelMinDownloadsValue.AutoSize = true;
            this.labelMinDownloadsValue.Location = new System.Drawing.Point(12, 383);
            this.labelMinDownloadsValue.Name = "labelMinDownloadsValue";
            this.labelMinDownloadsValue.Size = new System.Drawing.Size(35, 13);
            this.labelMinDownloadsValue.TabIndex = 26;
            this.labelMinDownloadsValue.Text = "Value 1";
            // 
            // labelMaxSizeValue
            // 
            this.labelMaxSizeValue.AutoSize = true;
            this.labelMaxSizeValue.Location = new System.Drawing.Point(118, 383);
            this.labelMaxSizeValue.Name = "labelMaxSizeValue";
            this.labelMaxSizeValue.Size = new System.Drawing.Size(35, 13);
            this.labelMaxSizeValue.TabIndex = 27;
            this.labelMaxSizeValue.Text = "Value 2";
            // 
            // labelMinLikesValue
            // 
            this.labelMinLikesValue.AutoSize = true;
            this.labelMinLikesValue.Location = new System.Drawing.Point(224, 383);
            this.labelMinLikesValue.Name = "labelMinLikesValue";
            this.labelMinLikesValue.Size = new System.Drawing.Size(35, 13);
            this.labelMinLikesValue.TabIndex = 28;
            this.labelMinLikesValue.Text = "Value 3";
            // 
            // labelMaxModelsValue
            // 
            this.labelMaxModelsValue.AutoSize = true;
            this.labelMaxModelsValue.Location = new System.Drawing.Point(330, 383);
            this.labelMaxModelsValue.Name = "labelMaxModelsValue";
            this.labelMaxModelsValue.Size = new System.Drawing.Size(35, 13);
            this.labelMaxModelsValue.TabIndex = 29;
            this.labelMaxModelsValue.Text = "Value 4";
            // 
            // checkBoxExcludeArchived
            // 
            this.checkBoxExcludeArchived.AutoSize = true;
            this.checkBoxExcludeArchived.Location = new System.Drawing.Point(12, 409);
            this.checkBoxExcludeArchived.Name = "checkBoxExcludeArchived";
            this.checkBoxExcludeArchived.Size = new System.Drawing.Size(100, 17);
            this.checkBoxExcludeArchived.TabIndex = 30;
            this.checkBoxExcludeArchived.Text = "Exclude Archived";
            this.checkBoxExcludeArchived.UseVisualStyleBackColor = true;
            // 
            // checkBoxExcludePrivate
            // 
            this.checkBoxExcludePrivate.AutoSize = true;
            this.checkBoxExcludePrivate.Location = new System.Drawing.Point(12, 426);
            this.checkBoxExcludePrivate.Name = "checkBoxExcludePrivate";
            this.checkBoxExcludePrivate.Size = new System.Drawing.Size(100, 17);
            this.checkBoxExcludePrivate.TabIndex = 31;
            this.checkBoxExcludePrivate.Text = "Exclude Private";
            this.checkBoxExcludePrivate.UseVisualStyleBackColor = true;
            // 
            // checkBoxOnlyVerified
            // 
            this.checkBoxOnlyVerified.AutoSize = true;
            this.checkBoxOnlyVerified.Location = new System.Drawing.Point(12, 443);
            this.checkBoxOnlyVerified.Name = "checkBoxOnlyVerified";
            this.checkBoxOnlyVerified.Size = new System.Drawing.Size(100, 17);
            this.checkBoxOnlyVerified.TabIndex = 32;
            this.checkBoxOnlyVerified.Text = "Only Verified";
            this.checkBoxOnlyVerified.UseVisualStyleBackColor = true;
            // 
            // checkBoxPreferImageNet
            // 
            this.checkBoxPreferImageNet.AutoSize = true;
            this.checkBoxPreferImageNet.Location = new System.Drawing.Point(12, 460);
            this.checkBoxPreferImageNet.Name = "checkBoxPreferImageNet";
            this.checkBoxPreferImageNet.Size = new System.Drawing.Size(100, 17);
            this.checkBoxPreferImageNet.TabIndex = 33;
            this.checkBoxPreferImageNet.Text = "Prefer ImageNet";
            this.checkBoxPreferImageNet.UseVisualStyleBackColor = true;
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabPageInstalled);
            this.tabControl.Controls.Add(this.tabPageRepository);
            this.tabControl.Controls.Add(this.tabPageModelZoo);
            this.tabControl.Location = new System.Drawing.Point(12, 483);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(824, 200);
            this.tabControl.TabIndex = 34;
            // 
            // tabPageInstalled
            // 
            this.tabPageInstalled.Location = new System.Drawing.Point(4, 22);
            this.tabPageInstalled.Name = "tabPageInstalled";
            this.tabPageInstalled.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageInstalled.Size = new System.Drawing.Size(816, 174);
            this.tabPageInstalled.TabIndex = 0;
            this.tabPageInstalled.Text = "Installed Models";
            this.tabPageInstalled.UseVisualStyleBackColor = true;
            // 
            // tabPageRepository
            // 
            this.tabPageRepository.Location = new System.Drawing.Point(4, 22);
            this.tabPageRepository.Name = "tabPageRepository";
            this.tabPageRepository.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageRepository.Size = new System.Drawing.Size(816, 174);
            this.tabPageRepository.TabIndex = 1;
            this.tabPageRepository.Text = "Repository Models";
            this.tabPageRepository.UseVisualStyleBackColor = true;
            // 
            // tabPageModelZoo
            // 
            this.tabPageModelZoo.Location = new System.Drawing.Point(4, 22);
            this.tabPageModelZoo.Name = "tabPageModelZoo";
            this.tabPageModelZoo.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageModelZoo.Size = new System.Drawing.Size(816, 174);
            this.tabPageModelZoo.TabIndex = 2;
            this.tabPageModelZoo.Text = "Model Zoo";
            this.tabPageModelZoo.UseVisualStyleBackColor = true;
            // 
            // listViewModelZoo
            // 
            this.listViewModelZoo.Location = new System.Drawing.Point(12, 12);
            this.listViewModelZoo.Name = "listViewModelZoo";
            this.listViewModelZoo.Size = new System.Drawing.Size(200, 150);
            this.listViewModelZoo.TabIndex = 35;
            this.listViewModelZoo.UseCompatibleStateImageBehavior = false;
            this.listViewModelZoo.View = System.Windows.Forms.View.Details;
            // 
            // buttonScanModelZoo
            // 
            this.buttonScanModelZoo.Location = new System.Drawing.Point(12, 168);
            this.buttonScanModelZoo.Name = "buttonScanModelZoo";
            this.buttonScanModelZoo.Size = new System.Drawing.Size(75, 23);
            this.buttonScanModelZoo.TabIndex = 36;
            this.buttonScanModelZoo.Text = "Scan";
            this.buttonScanModelZoo.UseVisualStyleBackColor = true;
            // 
            // buttonDownloadModelZoo
            // 
            this.buttonDownloadModelZoo.Location = new System.Drawing.Point(93, 168);
            this.buttonDownloadModelZoo.Name = "buttonDownloadModelZoo";
            this.buttonDownloadModelZoo.Size = new System.Drawing.Size(75, 23);
            this.buttonDownloadModelZoo.TabIndex = 37;
            this.buttonDownloadModelZoo.Text = "Download";
            this.buttonDownloadModelZoo.UseVisualStyleBackColor = true;
            // 
            // labelModelZoo
            // 
            this.labelModelZoo.AutoSize = true;
            this.labelModelZoo.Location = new System.Drawing.Point(12, 22);
            this.labelModelZoo.Name = "labelModelZoo";
            this.labelModelZoo.Size = new System.Drawing.Size(50, 13);
            this.labelModelZoo.TabIndex = 38;
            this.labelModelZoo.Text = "Model Zoo";
            // 
            // labelModelZooStatus
            // 
            this.labelModelZooStatus.AutoSize = true;
            this.labelModelZooStatus.Location = new System.Drawing.Point(12, 38);
            this.labelModelZooStatus.Name = "labelModelZooStatus";
            this.labelModelZooStatus.Size = new System.Drawing.Size(35, 13);
            this.labelModelZooStatus.TabIndex = 39;
            this.labelModelZooStatus.Text = "Status";
            // 
            // buttonDeleteAllModels
            // 
            this.buttonDeleteAllModels.Location = new System.Drawing.Point(174, 168);
            this.buttonDeleteAllModels.Name = "buttonDeleteAllModels";
            this.buttonDeleteAllModels.Size = new System.Drawing.Size(75, 23);
            this.buttonDeleteAllModels.TabIndex = 40;
            this.buttonDeleteAllModels.Text = "Delete All";
            this.buttonDeleteAllModels.UseVisualStyleBackColor = true;
            // 
            // buttonGenerateLabels
            // 
            this.buttonGenerateLabels.Location = new System.Drawing.Point(255, 168);
            this.buttonGenerateLabels.Name = "buttonGenerateLabels";
            this.buttonGenerateLabels.Size = new System.Drawing.Size(75, 23);
            this.buttonGenerateLabels.TabIndex = 41;
            this.buttonGenerateLabels.Text = "Generate Labels";
            this.buttonGenerateLabels.UseVisualStyleBackColor = true;
            // 
            // ModelManagementForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(848, 695);
            this.Controls.Add(this.buttonGenerateLabels);
            this.Controls.Add(this.buttonDeleteAllModels);
            this.Controls.Add(this.labelModelZooStatus);
            this.Controls.Add(this.labelModelZoo);
            this.Controls.Add(this.buttonDownloadModelZoo);
            this.Controls.Add(this.buttonScanModelZoo);
            this.Controls.Add(this.listViewModelZoo);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.checkBoxPreferImageNet);
            this.Controls.Add(this.checkBoxOnlyVerified);
            this.Controls.Add(this.checkBoxExcludePrivate);
            this.Controls.Add(this.checkBoxExcludeArchived);
            this.Controls.Add(this.labelMaxModelsValue);
            this.Controls.Add(this.labelMinLikesValue);
            this.Controls.Add(this.labelMaxSizeValue);
            this.Controls.Add(this.labelMinDownloadsValue);
            this.Controls.Add(this.textBoxSearchTerms);
            this.Controls.Add(this.comboBoxLicenses);
            this.Controls.Add(this.trackBarMaxModels);
            this.Controls.Add(this.trackBarMinLikes);
            this.Controls.Add(this.trackBarMaxSize);
            this.Controls.Add(this.trackBarMinDownloads);
            this.Controls.Add(this.labelRepositoryFilters);
            this.Controls.Add(this.labelRepositoryStatus);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.progressBarRepository);
            this.Controls.Add(this.comboBoxDefaultModel);
            this.Controls.Add(this.buttonClearFilters);
            this.Controls.Add(this.buttonApplyFilters);
            this.Controls.Add(this.buttonConvertToOnnx);
            this.Controls.Add(this.buttonDownloadRepositoryModel);
            this.Controls.Add(this.buttonScanRepository);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.buttonRefresh);
            this.Controls.Add(this.buttonValidateModel);
            this.Controls.Add(this.buttonEnableDisable);
            this.Controls.Add(this.buttonDownloadModel);
            this.Controls.Add(this.listViewRepositoryModels);
            this.Controls.Add(this.listViewAvailableModels);
            this.Controls.Add(this.listViewInstalledModels);
            this.Name = "ModelManagementForm";
            this.Text = "Model Management";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
} 