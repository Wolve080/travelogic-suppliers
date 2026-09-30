@echo off
cd /d "%~dp0"
docker compose down
echo.
echo Stopped. Your data is kept; run start.cmd to start again.
pause
