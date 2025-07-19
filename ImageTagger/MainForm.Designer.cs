namespace ImageTagger;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.btnSelectImage = new System.Windows.Forms.Button();
        this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
        this.lblModel = new System.Windows.Forms.Label();
        this.comboBoxTagMethod = new System.Windows.Forms.ComboBox();
        this.btnManageModels = new System.Windows.Forms.Button();
        this.btnTagImage = new System.Windows.Forms.Button();
        this.lblTags = new System.Windows.Forms.Label();
        this.listBoxTags = new System.Windows.Forms.ListBox();
        this.btnSaveTags = new System.Windows.Forms.Button();
        this.progressBar = new System.Windows.Forms.ProgressBar();
        this.lblStatus = new System.Windows.Forms.Label();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
        this.SuspendLayout();
        // 
        // btnSelectImage
        // 
        this.btnSelectImage.Location = new System.Drawing.Point(20, 20);
        this.btnSelectImage.Name = "btnSelectImage";
        this.btnSelectImage.Size = new System.Drawing.Size(140, 35);
        this.btnSelectImage.TabIndex = 0;
        this.btnSelectImage.Text = "Select Image";
        this.btnSelectImage.UseVisualStyleBackColor = true;
        this.btnSelectImage.Click += new System.EventHandler(this.btnSelectImage_Click);
        // 
        // pictureBoxPreview
        // 
        this.pictureBoxPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.pictureBoxPreview.Location = new System.Drawing.Point(20, 70);
        this.pictureBoxPreview.Name = "pictureBoxPreview";
        this.pictureBoxPreview.Size = new System.Drawing.Size(400, 300);
        this.pictureBoxPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.pictureBoxPreview.TabIndex = 1;
        this.pictureBoxPreview.TabStop = false;
        // 
        // comboBoxTagMethod
        // 
        this.comboBoxTagMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.comboBoxTagMethod.FormattingEnabled = true;
        this.comboBoxTagMethod.Items.AddRange(new object[] {"Cloud API", "ML.NET (Local)"});
        this.comboBoxTagMethod.Location = new System.Drawing.Point(450, 20);
        this.comboBoxTagMethod.Name = "comboBoxTagMethod";
        this.comboBoxTagMethod.Size = new System.Drawing.Size(200, 28);
        this.comboBoxTagMethod.TabIndex = 2;
        // 
        // lblModel
        // 
        this.lblModel.AutoSize = true;
        this.lblModel.Location = new System.Drawing.Point(450, 0);
        this.lblModel.Name = "lblModel";
        this.lblModel.Size = new System.Drawing.Size(50, 20);
        this.lblModel.TabIndex = 9;
        this.lblModel.Text = "Model:";
        // 
        // btnManageModels
        // 
        this.btnManageModels.Location = new System.Drawing.Point(660, 20);
        this.btnManageModels.Name = "btnManageModels";
        this.btnManageModels.Size = new System.Drawing.Size(120, 28);
        this.btnManageModels.TabIndex = 8;
        this.btnManageModels.Text = "Manage Models";
        this.btnManageModels.UseVisualStyleBackColor = true;
        this.btnManageModels.Click += new System.EventHandler(this.btnManageModels_Click);
        // 
        // btnTagImage
        // 
        this.btnTagImage.Location = new System.Drawing.Point(450, 60);
        this.btnTagImage.Name = "btnTagImage";
        this.btnTagImage.Size = new System.Drawing.Size(200, 35);
        this.btnTagImage.TabIndex = 3;
        this.btnTagImage.Text = "Tag Image";
        this.btnTagImage.UseVisualStyleBackColor = true;
        this.btnTagImage.Click += new System.EventHandler(this.btnTagImage_Click);
        // 
        // lblTags
        // 
        this.lblTags.AutoSize = true;
        this.lblTags.Location = new System.Drawing.Point(450, 100);
        this.lblTags.Name = "lblTags";
        this.lblTags.Size = new System.Drawing.Size(40, 20);
        this.lblTags.TabIndex = 10;
        this.lblTags.Text = "Tags:";
        // 
        // listBoxTags
        // 
        this.listBoxTags.FormattingEnabled = true;
        this.listBoxTags.ItemHeight = 20;
        this.listBoxTags.Location = new System.Drawing.Point(450, 110);
        this.listBoxTags.Name = "listBoxTags";
        this.listBoxTags.Size = new System.Drawing.Size(330, 200);
        this.listBoxTags.TabIndex = 4;
        // 
        // btnSaveTags
        // 
        this.btnSaveTags.Location = new System.Drawing.Point(450, 320);
        this.btnSaveTags.Name = "btnSaveTags";
        this.btnSaveTags.Size = new System.Drawing.Size(200, 35);
        this.btnSaveTags.TabIndex = 5;
        this.btnSaveTags.Text = "Save Tags to Image";
        this.btnSaveTags.UseVisualStyleBackColor = true;
        this.btnSaveTags.Click += new System.EventHandler(this.btnSaveTags_Click);
        // 
        // progressBar
        // 
        this.progressBar.Location = new System.Drawing.Point(20, 390);
        this.progressBar.Name = "progressBar";
        this.progressBar.Size = new System.Drawing.Size(400, 23);
        this.progressBar.TabIndex = 6;
        this.progressBar.Visible = false;
        // 
        // lblStatus
        // 
        this.lblStatus.AutoSize = true;
        this.lblStatus.Location = new System.Drawing.Point(20, 420);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(50, 20);
        this.lblStatus.TabIndex = 7;
        this.lblStatus.Text = "Ready";
        // 
        // MainForm
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(950, 460);
        this.Controls.Add(this.btnSelectImage);
        this.Controls.Add(this.pictureBoxPreview);
        this.Controls.Add(this.lblModel);
        this.Controls.Add(this.comboBoxTagMethod);
        this.Controls.Add(this.btnManageModels);
        this.Controls.Add(this.btnTagImage);
        this.Controls.Add(this.lblTags);
        this.Controls.Add(this.listBoxTags);
        this.Controls.Add(this.btnSaveTags);
        this.Controls.Add(this.progressBar);
        this.Controls.Add(this.lblStatus);
        this.Name = "MainForm";
        this.Text = "Image Tagger";
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Button btnSelectImage;
    private System.Windows.Forms.PictureBox pictureBoxPreview;
    private System.Windows.Forms.Label lblModel;
    private System.Windows.Forms.ComboBox comboBoxTagMethod;
    private System.Windows.Forms.Button btnManageModels;
    private System.Windows.Forms.Button btnTagImage;
    private System.Windows.Forms.Label lblTags;
    private System.Windows.Forms.ListBox listBoxTags;
    private System.Windows.Forms.Button btnSaveTags;
    private System.Windows.Forms.ProgressBar progressBar;
    private System.Windows.Forms.Label lblStatus;
}
