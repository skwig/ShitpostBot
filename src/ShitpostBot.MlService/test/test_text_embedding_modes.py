import os
import sys

from fastapi.testclient import TestClient

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "../src"))

import app


class FakeModel:
    def __init__(self):
        self.inputs = []

    def encode(self, text):
        self.inputs.append(text)
        return [0.1, 0.2, 0.3]


def test_embed_conversation_query_mode_uses_e5_query_prefix(monkeypatch):
    fake_clip = FakeModel()
    fake_e5 = FakeModel()
    monkeypatch.setattr(app, "clip_model", fake_clip)
    monkeypatch.setattr(app, "conversation_text_model", fake_e5)

    client = TestClient(app.app)

    response = client.post(
        "/embed/conversation", json={"text": "gta5 discussion", "mode": "query"}
    )

    assert response.status_code == 200
    assert response.json() == {"embedding": [0.1, 0.2, 0.3]}
    assert fake_e5.inputs == ["query: gta5 discussion"]
    assert fake_clip.inputs == []


def test_embed_conversation_passage_mode_uses_e5_passage_prefix(monkeypatch):
    fake_clip = FakeModel()
    fake_e5 = FakeModel()
    monkeypatch.setattr(app, "clip_model", fake_clip)
    monkeypatch.setattr(app, "conversation_text_model", fake_e5)

    client = TestClient(app.app)

    response = client.post(
        "/embed/conversation", json={"text": "petr: dame gta", "mode": "passage"}
    )

    assert response.status_code == 200
    assert fake_e5.inputs == ["passage: petr: dame gta"]
    assert fake_clip.inputs == []


def test_embed_text_keeps_clip_behavior(monkeypatch):
    fake_clip = FakeModel()
    fake_e5 = FakeModel()
    monkeypatch.setattr(app, "clip_model", fake_clip)
    monkeypatch.setattr(app, "conversation_text_model", fake_e5)

    client = TestClient(app.app)

    response = client.post("/embed/text", json={"text": "cat image"})

    assert response.status_code == 200
    assert fake_clip.inputs == ["cat image"]
    assert fake_e5.inputs == []


def test_embed_conversation_rejects_unknown_mode(monkeypatch):
    monkeypatch.setattr(app, "clip_model", FakeModel())
    monkeypatch.setattr(app, "conversation_text_model", FakeModel())
    client = TestClient(app.app)

    response = client.post("/embed/conversation", json={"text": "abc", "mode": "bad"})

    assert response.status_code == 422
