#!/usr/bin/env bash
# Double-click on macOS, or run ./start.command on Linux.
cd "$(dirname "$0")" || exit 1

web=http://localhost:3000
api=http://localhost:5080

finish() { echo; read -r -p "Press Enter to close" _; exit "$1"; }
open_url() { if command -v open >/dev/null; then open "$1"; else xdg-open "$1" >/dev/null 2>&1; fi; }

echo "Travelogic Suppliers"
echo "--------------------"

if ! command -v docker >/dev/null; then
  echo "Docker Desktop is not installed. Download it, then run this again."
  open_url https://www.docker.com/products/docker-desktop/
  finish 1
fi

if ! docker info >/dev/null 2>&1; then
  [ "$(uname)" = "Darwin" ] && open -a Docker
  printf "Waiting for Docker"
  for _ in $(seq 1 60); do
    docker info >/dev/null 2>&1 && break
    printf "."; sleep 3
  done
  echo
  docker info >/dev/null 2>&1 || { echo "Docker did not start. Open Docker Desktop and try again."; finish 1; }
fi

echo "Building and starting the containers (first run takes a few minutes)..."
docker compose up -d --build || { echo "docker compose failed. Free ports 3000, 5080 and 1433 and try again."; finish 1; }

printf "Waiting for the API and web app"
for _ in $(seq 1 100); do
  curl -fs "$api/health/ready" >/dev/null && curl -fs "$web" >/dev/null && break
  printf "."; sleep 3
done
echo
curl -fs "$api/health/ready" >/dev/null || { echo "Not ready yet. Check: docker compose logs supplier-api"; finish 1; }

echo
echo "Web app:        $web"
echo "API reference:  $api/scalar"
echo "Stop it with ./stop.command"
open_url "$web"
finish 0
