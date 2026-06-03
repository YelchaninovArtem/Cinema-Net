#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$Root  = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RunDir = "$Root\.run"

function Write-Info { param($msg) Write-Host "[INFO]  $msg" -ForegroundColor Green }
function Write-Warn { param($msg) Write-Host "[WARN]  $msg" -ForegroundColor Yellow }

function Stop-ByPidFile {
    param([string]$Name, [string]$PidFile)
    if (Test-Path $PidFile) {
        $procId = (Get-Content $PidFile -Raw).Trim()
        try {
            Get-Process -Id $procId -ErrorAction Stop | Out-Null
            # Kill the process tree (handles cmd -> node child processes)
            taskkill /PID $procId /T /F 2>&1 | Out-Null
            Write-Info "$Name (PID $procId) stopped."
        } catch {
            Write-Warn "$Name (PID $procId) was not running."
        }
        Remove-Item $PidFile -Force
    } else {
        Write-Warn "PID file $PidFile not found -- $Name may not be running."
    }
}

Stop-ByPidFile "Frontend" "$RunDir\frontend.pid"
Stop-ByPidFile "Backend"  "$RunDir\backend.pid"

Write-Info "Stopping MSSQL container..."
docker compose -f "$Root\docker-compose.yml" stop

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "  System stopped"                              -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
