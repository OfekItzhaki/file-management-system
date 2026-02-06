Write-Host "🚀 Setting up Horizon FMS development environment..." -ForegroundColor Green

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
Start-Process powershell -WorkingDirectory $PSScriptRoot -ArgumentList "-NoExit", "-Command", "Write-Host '--- Horizon API ---' -ForegroundColor Cyan; Set-Location 'FileManagementSystem.API'; dotnet run"

# Launch Web
Start-Process powershell -WorkingDirectory $PSScriptRoot -ArgumentList "-NoExit", "-Command", "Write-Host '--- Horizon Web ---' -ForegroundColor Cyan; Set-Location 'FileManagementSystem.Web'; npm run dev"

Write-Host "`n✅ Done! Both services are starting." -ForegroundColor Green
Write-Host "API: https://localhost:7136 (Swagger: /swagger)" -ForegroundColor Gray
Write-Host "Web: http://localhost:5173" -ForegroundColor Gray
