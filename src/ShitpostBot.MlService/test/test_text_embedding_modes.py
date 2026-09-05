import os
import sys

from fastapi.testclient import TestClient

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "../src"))

import app


class FakeModel:
    def __init__(self, max_seq_length=5):
        self.inputs = []
        self.max_seq_length = max_seq_length
        self.tokenizer = FakeTokenizer()

    def encode(self, text, **kwargs):
        self.inputs.append(text)
        return [0.1, 0.2, 0.3]


class FakeTokenizer:
    def __call__(self, text, add_special_tokens=True, truncation=False):
        tokens = text.split()
        return {"input_ids": list(range(len(tokens)))}


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
    assert response.json() == {
        "embedding": [0.1, 0.2, 0.3],
        "token_count": 3,
        "max_token_count": 5,
        "truncated": False,
    }
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


def test_embed_conversation_returns_truncated_true_when_over_token_limit(monkeypatch):
    fake_clip = FakeModel()
    fake_e5 = FakeModel(max_seq_length=3)
    monkeypatch.setattr(app, "clip_model", fake_clip)
    monkeypatch.setattr(app, "conversation_text_model", fake_e5)

    client = TestClient(app.app)

    response = client.post(
        "/embed/conversation",
        json={"text": "one two three four", "mode": "passage"},
    )

    assert response.status_code == 200
    assert response.json()["token_count"] == 5
    assert response.json()["max_token_count"] == 3
    assert response.json()["truncated"] is True


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
