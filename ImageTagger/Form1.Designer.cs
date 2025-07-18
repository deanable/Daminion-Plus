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
        this.comboBoxTagMethod = new System.Windows.Forms.ComboBox();
        this.btnTagImage = new System.Windows.Forms.Button();
        this.listBoxTags = new System.Windows.Forms.ListBox();
        this.btnSaveTags = new System.Windows.Forms.Button();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
        this.SuspendLayout();
        // 
        // btnSelectImage
        // 
        this.btnSelectImage.Location = new System.Drawing.Point(20, 20);
        this.btnSelectImage.Name = "btnSelectImage";
        this.btnSelectImage.Size = new System.Drawing.Size(120, 30);
        this.btnSelectImage.TabIndex = 0;
        this.btnSelectImage.Text = "Select Image";
        this.btnSelectImage.UseVisualStyleBackColor = true;
        this.btnSelectImage.Click += new System.EventHandler(this.btnSelectImage_Click);
        // 
        // pictureBoxPreview
        // 
        this.pictureBoxPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.pictureBoxPreview.Location = new System.Drawing.Point(20, 60);
        this.pictureBoxPreview.Name = "pictureBoxPreview";
        this.pictureBoxPreview.Size = new System.Drawing.Size(320, 240);
        this.pictureBoxPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.pictureBoxPreview.TabIndex = 1;
        this.pictureBoxPreview.TabStop = false;
        // 
        // comboBoxTagMethod
        // 
        this.comboBoxTagMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.comboBoxTagMethod.FormattingEnabled = true;
        this.comboBoxTagMethod.Items.AddRange(new object[] {"Cloud API", "ML.NET (Local)"});
        this.comboBoxTagMethod.Location = new System.Drawing.Point(360, 60);
        this.comboBoxTagMethod.Name = "comboBoxTagMethod";
        this.comboBoxTagMethod.Size = new System.Drawing.Size(180, 28);
        this.comboBoxTagMethod.TabIndex = 2;
        // 
        // btnTagImage
        // 
        this.btnTagImage.Location = new System.Drawing.Point(360, 100);
        this.btnTagImage.Name = "btnTagImage";
        this.btnTagImage.Size = new System.Drawing.Size(180, 30);
        this.btnTagImage.TabIndex = 3;
        this.btnTagImage.Text = "Tag Image";
        this.btnTagImage.UseVisualStyleBackColor = true;
        this.btnTagImage.Click += new System.EventHandler(this.btnTagImage_Click);
        // 
        // listBoxTags
        // 
        this.listBoxTags.FormattingEnabled = true;
        this.listBoxTags.ItemHeight = 20;
        this.listBoxTags.Location = new System.Drawing.Point(360, 140);
        this.listBoxTags.Name = "listBoxTags";
        this.listBoxTags.Size = new System.Drawing.Size(180, 164);
        this.listBoxTags.TabIndex = 4;
        // 
        // btnSaveTags
        // 
        this.btnSaveTags.Location = new System.Drawing.Point(360, 320);
        this.btnSaveTags.Name = "btnSaveTags";
        this.btnSaveTags.Size = new System.Drawing.Size(180, 30);
        this.btnSaveTags.TabIndex = 5;
        this.btnSaveTags.Text = "Save Tags to Image";
        this.btnSaveTags.UseVisualStyleBackColor = true;
        this.btnSaveTags.Click += new System.EventHandler(this.btnSaveTags_Click);
        // 
        // MainForm
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(570, 370);
        this.Controls.Add(this.btnSelectImage);
        this.Controls.Add(this.pictureBoxPreview);
        this.Controls.Add(this.comboBoxTagMethod);
        this.Controls.Add(this.btnTagImage);
        this.Controls.Add(this.listBoxTags);
        this.Controls.Add(this.btnSaveTags);
        this.Name = "MainForm";
        this.Text = "Image Tagger";
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Button btnSelectImage;
    private System.Windows.Forms.PictureBox pictureBoxPreview;
    private System.Windows.Forms.ComboBox comboBoxTagMethod;
    private System.Windows.Forms.Button btnTagImage;
    private System.Windows.Forms.ListBox listBoxTags;
    private System.Windows.Forms.Button btnSaveTags;
}
