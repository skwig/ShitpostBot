import pytest
from fastapi.testclient import TestClient
from unittest.mock import patch, MagicMock
import requests

import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '../src'))

from app import app

client = TestClient(app)


def test_process_image_returns_404_when_image_not_found():
    """Test that 404 from image URL returns 404 response"""
    
    # Mock requests.get to return 404
    mock_response = MagicMock()
    mock_response.status_code = 404
    mock_response.raise_for_status.side_effect = requests.exceptions.HTTPError(response=mock_response)
    
    with patch('image_loader.requests.get', return_value=mock_response):
        response = client.post(
            "/process/image",
            json={
                "image_url": "https://example.com/deleted.jpg",
                "embedding": True,
                "caption": False,
                "ocr": False
            }
        )
    
    assert response.status_code == 404
    assert "Failed to download image" in response.json()["detail"]


def test_process_image_returns_500_for_other_errors():
    """Test that non-HTTP errors return 500"""
    
    with patch('image_loader.requests.get', side_effect=Exception("Network error")):
        response = client.post(
            "/process/image",
            json={
                "image_url": "https://example.com/image.jpg",
                "embedding": True,
                "caption": False,
                "ocr": False
            }
        )
    
    assert response.status_code == 500
    assert "Failed to process image" in response.json()["detail"]
