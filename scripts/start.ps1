param(
    [switch]$NoBrowser,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

$webUrl = 'http://localhost:3000'
$apiUrl = 'http://localhost:5080'

function Pause-Exit([int]$code) {
    if (-not $NoPause) { Read-Host 'Press Enter to close' | Out-Null }
    exit $code
}

function Test-Url([string]$url) {
    try {
        $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
        return $r.StatusCode -eq 200
    } catch {
        return $false
    }
}

function Test-DockerRunning {
    & docker info *> $null
    return $LASTEXITCODE -eq 0
}

Write-Host ''
Write-Host 'Travelogic Suppliers' -ForegroundColor Cyan
Write-Host '--------------------'

# 1. Docker installed?
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host 'Docker Desktop is not installed. It is needed to run SQL Server, the API and the web app.' -ForegroundColor Yellow
    if (Get-Command winget -ErrorAction SilentlyContinue) {
        $answer = Read-Host 'Install Docker Desktop now? (Y/N)'
        if ($answer -match '^[Yy]') {
            winget install -e --id Docker.DockerDesktop --accept-package-agreements --accept-source-agreements
            Write-Host ''
            Write-Host 'Docker Desktop is installed. Restart Windows (or sign out and in), open Docker Desktop once to finish its setup, then run start.cmd again.' -ForegroundColor Yellow
            Pause-Exit 0
        }
    }
    Write-Host 'Download it from https://www.docker.com/products/docker-desktop/ then run start.cmd again.'
    Start-Process 'https://www.docker.com/products/docker-desktop/'
    Pause-Exit 1
}

# 2. Docker running?
if (-not (Test-DockerRunning)) {
    $desktop = Join-Path $env:ProgramFiles 'Docker\Docker\Docker Desktop.exe'
    if (Test-Path $desktop) {
        Write-Host 'Starting Docker Desktop...'
        Start-Process $desktop
    } else {
        Write-Host 'Please start Docker Desktop.' -ForegroundColor Yellow
    }

    Write-Host -NoNewline 'Waiting for Docker'
    $deadline = (Get-Date).AddMinutes(3)
    while (-not (Test-DockerRunning)) {
        if ((Get-Date) -gt $deadline) {
            Write-Host ''
            Write-Host 'Docker did not start within 3 minutes. Open Docker Desktop, wait until it says "running", then try again.' -ForegroundColor Red
            Pause-Exit 1
        }
        Write-Host -NoNewline '.'
        Start-Sleep -Seconds 3
    }
    Write-Host ' ready'
}

# 3. Build and start
Write-Host 'Building and starting the containers (the first run downloads images and takes a few minutes)...'
& docker compose up -d --build
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host 'docker compose failed. If a port is already in use, free ports 3000, 5080 and 1433 and try again.' -ForegroundColor Red
    Pause-Exit 1
}

# 4. Wait until the app answers
Write-Host -NoNewline 'Waiting for the API and web app'
$deadline = (Get-Date).AddMinutes(5)
while (-not ((Test-Url "$apiUrl/health/ready") -and (Test-Url $webUrl))) {
    if ((Get-Date) -gt $deadline) {
        Write-Host ''
        Write-Host 'The app did not become ready in 5 minutes. Check the logs with: docker compose logs supplier-api' -ForegroundColor Red
        Pause-Exit 1
    }
    Write-Host -NoNewline '.'
    Start-Sleep -Seconds 3
}
Write-Host ' ready'

Write-Host ''
Write-Host "Web app:        $webUrl" -ForegroundColor Green
Write-Host "API reference:  $apiUrl/scalar" -ForegroundColor Green
Write-Host 'Stop it with stop.cmd'
Write-Host ''

if (-not $NoBrowser) { Start-Process $webUrl }
Pause-Exit 0
