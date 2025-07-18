using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;

namespace ImageTagger
{
    public partial class MainForm : Form
    {
        private string selectedImagePath;
        private List<string> currentTags = new List<string>();

        // ML.NET model/label paths
        private static readonly string ModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "resnet50-v1-12.onnx");
        private static readonly string LabelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "imagenet_classes.txt");
        private static readonly int ImageWidth = 224;
        private static readonly int ImageHeight = 224;

        public MainForm()
        {
            InitializeComponent();
            comboBoxTagMethod.SelectedIndex = 0;
            // Removed PrintOnnxModelInfo from constructor
        }

        private void PrintOnnxModelInfo(string modelPath)
        {
            try
            {
                using var session = new InferenceSession(modelPath);
                Console.WriteLine("ONNX Model Inputs:");
                foreach (var input in session.InputMetadata)
                    Console.WriteLine($"  {input.Key} : {input.Value.ElementType} [{string.Join(",", input.Value.Dimensions)}]");
                Console.WriteLine("ONNX Model Outputs:");
                foreach (var output in session.OutputMetadata)
                    Console.WriteLine($"  {output.Key} : {output.Value.ElementType} [{string.Join(",", output.Value.Dimensions)}]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inspecting ONNX model: {ex.Message}");
            }
        }

        private void btnSelectImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    selectedImagePath = ofd.FileName;
                    pictureBoxPreview.Image = Image.FromFile(selectedImagePath);
                    listBoxTags.Items.Clear();
                    currentTags.Clear();
                }
            }
        }

        private async void btnTagImage_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedImagePath))
            {
                MessageBox.Show("Please select an image first.");
                return;
            }
            listBoxTags.Items.Clear();
            currentTags.Clear();
            var method = comboBoxTagMethod.SelectedItem.ToString();
            List<string> tags = new List<string>();
            if (method == "Cloud API")
            {
                // TODO: Call cloud API for tagging
                tags = await TagImageWithCloudApi(selectedImagePath);
            }
            else
            {
                // TODO: Use ML.NET for local tagging
                tags = await TagImageWithMLNet(selectedImagePath);
            }
            foreach (var tag in tags)
            {
                listBoxTags.Items.Add(tag);
                currentTags.Add(tag);
            }
        }

        private void btnSaveTags_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedImagePath) || currentTags.Count == 0)
            {
                MessageBox.Show("No tags to save.");
                return;
            }
            // TODO: Write tags to image metadata (EXIF/IPTC/XMP)
            SaveTagsToImage(selectedImagePath, currentTags);
            MessageBox.Show("Tags saved to image metadata.");
        }

        // Placeholder for cloud API tagging
        private async System.Threading.Tasks.Task<List<string>> TagImageWithCloudApi(string imagePath)
        {
            await System.Threading.Tasks.Task.Delay(500); // Simulate async call
            return new List<string> { "cloud", "tag", "example" };
        }

        // Placeholder for ML.NET tagging
        private async System.Threading.Tasks.Task<List<string>> TagImageWithMLNet(string imagePath)
        {
            // Load labels
            var labels = File.ReadAllLines(LabelsPath);

            // Create MLContext
            var mlContext = new MLContext();

            // Define input data
            var data = new List<ImageInput> { new ImageInput { ImagePath = imagePath } };
            var imageData = mlContext.Data.LoadFromEnumerable(data);

            // Get ONNX output column name programmatically
            string outputColumnName = GetOnnxOutputColumnName(ModelPath);

            // Define pipeline
            var pipeline = mlContext.Transforms.LoadImages(outputColumnName: "data", imageFolder: Path.GetDirectoryName(imagePath), inputColumnName: nameof(ImageInput.ImagePath))
                .Append(mlContext.Transforms.ResizeImages(outputColumnName: "data", imageWidth: ImageWidth, imageHeight: ImageHeight, inputColumnName: "data"))
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "data"))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    modelFile: ModelPath,
                    outputColumnNames: new[] { outputColumnName },
                    inputColumnNames: new[] { "data" }));

            // Fit and transform
            var model = pipeline.Fit(imageData);
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ImageInput, ImagePrediction>(model);
            var prediction = predictionEngine.Predict(new ImageInput { ImagePath = imagePath });

            // Get top 3 predictions
            var topK = prediction.PredictedLabels
                .Select((score, index) => new { Label = labels[index], Score = score })
                .OrderByDescending(x => x.Score)
                .Take(3)
                .Select(x => x.Label)
                .ToList();

            return topK;
        }

        private string GetOnnxOutputColumnName(string modelPath)
        {
            using var session = new InferenceSession(modelPath);
            // Return the first output column name
            return session.OutputMetadata.Keys.First();
        }

        public class ImageInput
        {
            public string ImagePath { get; set; }
        }

        public class ImagePrediction
        {
            // The output column name will be set dynamically at runtime
            [ColumnName(null)]
            public float[] PredictedLabels { get; set; }
        }

        // Placeholder for writing tags to image metadata
        private void SaveTagsToImage(string imagePath, List<string> tags)
        {
            // TODO: Implement metadata writing using MetadataExtractor or other library
        }
    }
}
