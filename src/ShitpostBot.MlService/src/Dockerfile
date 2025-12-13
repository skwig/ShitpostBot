FROM python:3.8-slim-bookworm
WORKDIR /app

RUN apt-get update && apt-get install -y curl ffmpeg libmagic1

ADD "requirements.txt" "."
RUN pip install -r requirements.txt
RUN curl https://storage.googleapis.com/tensorflow/keras-applications/inception_resnet_v2/inception_resnet_v2_weights_tf_dim_ordering_tf_kernels.h5 -o /app/inception_resnet_v2_weights_tf_dim_ordering_tf_kernels.h5

ADD "src" "src"

EXPOSE 5000
CMD ["python", "src/app.py"]
