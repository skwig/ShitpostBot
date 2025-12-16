from fastapi import FastAPI, UploadFile, File, Depends, Query
from pydantic import BaseModel, Field
from typing import Optional, List, Tuple
from sentence_transformers import SentenceTransformer
from transformers import BlipProcessor, BlipForConditionalGeneration
from paddleocr import PaddleOCR
import pytesseract
import numpy as np
from PIL import Image, ImageOps
import io
import cv2
import os
import multiprocessing
from concurrent.futures import ThreadPoolExecutor

import image_loader as il

app = FastAPI()

# Load models once at startup (crucial for performance)
clip_model = SentenceTransformer('sentence-transformers/clip-ViT-B-32')
ocr_engine = PaddleOCR(use_angle_cls=False, lang='en')

blip_processor = BlipProcessor.from_pretrained("Salesforce/blip-image-captioning-base")
blip_model = BlipForConditionalGeneration.from_pretrained("Salesforce/blip-image-captioning-base")

image_loader = il.ImageLoader()

tesseract_workers = multiprocessing.cpu_count()

def _load_and_convert_image(image_url: str) -> Tuple[np.ndarray, Image.Image]:
    """Load image from URL and return both OpenCV and PIL formats"""
    cv_img = image_loader.load(image_url)
    pil_img = Image.fromarray(cv2.cvtColor(cv_img, cv2.COLOR_BGR2RGB))
    return cv_img, pil_img

def _generate_embedding(pil_img: Image.Image) -> np.ndarray:
    """Generate CLIP embedding from PIL image"""
    return clip_model.encode(pil_img)

def _generate_embeddings_batch(pil_images: List[Image.Image]) -> np.ndarray:
    """Generate CLIP embeddings for multiple images in a single batch"""
    return clip_model.encode(pil_images)

def _extract_ocr_text_paddleocr(cv_img: np.ndarray) -> Tuple[str, float]:
    """Extract text from OpenCV image using PaddleOCR"""
    results = ocr_engine.ocr(cv_img)
    result = results[0]
    
    texts = result["rec_texts"]
    scores = result["rec_scores"]
    
    text = " ".join(texts)
    confidence = float(np.mean(scores)) if scores else 0.0
    
    return text, confidence


def _extract_ocr_text_tesseract(pil_img: Image.Image) -> Tuple[str, float]:
    """Extract text from PIL image using enhanced Tesseract OCR with preprocessing"""
    
    psm_mode = 3
    config = f'--psm {psm_mode}'
    
    data = pytesseract.image_to_data(pil_img, lang='eng+ces+slk',
                                     config=config, output_type=pytesseract.Output.DICT)
    
    texts = []
    confidences = []
    
    for i, conf in enumerate(data['conf']):
        if conf != -1:  # -1 means no text detected
            word = data['text'][i].strip()
            if word:
                texts.append(word)
                confidences.append(float(conf))
    
    text = ' '.join(texts)
    avg_confidence = float(np.mean(confidences)) / 100.0 if confidences else 0.0
    
    return text, avg_confidence

def _extract_ocr_texts_batch(cv_images: List[np.ndarray], pil_images: List[Image.Image], use_tesseract: bool = False) -> List[Tuple[str, float, str]]:
    """Extract text from multiple images with optional Tesseract support
    
    Tesseract uses parallel processing with ThreadPoolExecutor for 3+ images.
    PaddleOCR remains sequential (will be removed soon).
    
    Returns: List of (text, confidence, engine_name) tuples
    """
    # PaddleOCR keeps sequential processing (will be removed soon)
    if not use_tesseract:
        results = []
        for cv_img in cv_images:
            text, confidence = _extract_ocr_text_paddleocr(cv_img)
            results.append((text, confidence, "paddleocr"))
        return results
    
    num_images = len(pil_images)
    
    # For very small batches, sequential is faster due to threading overhead
    if num_images <= 2:
        results = []
        for pil_img in pil_images:
            text, confidence = _extract_ocr_text_tesseract(pil_img)
            results.append((text, confidence, "tesseract"))
        return results
    
    max_workers = min(tesseract_workers, num_images)
    
    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = [executor.submit(_extract_ocr_text_tesseract, pil_img) for pil_img in pil_images]
        results = []
        for future in futures:
            text, confidence = future.result()
            results.append((text, confidence, "tesseract"))
    
    return results

def _generate_caption(pil_img: Image.Image) -> str:
    """Generate natural language caption for PIL image"""
    inputs = blip_processor(pil_img, return_tensors="pt")
    output = blip_model.generate(**inputs, max_length=50)
    caption = blip_processor.decode(output[0], skip_special_tokens=True)
    return caption

def _generate_captions_batch(pil_images: List[Image.Image]) -> List[str]:
    """Generate captions for multiple images in a single batch"""
    if not pil_images:
        return []
    
    inputs = blip_processor(pil_images, return_tensors="pt", padding=True)
    outputs = blip_model.generate(**inputs, max_length=50)
    captions = [blip_processor.decode(output, skip_special_tokens=True) for output in outputs]
    return captions

@app.get("/healthz")
def health():
    return {"status": "ok", "models": ["clip", "ocr", "blip"]}

class TextEmbedRequest(BaseModel):
    text: str

@app.post("/embed/text")
def embed_text(request: TextEmbedRequest):
    """Text to embedding for semantic search"""
    embedding = clip_model.encode(request.text)
    return {
        "embedding": embedding.tolist(),
    }

class ProcessImageRequest(BaseModel):
    image_url: str = Field()
    embedding: bool = True
    caption: bool = True
    ocr: bool = True
    use_tesseract: bool = True

class ProcessImageResponse(BaseModel):
    image_url: str
    embedding: Optional[List[float]] = None
    caption: Optional[str] = None
    ocr: Optional[str] = None
    ocr_confidence: Optional[float] = None

@app.post("/process/image")
async def process_image(request: ProcessImageRequest):
    """Process image with optional embedding, captioning, and OCR extraction"""
    cv_img, pil_img = _load_and_convert_image(request.image_url)
    
    result = {
        "size": list(pil_img.size),
    }
    
    if request.embedding:
        embedding = _generate_embedding(pil_img)
        result["embedding"] = embedding.tolist()
    
    if request.caption:
        caption = _generate_caption(pil_img)
        result["caption"] = caption
    
    if request.ocr:
        
        if request.use_tesseract:
            text, confidence = _extract_ocr_text_tesseract(pil_img)
            ocr_engine_used = "tesseract"
        else:
            text, confidence = _extract_ocr_text(cv_img)
            ocr_engine_used = "paddleocr"
        
        result["ocr"] = text
        result["ocr_confidence"] = confidence
        result["ocr_engine"] = ocr_engine_used
    
    return result

class ProcessImageBatchRequest(BaseModel):
    image_urls: List[str] = Field(..., min_length=1, max_length=100)
    embedding: bool = True
    caption: bool = True
    ocr: bool = True
    use_tesseract: bool = True

class ProcessImageBatchItem(BaseModel):
    image_url: str
    embedding: Optional[List[float]] = None
    caption: Optional[str] = None
    ocr: Optional[str] = None
    ocr_confidence: Optional[float] = None
    ocr_engine: Optional[str] = None

@app.post("/process/image/batch")
async def process_image_batch(request: ProcessImageBatchRequest):
    """Batch process multiple images with optional embedding, captioning, and OCR extraction"""
    
    # Load all images
    cv_images = []
    pil_images = []
    image_urls = []
    
    for image_url in request.image_urls:
        try:
            cv_img, pil_img = _load_and_convert_image(image_url)
            cv_images.append(cv_img)
            pil_images.append(pil_img)
            image_urls.append(image_url)
        except Exception as e:
            # Skip failed images but could also return errors
            print(f"Failed to load image {image_url}: {e}")
            continue
    
    if not pil_images:
        return {"results": [], "count": 0}
    
    embeddings = None
    if request.embedding:
        embeddings_array = _generate_embeddings_batch(pil_images)
        embeddings = [emb.tolist() for emb in embeddings_array]
    
    captions = None
    if request.caption:
        captions = _generate_captions_batch(pil_images)
    
    ocr_results = None
    if request.ocr:
        ocr_results = _extract_ocr_texts_batch(cv_images, pil_images, request.use_tesseract)
    
    results = []
    for i, (pil_img, image_url) in enumerate(zip(pil_images, image_urls)):
        result = {
            "image_url": image_url,
            "size": list(pil_img.size),
            "format": pil_img.format if pil_img.format else "unknown"
        }
        
        if embeddings:
            result["embedding"] = embeddings[i]
        
        if captions:
            result["caption"] = captions[i]
        
        if ocr_results:
            result["ocr"] = ocr_results[i][0]
            result["ocr_confidence"] = ocr_results[i][1]
            result["ocr_engine"] = ocr_results[i][2]
        
        results.append(result)
    
    return {
        "results": results
    }
