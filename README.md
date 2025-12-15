# ShitpostBot

## Helm Chart

Install ShitpostBot using Helm:

```bash
# Add the Helm repository
helm repo add shitpostbot https://skwig.github.io/ShitpostBot/
helm repo update

# Install the chart
helm install my-shitpostbot shitpostbot/shitpostbot --namespace shitpostbot --create-namespace
```

See the [Helm repository](https://skwig.github.io/ShitpostBot/) for more details.

## Run dev env
```shell
docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build
```

## Test API

For local development and testing without Discord, use WebApi:

```bash
# Start all services including WebApi
docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build

# WebApi available at http://localhost:5001

# Test repost detection
curl -X POST http://localhost:5001/test/image-message \
  -H "Content-Type: application/json" \
  -d '{"imageUrl": "https://example.com/image.jpg"}'

# List available fixtures
curl http://localhost:5001/test/fixtures
```

See [Test API Design](docs/plans/2024-12-14-test-api-design.md) for details.

## E2E Testing

For comprehensive end-to-end validation of repost detection:

```bash
# Run full E2E test suite (from repository root)
./test/e2e/run-e2e-tests.sh
```

This script:
- Spins up local docker compose environment (webapi, ml-service, database)
- Runs automated E2E tests with sample data
- Validates repost detection behavior across multiple scenarios

**Important**: Always run from repository root.

**Use this after making substantial changes to:**
- Repost detection logic
- Image feature extraction
- Test infrastructure