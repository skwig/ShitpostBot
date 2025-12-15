#!/usr/bin/env sh
#
# E2E Test Runner for ShitpostBot Repost Detection
#
# This script runs the full end-to-end test suite for repost detection.
# It spins up the necessary services via docker compose and executes
# automated HTTP tests.
#
# IMPORTANT: This script MUST be run from the repository root:
#   ./test/e2e/run-e2e-tests.sh
#
# What it tests:
# - Scenario 1: Posting unrelated images (should not trigger repost)
# - Scenario 2: Reposting downscaled images (should detect repost)
# - Scenario 3: Reposting in different formats (should detect repost)
#
# Usage:
#   ./test/e2e/run-e2e-tests.sh    # Run from repo root
#
# The script will:
# 1. Stop any running compose services
# 2. Start fresh services (webapi, ml-service, database)
# 3. Wait for services to be ready
# 4. Execute E2E test scenarios
# 5. Report test results
#

docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml down
docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build --wait webapi
ijhttp test/e2e/e2e-tests.http
