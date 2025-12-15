# Agent Guidelines

CRITICAL: When you encounter a file reference (e.g., [src/general.md]), use your Read tool to load it on a need-to-know basis. They're relevant to the SPECIFIC task at hand.

## Overview
ShitpostBot is a Discord bot with image repost detection, consisting of:
- **C# .NET Services**: Discord bot worker, domain models, infrastructure (PostgreSQL + EF Core)
- **Python ML Service**: TensorFlow-based image feature extraction API

## Quick Start
- **Run dev env**: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build`
- **C# build/test**: See [src/ShitpostBot/AGENTS.md](src/ShitpostBot/AGENTS.md)
- **Python ML service**: See [src/ShitpostBot.MlService/AGENTS.md](src/ShitpostBot.MlService/AGENTS.md)

## Project Structure
```
src/
├── ShitpostBot/              # C# .NET solution
│   ├── src/
│   └── test/
│
└── ShitpostBot.MlService/    # Python Flask API
    └── src/
```

## Technology Stack
- **C#**: .NET 10.0, EF Core, MediatR, MassTransit, DSharpPlus (Discord), Refit
- **Python**: Flask, TensorFlow 2.3, OpenCV, Pillow
- **Database**: PostgreSQL with pgvector extension
- **Testing**: NUnit, FluentAssertions, Testcontainers
- **DevOps**: Docker, Helm charts, GitHub Actions

## Development Workflow
1. Work on C# code? → See [src/ShitpostBot/AGENTS.md](src/ShitpostBot/AGENTS.md)
2. Work on ML service? → See [src/ShitpostBot.MlService/AGENTS.md](src/ShitpostBot.MlService/AGENTS.md)
3. Run full stack locally: Use docker-compose command above

## E2E Testing
- **Run E2E tests**: `./test/e2e/run-e2e-tests.sh` (from repo root)
- **Purpose**: Validates repost detection end-to-end with sample data
- **What it does**: Spins up local docker compose (webapi + services), runs HTTP tests
- **When to use**: After making substantial changes to repost detection or test infrastructure
- **Important**: Always run from repository root
