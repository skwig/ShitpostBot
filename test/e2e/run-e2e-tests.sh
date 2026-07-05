#!/usr/bin/env sh

# E2E Test Runner for ShitpostBot Repost Detection
#
# IMPORTANT: This script MUST be run from the repository root:
#   ./test/e2e/run-e2e-tests.sh
#

docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml down
docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build --wait webapi
ijhttp --no-progress test/e2e/e2e-tests.http
