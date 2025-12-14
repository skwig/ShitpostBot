FROM python:3.8-slim-bookworm
EXPOSE 5000
HEALTHCHECK --interval=2s --timeout=1s --start-period=0s --retries=3 \
    CMD curl --fail http://localhost:5000/ || exit 1

WORKDIR /app

RUN apt-get update && apt-get install -y curl ffmpeg libmagic1

ADD "requirements.txt" "."
RUN pip install -r requirements.txt
RUN curl https://storage.googleapis.com/tensorflow/keras-applications/inception_resnet_v2/inception_resnet_v2_weights_tf_dim_ordering_tf_kernels.h5 -o /app/inception_resnet_v2_weights_tf_dim_ordering_tf_kernels.h5

ADD "src" "src"

CMD ["python", "src/app.py"]
