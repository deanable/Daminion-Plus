# ImageTagger Plus - Model Management System

## Overview

The ImageTagger Plus application now supports a streamlined workflow for acquiring, converting, and managing both ONNX and PyTorch models for image tagging.

---

## New PyTorch Model Workflow

### 1. Python Requirement
- **Python 3.8+** must be installed on your system.
- The app checks for Python and required packages before allowing PyTorch model conversion.
- If Python is missing, you will be prompted to install it.

### 2. Downloading PyTorch Models
- Only common, well-supported architectures (e.g., ResNet, EfficientNet, MobileNet) are available for download.
- Models requiring extra configuration are not shown.
- Downloaded PyTorch models appear in the model management UI with type **PyTorch** and status **NotConverted**.

### 3. Converting to ONNX
- A **Convert to ONNX** button appears next to each unconverted PyTorch model.
- Click the button to start the conversion process.
- The app will:
  - Check for Python and required packages
  - Run a conversion script to generate an ONNX model and label file
  - Show progress and error messages in the UI
- After successful conversion, the model type changes to **ONNX** and status to **Converted**.
- If conversion fails, the status is set to **Failed** and errors are displayed.

### 4. Model Status Tracking
- The model management UI now shows **Type** (ONNX, PyTorch) and **Conversion Status** (NotConverted, Converting, Converted, Failed) for each model.
- Only ONNX models with valid label files are available for tagging.
- You can validate models, enable/disable them, and set the default model as before.

### 5. Error and Progress Reporting
- All steps (download, conversion, validation) provide clear progress and error messages in the UI.
- The log file contains detailed information for troubleshooting.

---

## Example Workflow

1. **Download a PyTorch model** (e.g., ResNet-50) from the repository browser.
2. The model appears in the installed models list as **PyTorch / NotConverted**.
3. Click **Convert to ONNX** next to the model.
4. The app runs the conversion and updates the status to **ONNX / Converted** if successful.
5. The model is now available for image tagging.

---

## Troubleshooting
- If conversion fails, check the error message and log file.
- Ensure Python and required packages are installed.
- Only supported architectures can be converted automatically.

---

## Legacy ONNX Model Workflow

- ONNX models can still be downloaded and used as before.
- The app validates ONNX and label files before making them available for tagging.

---

## UI Reference
- **Type**: Shows if the model is ONNX or PyTorch.
- **Conversion**: Shows conversion status (NotConverted, Converting, Converted, Failed).
- **Convert to ONNX**: Button appears only for unconverted PyTorch models.

---

For more details, see the in-app help or contact support. 