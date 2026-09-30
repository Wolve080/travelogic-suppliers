#!/usr/bin/env bash
cd "$(dirname "$0")" || exit 1
docker compose down
echo
echo "Stopped. Your data is kept; run start.command to start again."
read -r -p "Press Enter to close" _
