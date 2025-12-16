FROM python:3.12-slim
EXPOSE 8080
HEALTHCHECK --interval=2s --timeout=1s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/healthz || exit 1

WORKDIR /app

RUN apt-get update && apt-get install -y \
    curl \
    libgomp1 \
    libglib2.0-0 \
    libmagic1 \
    ffmpeg \
    libgl1\
    libsm6 \
    libxext6 \
    libxrender-dev \
    build-essential \
    swig \
    tesseract-ocr \
    tesseract-ocr-eng \
    tesseract-ocr-ces \
    tesseract-ocr-slk \
    && rm -rf /var/lib/apt/lists/*


COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Pre-download models at build time
RUN python -c "from sentence_transformers import SentenceTransformer; SentenceTransformer('sentence-transformers/clip-ViT-B-32')"
RUN python -c "from paddleocr import PaddleOCR; PaddleOCR(use_angle_cls=False, lang='en')"
RUN python -c "from transformers import BlipProcessor, BlipForConditionalGeneration; BlipProcessor.from_pretrained('Salesforce/blip-image-captioning-base'); BlipForConditionalGeneration.from_pretrained('Salesforce/blip-image-captioning-base')"

COPY src/*.py .

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8080", "--workers", "1"]
