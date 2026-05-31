# build-installer.ps1
# Compile CFF View puis package avec InnoSetup
# Lancer depuis le dossier installer\ ou depuis la racine du repo

param(
    [string]$Runtime       = "win-x64",
    [string]$Configuration = "Release",
    [switch]$SelfContained = $true
)

$ErrorActionPreference = "Stop"

$repoRoot    = Resolve-Path "$PSScriptRoot\.."
$projectFile = "$repoRoot\cffview\cffview.csproj"
$publishOut  = "$repoRoot\publish\$Runtime"
$issFile     = "$PSScriptRoot\CFFView.iss"

$innoSetupPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$iscc = $innoSetupPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    Write-Error "Inno Setup 6 introuvable. Telechargez-le depuis https://jrsoftware.org/isdl.php"
    exit 1
}

Write-Host ""
Write-Host "=== CFF View - Build Installer ===" -ForegroundColor Cyan
Write-Host "Project  : $projectFile"
Write-Host "Runtime  : $Runtime"
Write-Host "Mode     : $(if ($SelfContained) { 'Self-contained' } else { 'Framework-dependent' })"
Write-Host "Output   : $publishOut"
Write-Host "ISCC     : $iscc"
Write-Host ""

# 1) dotnet publish
Write-Host "[1/2] Compilation + publication..." -ForegroundColor Yellow

if (Test-Path $publishOut) { Remove-Item $publishOut -Recurse -Force }

$scFlag = $SelfContained.ToString().ToLower()

dotnet publish $projectFile `
    -c $Configuration `
    -r $Runtime `
    --self-contained $scFlag `
    -p:PublishReadyToRun=true `
    -o $publishOut

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish a echoue (code $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host "Publication reussie : $publishOut" -ForegroundColor Green

# 2) InnoSetup compile
Write-Host ""
Write-Host "[2/2] Generation du setup avec InnoSetup..." -ForegroundColor Yellow

$outputDir = "$PSScriptRoot\output"
if (-not (Test-Path $outputDir)) { New-Item -ItemType Directory $outputDir | Out-Null }

& $iscc /DPublishDir="$publishOut" $issFile

if ($LASTEXITCODE -ne 0) {
    Write-Error "InnoSetup a echoue (code $LASTEXITCODE)"
    exit $LASTEXITCODE
}

$installer = Get-ChildItem "$outputDir\*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

Write-Host ""
Write-Host "=== DONE ===" -ForegroundColor Green
if ($installer) {
    Write-Host "Installer genere : $($installer.FullName)" -ForegroundColor Cyan
    Write-Host "Taille           : $([math]::Round($installer.Length / 1MB, 1)) MB" -ForegroundColor Cyan
}
