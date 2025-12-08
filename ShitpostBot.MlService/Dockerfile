#alpine doesnt support (?) tensorflow
FROM python:3.14-slim-bookworm
WORKDIR /app

RUN apt-get update && apt-get install -y curl ffmpeg libmagic1

ADD requirements.txt /app
RUN pip install -r requirements.txt \
    && curl https://storage.googleapis.com/tensorflow/keras-applications/inception_resnet_v2/inception_resnet_v2_weights_tf_dim_ordering_tf_kernels.h5 -o /app/inception_resnet_v2_weights_tf_dim_ordering_tf_kernels.h5

ADD . /app
EXPOSE 5000
CMD ["python","app.py"]
