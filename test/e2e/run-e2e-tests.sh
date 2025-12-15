#!/usr/bin/env sh

docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml down
docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build --wait webapi
ijhttp test/e2e/e2e-tests.http