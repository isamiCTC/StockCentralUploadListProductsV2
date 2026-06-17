[CmdletBinding()]
param(
    [switch]$SkipUPX,
    [string]$GoCachePath,
    [string]$GoModCachePath
)

# Este script compila el binario del proyecto y deja una carpeta `dist`
# lista para entregar con el ejecutable y la carpeta `config`.
#
# Responsabilidades:
# - detectar el OS y la arquitectura del host
# - compilar el binario correcto para ese entorno
# - copiar la carpeta `config` completa al artefacto final
# - comprimir con `upx` si está instalado y no se pidió omitirlo

$ErrorActionPreference = "Stop"

# Resolvemos rutas base relativas al propio script para que no dependa
# del directorio actual desde el que se lo ejecute.
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# Datos principales del binario y del layout de salida.
$AppName = "StockCentralUploadListProductsV2"
$ConfigSourceDir = Join-Path $ProjectRoot "config"
$DistRoot = Join-Path $ProjectRoot "dist"

# Definimos caches por defecto dentro del repo, pero permitimos override
# por parámetro o variables de entorno para que el script sea más flexible.
$CacheRoot = Join-Path $ProjectRoot ".cache"
$DefaultGoCache = Join-Path $CacheRoot "go-build"
$DefaultGoModCache = Join-Path $CacheRoot "go-modcache"

# Determinamos el OS del host usando variables nativas de PowerShell.
if ($IsWindows) {
    $TargetOS = "windows"
    $BinaryName = "$AppName.exe"
}
elseif ($IsMacOS) {
    $TargetOS = "darwin"
    $BinaryName = $AppName
}
elseif ($IsLinux) {
    $TargetOS = "linux"
    $BinaryName = $AppName
}
else {
    throw "No se pudo determinar el sistema operativo actual."
}

# Normalizamos la arquitectura del proceso actual a nombres compatibles
# con Go para que la carpeta de salida sea clara.
switch ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()) {
    "x64"   { $TargetArch = "amd64" }
    "arm64" { $TargetArch = "arm64" }
    "x86"   { $TargetArch = "386" }
    default { throw "Arquitectura no soportada: $([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)" }
}

# Armamos una dist por plataforma para que el resultado sea explícito.
$ArtifactName = "$TargetOS-$TargetArch"
$DistDir = Join-Path $DistRoot $ArtifactName
$ConfigDistDir = Join-Path $DistDir "config"
$BinaryPath = Join-Path $DistDir $BinaryName

# Elegimos el cache efectivo priorizando:
# 1. parámetro explícito
# 2. variable de entorno existente
# 3. default del repo
$EffectiveGoCache = if ($GoCachePath) { $GoCachePath } elseif ($env:GOCACHE) { $env:GOCACHE } else { $DefaultGoCache }
$EffectiveGoModCache = if ($GoModCachePath) { $GoModCachePath } elseif ($env:GOMODCACHE) { $env:GOMODCACHE } else { $DefaultGoModCache }

Write-Host ""
Write-Host "=== Build StockCentralUploadListProductsV2 ===" -ForegroundColor Cyan
Write-Host "Proyecto     : $ProjectRoot"
Write-Host "Target OS    : $TargetOS"
Write-Host "Target Arch  : $TargetArch"
Write-Host "Salida       : $DistDir"
Write-Host "GOCACHE      : $EffectiveGoCache"
Write-Host "GOMODCACHE   : $EffectiveGoModCache"
Write-Host ""

# Limpiamos solo el artefacto de esta plataforma para no tocar otras dist.
if (Test-Path $DistDir) {
    Remove-Item -Path $DistDir -Recurse -Force
}

# Creamos todo lo necesario antes de invocar a Go.
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
New-Item -ItemType Directory -Path $ConfigDistDir -Force | Out-Null
New-Item -ItemType Directory -Path $EffectiveGoCache -Force | Out-Null
New-Item -ItemType Directory -Path $EffectiveGoModCache -Force | Out-Null

# Exportamos variables al proceso actual para que `go build` use los caches
# locales y compile para la plataforma detectada.
$env:GOCACHE = $EffectiveGoCache
$env:GOMODCACHE = $EffectiveGoModCache
$env:GOOS = $TargetOS
$env:GOARCH = $TargetArch

Write-Host "Compilando binario..." -ForegroundColor Yellow

Push-Location $ProjectRoot
try {
    & go build -buildvcs=false -o $BinaryPath "./cmd/$AppName"
    if ($LASTEXITCODE -ne 0) {
        throw "go build terminó con código $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

Write-Host "Copiando carpeta config..." -ForegroundColor Yellow

# Copiamos todos los archivos, incluidos ocultos, preservando estructura.
Get-ChildItem -Path $ConfigSourceDir -Force | ForEach-Object {
    $Destination = Join-Path $ConfigDistDir $_.Name
    Copy-Item -Path $_.FullName -Destination $Destination -Recurse -Force
}

# Detectamos `upx` en PATH y lo usamos solo si existe y no se pidió omitirlo.
$UpxCommand = Get-Command upx -ErrorAction SilentlyContinue
if (-not $SkipUPX -and $null -ne $UpxCommand) {
    Write-Host "UPX detectado en PATH. Comprimiendo binario..." -ForegroundColor Yellow
    & $UpxCommand.Source "--best" "--lzma" $BinaryPath
    if ($LASTEXITCODE -ne 0) {
        throw "upx terminó con código $LASTEXITCODE"
    }
}
else {
    Write-Host "UPX no se usará (no disponible o SkipUPX activo)." -ForegroundColor DarkYellow
}

Write-Host ""
Write-Host "Build completado correctamente." -ForegroundColor Green
Write-Host "Binario : $BinaryPath"
Write-Host "Config  : $ConfigDistDir"
Write-Host ""
