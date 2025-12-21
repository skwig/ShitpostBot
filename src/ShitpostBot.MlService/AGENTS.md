# Agent Guidelines for ShitpostBot ML Service

## Build/Run Commands
- **Install dependencies**: `pip install -r requirements.txt`
- **Run locally**: `cd src && uvicorn app:app --reload --port 8080`
- **Build Docker image**: `docker build -f src/Dockerfile -t shitpostbot-ml-service .`
- **Run container**: `docker run -p 8080:8080 shitpostbot-ml-service`

## Architecture
- **FastAPI**: Image processing service with CLIP embeddings, BLIP captioning, and OCR
- **Models**: 
  - CLIP-ViT-B-32 for semantic image embeddings (sentence-transformers)
  - BLIP (Salesforce/blip-image-captioning-base) for natural language image descriptions
  - Tesseract OCR (primary) and PaddleOCR for text extraction from images
- **Endpoints**: 
  - POST `/process/image` - Process single image with configurable features (embedding/caption/ocr)
  - POST `/process/image/batch` - Batch process multiple images efficiently
  - POST `/embed/text` - Generate text embeddings for semantic search
  - GET `/healthz` - Health check
- **Dependencies**: FastAPI, Uvicorn, sentence-transformers, transformers, PaddleOCR, Tesseract, OpenCV, Pillow

## Project Structure
- **app.py**: FastAPI application, API routes, model initialization, and processing endpoints
- **image_loader.py**: Load images from URLs or files, handle videos/GIFs (first frame)
- **utils.py**: Image processing utilities and helper functions

## Code Style
- **Python version**: 3.12 (compatible with FastAPI 0.124.0, transformers 4.57.3)
- **Formatting**: Follow PEP 8 (4 spaces indentation)
- **Imports**: Standard library first, then third-party (fastapi, sentence_transformers, transformers, paddleocr, cv2, etc.), then local modules
- **Naming**: snake_case for functions/variables/modules, PascalCase for classes
- **Type hints**: Use type hints where helpful (e.g., `def __init__(self, model_path: str) -> None:`)
- **Classes**: Keep classes focused (ImageLoader for image loading)
- **Error handling**: Use try/except for HTTP requests, file operations
- **Image handling**: Support both URLs and file:// URIs, handle videos/GIFs by extracting first frame
- **Model loading**: Load model once as singleton in app.py
