# Builds ZoneShift and produces a single-file installer: dist\ZoneShift-Setup-1.1.0.exe
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$publishDir = Join-Path $root "publish\win-x64"
$distDir = Join-Path $root "dist"
$iss = Join-Path $root "installer\ZoneShift.iss"
$iscc = Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"

if (-not (Test-Path $iscc)) {
    $iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $iscc)) {
    throw "Inno Setup compiler not found. Install with: winget install JRSoftware.InnoSetup"
}

Write-Host "==> Publishing self-contained ZoneShift (win-x64)..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $distDir | Out-Null

dotnet publish `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -o $publishDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

$exe = Join-Path $publishDir "ZoneShift.exe"
if (-not (Test-Path $exe)) { throw "Published exe not found: $exe" }

Write-Host "==> Published payload:" -ForegroundColor Cyan
Get-ChildItem $publishDir | Format-Table Name, Length -AutoSize

Write-Host "==> Building installer with Inno Setup..." -ForegroundColor Cyan
& $iscc $iss
if ($LASTEXITCODE -ne 0) { throw "ISCC failed" }

$setup = Get-ChildItem $distDir -Filter "ZoneShift-Setup-*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $setup) { throw "Installer not found in dist\" }

Write-Host ""
Write-Host "Installer ready:" -ForegroundColor Green
Write-Host "  $($setup.FullName)"
Write-Host "  Size: $([math]::Round($setup.Length / 1MB, 1)) MB"
Write-Host ""
Write-Host "Double-click the setup exe to install ZoneShift." -ForegroundColor Green
