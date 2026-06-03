#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $MyInvocation.MyCommand.Definition

function Write-Info  { param($msg) Write-Host "[INFO]  $msg" -ForegroundColor Green }
function Write-Warn  { param($msg) Write-Host "[WARN]  $msg" -ForegroundColor Yellow }
function Write-Err   { param($msg) Write-Host "[ERROR] $msg" -ForegroundColor Red; exit 1 }

# 1a. Docker + MSSQL
Write-Info "Checking Docker..."
docker info 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Err "Docker is not running. Start Docker Desktop and try again." }

# 1b. Load .env into current process environment
$EnvFile = "$Root\.env"
if (Test-Path $EnvFile) {
    Get-Content $EnvFile | Where-Object { $_ -match '^\s*[^#]\S+=\S' } | ForEach-Object {
        $parts = $_ -split '=', 2
        [System.Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), 'Process')
    }

    $emailMappings = @{
        'EMAIL_HOST'        = 'Email__Host'
        'EMAIL_PORT'        = 'Email__Port'
        'EMAIL_USER'        = 'Email__User'
        'EMAIL_PASSWORD'    = 'Email__Password'
        'EMAIL_FROM'        = 'Email__From'
        'EMAIL_SENDER_NAME' = 'Email__SenderName'
    }
    foreach ($mapping in $emailMappings.GetEnumerator()) {
        $value = [System.Environment]::GetEnvironmentVariable($mapping.Key, 'Process')
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            [System.Environment]::SetEnvironmentVariable($mapping.Value, $value, 'Process')
        }
    }
    if ([string]::IsNullOrWhiteSpace([System.Environment]::GetEnvironmentVariable('Email__From', 'Process'))) {
        $emailUser = [System.Environment]::GetEnvironmentVariable('EMAIL_USER', 'Process')
        if (-not [string]::IsNullOrWhiteSpace($emailUser)) {
            [System.Environment]::SetEnvironmentVariable('Email__From', $emailUser, 'Process')
        }
    }

    Write-Info "Loaded .env"
} else {
    Write-Warn ".env not found -- copy .env.example to .env and fill in credentials."
}

Write-Info "Starting MSSQL container..."
docker compose -f "$Root\docker-compose.yml" up -d
if ($LASTEXITCODE -ne 0) { Write-Err "Failed to start docker compose." }

Write-Info "Waiting for MSSQL to be ready..."
$ready = $false
for ($i = 1; $i -le 30; $i++) {
    $status = docker inspect --format='{{.State.Health.Status}}' cinema-mssql-dev 2>$null
    if ($status -eq 'healthy') { Write-Info "MSSQL is ready"; $ready = $true; break }
    Write-Host -NoNewline "."
    Start-Sleep -Seconds 2
}
if (-not $ready) { Write-Err "MSSQL did not respond within 60 seconds." }

# 2. Backend
Write-Info "Building backend (ASP.NET)..."
New-Item -ItemType Directory -Force -Path "$Root\.run" | Out-Null
$buildLog = "$Root\.run\build.log"

# Kill any lingering Cinema.Api processes before building
Get-Process -Name 'Cinema.Api' -ErrorAction SilentlyContinue | ForEach-Object {
    taskkill /PID $_.Id /T /F 2>&1 | Out-Null
}
Start-Sleep -Seconds 1

# Remove stale locked DLLs (left by zombie processes or IDE)
$outDir = "$Root\backend\Cinema.Api\bin\Debug\net8.0"
@('Cinema.Api.exe','Cinema.Domain.dll','Cinema.Application.dll','Cinema.Infrastructure.dll',
  'Cinema.Domain.pdb','Cinema.Application.pdb','Cinema.Infrastructure.pdb') | ForEach-Object {
    $f = "$outDir\$_"
    if (Test-Path $f) { Remove-Item $f -Force -ErrorAction SilentlyContinue }
}

Push-Location "$Root\backend"
dotnet build Cinema.sln --nologo 2>&1 | Tee-Object -FilePath $buildLog | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Warn "Build failed, cleaning and retrying..."
    dotnet clean Cinema.sln --nologo 2>&1 | Out-Null
    dotnet build Cinema.sln --nologo 2>&1 | Tee-Object -FilePath $buildLog | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Get-Content $buildLog | Select-Object -Last 20 | Write-Host
        Pop-Location; Write-Err "dotnet build failed. See .run\build.log for details."
    }
}
Pop-Location

$RunDir = "$Root\.run"
New-Item -ItemType Directory -Force -Path $RunDir | Out-Null
$LogBackend  = "$RunDir\backend.log"
$LogFrontend = "$RunDir\frontend.log"

Write-Info "Starting backend..."
$backendJob = Start-Process -FilePath 'dotnet' `
    -ArgumentList "run --project `"$Root\backend\Cinema.Api`" --no-build" `
    -WorkingDirectory "$Root\backend" `
    -RedirectStandardOutput $LogBackend `
    -RedirectStandardError  "$RunDir\backend.err.log" `
    -PassThru -WindowStyle Hidden
$backendJob.Id | Set-Content "$RunDir\backend.pid"

Write-Info "Waiting for backend to start..."
$backendReady = $false
for ($i = 1; $i -le 20; $i++) {
    try {
        $null = Invoke-WebRequest -Uri 'http://localhost:5136/swagger/v1/swagger.json' -UseBasicParsing -TimeoutSec 2
        Write-Info "Backend ready -> http://localhost:5136  (Swagger: http://localhost:5136/swagger)"
        $backendReady = $true; break
    } catch { }
    Start-Sleep -Seconds 2
}
if (-not $backendReady) { Write-Warn "Backend did not respond within 40 seconds. Check log: .run\backend.log" }

# 3. Frontend
Write-Info "Starting frontend (Angular)..."
$frontendJob = Start-Process -FilePath 'cmd' `
    -ArgumentList '/c npm start' `
    -WorkingDirectory "$Root\frontend" `
    -RedirectStandardOutput $LogFrontend `
    -RedirectStandardError  "$RunDir\frontend.err.log" `
    -PassThru -WindowStyle Hidden
$frontendJob.Id | Set-Content "$RunDir\frontend.pid"

Write-Info "Waiting for frontend to start..."
$frontendReady = $false
for ($i = 1; $i -le 30; $i++) {
    try {
        $null = Invoke-WebRequest -Uri 'http://localhost:4200' -UseBasicParsing -TimeoutSec 2
        Write-Info "Frontend ready -> http://localhost:4200"
        $frontendReady = $true; break
    } catch { }
    Start-Sleep -Seconds 2
}
if (-not $frontendReady) { Write-Warn "Frontend did not respond within 60 seconds. Check log: .run\frontend.log" }

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "  System is running"                           -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host "  Site:    " -NoNewline; Write-Host "http://localhost:4200"            -ForegroundColor Yellow
Write-Host "  API:     " -NoNewline; Write-Host "http://localhost:5136"            -ForegroundColor Yellow
Write-Host "  Swagger: " -NoNewline; Write-Host "http://localhost:5136/swagger"    -ForegroundColor Yellow
Write-Host ""
Write-Host "  Logs: .run\backend.log  |  .run\frontend.log"
Write-Host "  Stop: " -NoNewline; Write-Host ".\stop.ps1" -ForegroundColor Yellow
Write-Host "=============================================" -ForegroundColor Green
