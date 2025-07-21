using ImageTagger.Core.Interfaces;
using ImageTagger.Core.Models;

namespace ImageTagger.Services;

public class ModelConversionService
{
    private readonly ILoggingService _loggingService;
    private readonly string _modelsDirectory;
    private readonly string _pythonScriptsDirectory;

    public ModelConversionService(ILoggingService loggingService, string modelsDirectory = "models")
    {
        _loggingService = loggingService;
        _modelsDirectory = modelsDirectory;
        _pythonScriptsDirectory = Path.Combine(modelsDirectory, "conversion_scripts");
        
        // Ensure conversion scripts directory exists
        if (!Directory.Exists(_pythonScriptsDirectory))
        {
            Directory.CreateDirectory(_pythonScriptsDirectory);
        }
    }

    public async Task<bool> ConvertPyTorchToOnnxAsync(string modelId, string pytorchModelPath, string? customOutputPath = null)
    {
        try
        {
            _loggingService.Log($"Starting PyTorch to ONNX conversion for model: {modelId}");
            
            var outputPath = customOutputPath ?? Path.Combine(_modelsDirectory, modelId.Replace("/", "-"));
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // Create conversion script
            var scriptPath = await CreateConversionScriptAsync(modelId, pytorchModelPath, outputPath);
            
            // Execute conversion
            var success = await ExecuteConversionScriptAsync(scriptPath);
            
            // Validate ONNX and label files after conversion
            var onnxPath = Path.Combine(outputPath, "model.onnx");
            var labelsPath = Path.Combine(outputPath, "labels.txt");
            var validationSuccess = success && await ValidateOnnxAndLabelsAsync(onnxPath, labelsPath);

            if (validationSuccess)
            {
                _loggingService.Log($"Successfully converted {modelId} to ONNX format and validated files");
                return true;
            }
            else
            {
                if (!File.Exists(onnxPath))
                    _loggingService.Log($"ONNX file missing after conversion: {onnxPath}", LogLevel.Error);
                if (!File.Exists(labelsPath))
                    _loggingService.Log($"Labels file missing after conversion: {labelsPath}", LogLevel.Error);
                _loggingService.Log($"Failed to validate ONNX or label files for {modelId}", LogLevel.Warning);
                return false;
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"ConvertPyTorchToOnnxAsync for {modelId}");
            return false;
        }
    }

    private async Task<string> CreateConversionScriptAsync(string modelId, string pytorchModelPath, string outputPath)
    {
        var scriptPath = Path.Combine(_pythonScriptsDirectory, $"convert_{modelId.Replace("/", "_")}.py");
        
        var script = $@"
import torch
import torchvision.transforms as transforms
from transformers import AutoModelForImageClassification, AutoImageProcessor
import onnx
import onnxruntime
import os
import sys

def convert_model_to_onnx(model_id, output_path):
    try:
        print(f'Loading model {{model_id}}...')
        
        # Load model and processor
        model = AutoModelForImageClassification.from_pretrained(model_id)
        processor = AutoImageProcessor.from_pretrained(model_id)
        
        # Set model to evaluation mode
        model.eval()
        
        # Create dummy input (adjust size based on model requirements)
        dummy_input = torch.randn(1, 3, 224, 224)
        
        # Export to ONNX
        onnx_path = os.path.join(output_path, 'model.onnx')
        torch.onnx.export(
            model,
            dummy_input,
            onnx_path,
            export_params=True,
            opset_version=11,
            do_constant_folding=True,
            input_names=['input'],
            output_names=['output'],
            dynamic_axes={{'input': {{0: 'batch_size'}},
                          'output': {{0: 'batch_size'}}}}
        )
        
        print(f'Model converted successfully to {{onnx_path}}')
        
        # Create labels file
        labels_path = os.path.join(output_path, 'labels.txt')
        if hasattr(model.config, 'id2label') and model.config.id2label:
            labels = [model.config.id2label[i] for i in range(len(model.config.id2label))]
        else:
            # Create generic labels
            labels = [f'class_{{i}}' for i in range(1000)]
        
        with open(labels_path, 'w') as f:
            for label in labels:
                f.write(f'{{label}}\\n')
        
        print(f'Labels file created: {{labels_path}}')
        return True
        
    except Exception as e:
        print(f'Error converting model: {{e}}')
        return False

if __name__ == '__main__':
    model_id = '{modelId}'
    output_path = '{outputPath}'
    success = convert_model_to_onnx(model_id, output_path)
    sys.exit(0 if success else 1)
";

        await File.WriteAllTextAsync(scriptPath, script);
        _loggingService.LogVerbose($"Created conversion script: {scriptPath}");
        
        return scriptPath;
    }

    private async Task<bool> ExecuteConversionScriptAsync(string scriptPath)
    {
        try
        {
            _loggingService.LogVerbose($"Executing conversion script: {scriptPath}");
            
            // Check if Python is available
            var pythonPath = await FindPythonPathAsync();
            if (string.IsNullOrEmpty(pythonPath))
            {
                _loggingService.Log("Python not found. Please install Python and required packages.", LogLevel.Warning);
                return false;
            }

            // Check if required packages are installed
            if (!await CheckRequiredPackagesAsync(pythonPath))
            {
                _loggingService.Log("Required Python packages not found. Installing...", LogLevel.Warning);
                await InstallRequiredPackagesAsync(pythonPath);
            }

            // Execute the script
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = scriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new System.Diagnostics.Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(output))
            {
                _loggingService.LogVerbose($"Conversion output: {output}");
            }

            if (!string.IsNullOrEmpty(error))
            {
                _loggingService.Log($"Conversion errors: {error}", LogLevel.Warning);
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "ExecuteConversionScriptAsync");
            return false;
        }
    }

    private async Task<bool> ValidateOnnxAndLabelsAsync(string onnxPath, string labelsPath)
    {
        try
        {
            if (!File.Exists(onnxPath) || !File.Exists(labelsPath))
                return false;

            // Try to load ONNX model
            try
            {
                using var session = new Microsoft.ML.OnnxRuntime.InferenceSession(onnxPath);
                var inputMetadata = session.InputMetadata;
                var outputMetadata = session.OutputMetadata;
                if (inputMetadata.Count == 0 || outputMetadata.Count == 0)
                {
                    _loggingService.Log($"ONNX model has no inputs or outputs: {onnxPath}", LogLevel.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException(ex, $"ONNX validation failed: {onnxPath}");
                return false;
            }

            // Check labels file is not empty
            var labels = await File.ReadAllLinesAsync(labelsPath);
            if (labels.Length == 0)
            {
                _loggingService.Log($"Labels file is empty: {labelsPath}", LogLevel.Error);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "ValidateOnnxAndLabelsAsync");
            return false;
        }
    }

    private async Task<string> FindPythonPathAsync()
    {
        var possiblePaths = new[]
        {
            "python",
            "python3",
            "python.exe",
            "python3.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python39", "python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python310", "python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python311", "python.exe")
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
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    _loggingService.LogVerbose($"Found Python at: {path}");
                    return path;
                }
            }
            catch
            {
                // Continue to next path
            }
        }

        return string.Empty;
    }

    private async Task<bool> CheckRequiredPackagesAsync(string pythonPath)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = "-c \"import torch, transformers, onnx, onnxruntime\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new System.Diagnostics.Process { StartInfo = startInfo };
            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task InstallRequiredPackagesAsync(string pythonPath)
    {
        try
        {
            var packages = new[] { "torch", "torchvision", "transformers", "onnx", "onnxruntime" };
            
            foreach (var package in packages)
            {
                _loggingService.LogVerbose($"Installing {package}...");
                
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"-m pip install {package}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new System.Diagnostics.Process { StartInfo = startInfo };
                process.Start();
                await process.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "InstallRequiredPackagesAsync");
        }
    }

    public async Task<bool> IsConversionSupportedAsync(string modelId)
    {
        // Check if the model is likely to be convertible
        // This is a basic check - in practice, you'd want more sophisticated detection
        var supportedPatterns = new[]
        {
            "resnet", "mobilenet", "efficientnet", "vit", "swin", "convnext",
            "densenet", "inception", "alexnet", "vgg", "googlenet"
        };

        return supportedPatterns.Any(pattern => 
            modelId.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
} 