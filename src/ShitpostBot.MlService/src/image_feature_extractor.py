import numpy as np
import tensorflow as tf
import cv2


class ImageFeatureExtractor:

    def __init__(self, model_path: str) -> None:
        base_model = tf.keras.applications.inception_resnet_v2.InceptionResNetV2(weights=model_path, include_top=True)
        self.model = tf.keras.Model(inputs=base_model.input, outputs=base_model.get_layer('avg_pool').output)
        self.input_image_resolution = (self.model.input.shape[1], self.model.input.shape[2])

    def extract_features(self, image):
        resized_image = cv2.resize(image, self.input_image_resolution, cv2.INTER_NEAREST)
        
        x = np.expand_dims(resized_image, axis=0)
        x = tf.keras.applications.inception_resnet_v2.preprocess_input(x)

        features = self.model.predict(x)
        features = features[0]
        return features
