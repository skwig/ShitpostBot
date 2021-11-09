import numpy as np
import tensorflow as tf


class ImageFeatureExtractor:

    def __init__(self) -> None:
        base_model = tf.keras.applications.inception_resnet_v2.InceptionResNetV2(weights='imagenet', include_top=True)
        self.model = tf.keras.Model(inputs=base_model.input, outputs=base_model.get_layer('avg_pool').output)

    def extract_features(self, image):
        x = np.expand_dims(image, axis=0)
        x = tf.keras.applications.inception_resnet_v2.preprocess_input(x)

        features = self.model.predict(x)
        features = features[0]
        return features
