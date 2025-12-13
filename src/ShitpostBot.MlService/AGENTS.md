# Agent Guidelines for ShitpostBot ML Service

## Build/Run Commands
- **Install dependencies**: `pip install -r requirements.txt`
- **Run locally**: `python src/app.py` or `flask run`
- **Build Docker image**: `docker build -f src/Dockerfile -t shitpostbot-ml-service .`
- **Run container**: `docker run -p 5000:5000 shitpostbot-ml-service`

## Architecture
- **Flask API**: Image feature extraction service using TensorFlow
- **Model**: InceptionResNetV2 for feature extraction (avg_pool layer output)
- **Endpoints**: `/images/features?image_url=<url>` - extracts features from image URL
- **Dependencies**: TensorFlow CPU, OpenCV, Flask, requests

## Project Structure
- **app.py**: Flask application, API routes, singleton services
- **image_feature_extractor.py**: TensorFlow model wrapper, feature extraction
- **image_loader.py**: Load images from URLs or files, handle videos/GIFs (first frame)
- **utils.py**: Image loading utilities, PIL/TensorFlow integration

## Code Style
- **Python version**: 3.x (compatible with TensorFlow 2.3.0)
- **Formatting**: Follow PEP 8 (4 spaces indentation)
- **Imports**: Standard library first, then third-party (tensorflow, cv2, flask, etc.), then local modules
- **Naming**: snake_case for functions/variables/modules, PascalCase for classes
- **Type hints**: Use type hints where helpful (e.g., `def __init__(self, model_path: str) -> None:`)
- **Classes**: Keep classes focused (ImageFeatureExtractor, ImageLoader)
- **Error handling**: Use try/except for HTTP requests, file operations
- **Image handling**: Support both URLs and file:// URIs, handle videos/GIFs by extracting first frame
- **Model loading**: Load model once as singleton in app.py
