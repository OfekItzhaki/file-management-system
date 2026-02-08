[int]$StartApiPort = 5295,
[int]$StartWebPort = 5173
)

function Get-FreePort {
    param ([int]$Port)
    $properties = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties()
    $listeners = $properties.GetActiveTcpListeners()
    
    while ($listeners.Port -contains $Port) {
        Write-Host "   ⚠️ Port $Port is busy, trying $($Port + 1)..." -ForegroundColor DarkGray
        $Port++
    }
    return $Port
}

Write-Host "🚀 Setting up Horizon FMS development environment..." -ForegroundColor Green

# 0. Resolve Ports
Write-Host "`n🔍 Checking ports..." -ForegroundColor Cyan
$ApiPort = Get-FreePort -Port $StartApiPort
$WebPort = Get-FreePort -Port $StartWebPort

# Ensure they aren't the same
if ($ApiPort -eq $WebPort) {
    $WebPort = Get-FreePort -Port ($WebPort + 1)
}

Write-Host "   ✅ API Port: $ApiPort" -ForegroundColor Green
Write-Host "   ✅ Web Port: $WebPort" -ForegroundColor Green

# 1. Restore .NET Packages
Write-Host "`n📦 Restoring .NET packages..." -ForegroundColor Cyan
dotnet restore

# 2. Setup Web Frontend
Write-Host "`n📦 Setting up Web frontend..." -ForegroundColor Cyan
$webDir = Join-Path $PSScriptRoot "FileManagementSystem.Web"
if (-not (Test-Path (Join-Path $webDir "node_modules"))) {
    Write-Host "node_modules not found in $webDir. Installing dependencies..." -ForegroundColor Yellow
    Push-Location $webDir
    npm install
    Pop-Location
}
else {
    Write-Host "node_modules already exists. Skipping npm install." -ForegroundColor Gray
}

# 3. Launch Services
Write-Host "`n🚆 Launching services in separate windows..." -ForegroundColor Green

# Launch API
# We use --urls to override the launchSettings.json
$apiCommand = "Write-Host '--- Horizon API ---' -ForegroundColor Cyan; Set-Location 'FileManagementSystem.API'; dotnet run --urls http://localhost:$ApiPort"
Start-Process powershell -WorkingDirectory $PSScriptRoot -ArgumentList "-NoExit", "-Command", $apiCommand

# Launch Web
# We set API_PORT env var for Vite proxy, and use --port for Vite server
$webCommand = "Write-Host '--- Horizon Web ---' -ForegroundColor Cyan; Set-Location 'FileManagementSystem.Web'; `$env:API_PORT=$ApiPort; npm run dev -- --port $WebPort"
Start-Process powershell -WorkingDirectory $PSScriptRoot -ArgumentList "-NoExit", "-Command", $webCommand

Write-Host "`n✅ Done! Both services are starting." -ForegroundColor Green
Write-Host "API: http://localhost:$ApiPort (Swagger: /swagger)" -ForegroundColor Gray
Write-Host "Web: http://localhost:$WebPort" -ForegroundColor Gray
