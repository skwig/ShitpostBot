from flask import Flask, json
from flask import request
from flask import escape

import urllib.parse
import utils
import image_feature_extractor as ife

app = Flask(__name__)

# Singleton scope
image_feature_extractor = ife.ImageFeatureExtractor()


@app.route('/')
def home():
    return ""


@app.route('/images/features')
def extract_image_features():
    image_url_raw = urllib.parse.unquote(request.args.get('image_url'))
    image_url = escape(image_url_raw)

    image = utils.fetch_image(image_url, target_size=(299, 299))
    image_features = image_feature_extractor.extract_features(image)

    return json.dumps({
        "image_url": image_url,
        "image_features": image_features.tolist()
    })


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
