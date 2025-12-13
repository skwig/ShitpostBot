import io

import requests
import tensorflow as tf
from PIL import Image


# Modified tf.keras.preprocessing.image.load_img to download from urls
def load_img_from_url(image_url, grayscale=False, color_mode='rgb', target_size=None):
    image_response = requests.get(url=image_url, stream=True)
    with io.BytesIO(image_response.content) as f:
        with Image.open(f) as img:

            if color_mode == 'grayscale':
                # if image is not already an 8-bit, 16-bit or 32-bit grayscale image
                # convert it to an 8-bit grayscale image.
                if img.mode not in ('L', 'I;16', 'I'):
                    img = img.convert('L')
            elif color_mode == 'rgba':
                if img.mode != 'RGBA':
                    img = img.convert('RGBA')
            elif color_mode == 'rgb':
                if img.mode != 'RGB':
                    img = img.convert('RGB')
            else:
                raise ValueError('color_mode must be "grayscale", "rgb", or "rgba"')

            width_height_tuple = (target_size[1], target_size[0])
            if img.size != width_height_tuple:
                img = img.resize(width_height_tuple, Image.NEAREST)

            return img


def fetch_image(image_url, target_size):
    img = load_img_from_url(image_url, target_size=target_size)
    return tf.keras.preprocessing.image.img_to_array(img)