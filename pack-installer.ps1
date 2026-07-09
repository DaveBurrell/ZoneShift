# Builds ZoneShift installers for win-x64 and win-arm64:
#   dist\ZoneShift-Setup-<version>-x64.exe
#   dist\ZoneShift-Setup-<version>-arm64.exe
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$version = "1.5.0" # fallback if csproj parse fails
$csproj = Join-Path $root "TimezoneConverter.csproj"
# Prefer version from csproj if present
$csprojText = Get-Content $csproj -Raw
if ($csprojText -match '<Version>([^<]+)</Version>') {
    $version = $Matches[1].Trim()
}

$distDir = Join-Path $root "dist"
$iss = Join-Path $root "installer\ZoneShift.iss"
$iscc = Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { $iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" }
if (-not (Test-Path $iscc)) {
    throw "Inno Setup compiler not found. Install with: winget install JRSoftware.InnoSetup"
}

New-Item -ItemType Directory -Force -Path $distDir | Out-Null

function Publish-Arch([string]$rid) {
    $out = Join-Path $root "publish\$rid"
    Write-Host "==> Publishing self-contained ZoneShift ($rid)..." -ForegroundColor Cyan
    if (Test-Path $out) { Remove-Item $out -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $out | Out-Null

    dotnet publish $csproj `
        -c Release `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=none `
        -o $out

    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $rid" }
    $exe = Join-Path $out "ZoneShift.exe"
    if (-not (Test-Path $exe)) { throw "Published exe not found: $exe" }
    Write-Host "    $exe ($([math]::Round((Get-Item $exe).Length / 1MB, 1)) MB)"
    return $out
}

function Build-Setup([string]$arch, [string]$publishDir) {
    Write-Host "==> Building installer ($arch)..." -ForegroundColor Cyan
    $publishRel = "..\publish\win-$arch"
    if ($arch -eq "x64") { $publishRel = "..\publish\win-x64" }
    if ($arch -eq "arm64") { $publishRel = "..\publish\win-arm64" }

    & $iscc `
        "/DMyAppVersion=$version" `
        "/DAppArch=$arch" `
        "/DPublishDir=$publishRel" `
        $iss

    if ($LASTEXITCODE -ne 0) { throw "ISCC failed for $arch" }
}

Publish-Arch "win-x64" | Out-Null
Build-Setup "x64" "publish\win-x64"

Publish-Arch "win-arm64" | Out-Null
Build-Setup "arm64" "publish\win-arm64"

Write-Host ""
Write-Host "Installers ready:" -ForegroundColor Green
Get-ChildItem $distDir -Filter "ZoneShift-Setup-$version-*.exe" | ForEach-Object {
    Write-Host ("  {0}  ({1} MB)" -f $_.FullName, [math]::Round($_.Length / 1MB, 1))
}
Write-Host ""
Write-Host "Release both assets on GitHub so auto-update can pick the matching arch." -ForegroundColor Cyan
