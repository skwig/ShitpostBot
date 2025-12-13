from urllib.parse import urlparse

import os
import cv2
import numpy as np
import requests
import magic


class ImageLoader:

    def __init__(self) -> None:
        pass

    def load(self, uri: str):
        parsed_uri = urlparse(uri)

        if parsed_uri.scheme == "file":
            path = os.path.abspath(os.path.join(parsed_uri.netloc, parsed_uri.path))
            mime = magic.from_file(path, mime=True)

            if mime.startswith("video") or mime == "image/gif":
                success, first_frame = cv2.VideoCapture(path).read()
                return first_frame
            else:
                return cv2.imread(path)
        else:
            response = requests.get(uri, stream=True)
            response.raise_for_status()

            mime = response.headers["Content-Type"]

            if mime.startswith("video") or mime == "image/gif":
                success, first_frame = cv2.VideoCapture(uri).read()
                return first_frame
            else:
                np_image = np.asarray(bytearray(response.raw.read()), dtype="uint8")
                return cv2.imdecode(np_image, cv2.IMREAD_COLOR)
