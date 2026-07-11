import asyncio
import logging
import signal
import sys
from typing import AsyncIterator, List, Optional, Tuple

import cv2
import grpc
import numpy as np
from grpc_health.v1 import health, health_pb2_grpc
from grpc_reflection.v1alpha import reflection
from PIL import Image
from sentence_transformers import SentenceTransformer

import image_loader as il

# Generated stubs — these are created during Docker build by protoc
import image_feature_extractor_pb2 as pb2
import image_feature_extractor_pb2_grpc as pb2_grpc

logger = logging.getLogger(__name__)

MODEL_NAME = "clip-ViT-B-32"

clip_model: SentenceTransformer
image_loader: il.ImageLoader


def _load_and_convert_image(image_url: str) -> Tuple[np.ndarray, Image.Image]:
    cv_img = image_loader.load(image_url)
    pil_img = Image.fromarray(cv2.cvtColor(cv_img, cv2.COLOR_BGR2RGB))
    return cv_img, pil_img


def _generate_embedding(pil_img: Image.Image) -> np.ndarray:
    return clip_model.encode(pil_img)


class ImageFeatureExtractorServicer(pb2_grpc.ImageFeatureExtractorServicer):
    async def ProcessImage(
        self, request: pb2.ProcessImageRequest, context: grpc.aio.ServicerContext
    ) -> pb2.ProcessImageResponse:
        try:
            cv_img, pil_img = await asyncio.to_thread(
                _load_and_convert_image, request.image_url
            )
        except Exception as e:
            await context.abort(grpc.StatusCode.INTERNAL, f"Failed to process image: {e}")

        response = pb2.ProcessImageResponse(
            image_url=request.image_url,
            model_name=MODEL_NAME,
            size=[pil_img.size[0], pil_img.size[1]],
        )

        if request.embedding:
            embedding = await asyncio.to_thread(_generate_embedding, pil_img)
            response.embedding.extend(embedding.tolist())

        return response

    async def ProcessImageBatch(
        self, request: pb2.ProcessImageBatchRequest, context: grpc.aio.ServicerContext
    ) -> AsyncIterator[pb2.ProcessImageResponse]:
        for image_url in request.image_urls:
            try:
                cv_img, pil_img = await asyncio.to_thread(
                    _load_and_convert_image, image_url
                )
            except Exception as e:
                logger.warning("Failed to load image %s: %s", image_url, e)
                continue

            response = pb2.ProcessImageResponse(
                image_url=image_url,
                model_name=MODEL_NAME,
                size=[pil_img.size[0], pil_img.size[1]],
            )

            if request.embedding:
                embedding = await asyncio.to_thread(_generate_embedding, pil_img)
                response.embedding.extend(embedding.tolist())

            yield response

    async def EmbedText(
        self, request: pb2.EmbedTextRequest, context: grpc.aio.ServicerContext
    ) -> pb2.EmbedTextResponse:
        embedding = await asyncio.to_thread(clip_model.encode, request.text)
        response = pb2.EmbedTextResponse()
        response.embedding.extend(embedding.tolist())
        return response

    async def GetModelName(
        self, request, context: grpc.aio.ServicerContext
    ) -> pb2.ModelNameResponse:
        return pb2.ModelNameResponse(model_name=MODEL_NAME)


async def serve() -> None:
    global clip_model, image_loader

    logging.basicConfig(level=logging.INFO, stream=sys.stdout)

    logger.info("Loading CLIP model...")
    clip_model = SentenceTransformer("sentence-transformers/clip-ViT-B-32")
    image_loader = il.ImageLoader()
    logger.info("Models loaded.")

    server = grpc.aio.server()

    pb2_grpc.add_ImageFeatureExtractorServicer_to_server(
        ImageFeatureExtractorServicer(), server
    )

    health_servicer = health.HealthServicer()
    health_pb2_grpc.add_HealthServicer_to_server(health_servicer, server)

    SERVICE_NAMES = (
        pb2.DESCRIPTOR.services_by_name["ml.v1.ImageFeatureExtractor"].full_name,
        health.SERVICE_NAME,
        reflection.SERVICE_NAME,
    )
    reflection.enable_server_reflection(SERVICE_NAMES, server)

    server.add_insecure_port("[::]:8080")
    await server.start()
    logger.info("gRPC server listening on [::]:8080")

    stop = asyncio.Event()

    def _signal_handler() -> None:
        stop.set()

    loop = asyncio.get_running_loop()
    for sig in (signal.SIGINT, signal.SIGTERM):
        loop.add_signal_handler(sig, _signal_handler)

    await stop.wait()
    logger.info("Shutting down gRPC server...")
    await server.graceful_shutdown()


if __name__ == "__main__":
    asyncio.run(serve())
