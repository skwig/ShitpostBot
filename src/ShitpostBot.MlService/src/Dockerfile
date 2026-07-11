FROM python:3.12-slim
EXPOSE 8080

# Install wget for downloading grpc_health_probe
RUN apt-get update && apt-get install -y \
    wget \
    && rm -rf /var/lib/apt/lists/*

# Install grpc_health_probe
RUN GRPC_HEALTH_PROBE_VERSION=v0.4.38 && \
    wget -qO/bin/grpc_health_probe https://github.com/grpc-ecosystem/grpc-health-probe/releases/download/${GRPC_HEALTH_PROBE_VERSION}/grpc_health_probe-linux-amd64 && \
    chmod +x /bin/grpc_health_probe

HEALTHCHECK --interval=2s --timeout=1s --start-period=10s --retries=3 \
    CMD grpc_health_probe -addr=localhost:8080 || exit 1

WORKDIR /app

RUN apt-get update && apt-get install -y \
    libmagic1 \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt constraints.txt .
RUN pip install \
    --extra-index-url https://download.pytorch.org/whl/cpu \
    --constraint constraints.txt \
    -r requirements.txt

# Pre-download models at build time
RUN python -c "from sentence_transformers import SentenceTransformer; SentenceTransformer('sentence-transformers/clip-ViT-B-32')"

# Copy proto definition and generate Python stubs
COPY src/protos ./protos
RUN python -m grpc_tools.protoc \
    -I./protos \
    --python_out=. \
    --grpc_python_out=. \
    ./protos/image_feature_extractor.proto

# Copy application source
COPY src/*.py .

CMD ["python", "server.py"]
