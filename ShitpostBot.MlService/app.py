from flask import Flask, json
from flask import request
from flask import escape

import urllib.parse
import utils
import image_feature_extractor as ife
import image_loader as il

app = Flask(__name__)

# Singleton scope
image_feature_extractor = ife.ImageFeatureExtractor("/app/inception_resnet_v2_weights_tf_dim_ordering_tf_kernels.h5")
image_loader = il.ImageLoader()


@app.route("/")
def home():
    return ""


@app.route("/images/features")
def extract_image_features():
    original_uri = request.args.get("image_url")
    if original_uri is None:
        return "", 400

    uri = urllib.parse.unquote(original_uri)

    img = image_loader.load(uri)

    if img is None:
        return "", 400

    return json.dumps({
        "image_url": original_uri,
        "image_features": image_feature_extractor.extract_features(img).tolist(),
    })


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000)
