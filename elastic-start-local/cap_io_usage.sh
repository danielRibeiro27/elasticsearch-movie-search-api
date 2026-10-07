#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$script_dir"

if [[ ! -f .env ]]; then
  printf 'Missing %s/.env. Restore the Elastic start-local environment file first.\n' "$script_dir" >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  printf 'Docker CLI was not found in PATH.\n' >&2
  exit 1
fi

# Pass the actual Docker-host device as the first argument if it is not /dev/sda.
export ES_LOCAL_IO_DEVICE="${1:-${ES_LOCAL_IO_DEVICE:-/dev/sda}}"
printf 'Applying Elasticsearch I/O limits to device %s.\n' "$ES_LOCAL_IO_DEVICE"

docker compose --env-file .env -f docker-compose.yml config --quiet
docker compose --env-file .env -f docker-compose.yml up -d --force-recreate elasticsearch

container_id="$(docker compose --env-file .env -f docker-compose.yml ps -q elasticsearch)"
if [[ -z "$container_id" ]]; then
  printf 'Elasticsearch container was not created.\n' >&2
  exit 1
fi

docker inspect "$container_id" --format 'Read BPS: {{json .HostConfig.BlkioDeviceReadBps}}'
docker inspect "$container_id" --format 'Write BPS: {{json .HostConfig.BlkioDeviceWriteBps}}'
docker inspect "$container_id" --format 'Read IOPS: {{json .HostConfig.BlkioDeviceReadIOps}}'
docker inspect "$container_id" --format 'Write IOPS: {{json .HostConfig.BlkioDeviceWriteIOps}}'

exec docker compose --env-file .env -f docker-compose.yml logs --tail 100 --follow elasticsearch