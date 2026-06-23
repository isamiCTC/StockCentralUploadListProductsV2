# Este script corre la suite de tests del proyecto y deja una salida
# más clara para uso diario desde terminal.
#
# Responsabilidades:
# - ejecutar `go test -count=1 ./...`
# - mostrar la salida real mientras corre
# - detectar si hubo fallos o errores
# - cerrar con un resumen corto y legible

$ErrorActionPreference = "Stop"

# Resolvemos rutas base relativas al script para no depender del cwd.
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

Write-Host ""
Write-Host "=== Tests StockCentralUploadListProductsV2 ===" -ForegroundColor Cyan
Write-Host ""

$OutputLines = New-Object System.Collections.Generic.List[string]

Push-Location $ProjectRoot
try {
    # Stream de salida en vivo + captura para análisis posterior.
    & go test -count=1 ./... 2>&1 | Tee-Object -Variable TestOutput
    $ExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

foreach ($Line in $TestOutput) {
    $OutputLines.Add([string]$Line)
}

# Extraemos líneas relevantes para el resumen final.
$FailLines = $OutputLines | Where-Object { $_ -match '^(--- FAIL:|FAIL\t|FAIL |panic:)' }
$OkPackageLines = $OutputLines | Where-Object { $_ -match '^ok\s' }
$NoTestLines = $OutputLines | Where-Object { $_ -match '^\?\s' }

Write-Host ""
Write-Host "=== Resumen ===" -ForegroundColor Cyan
Write-Host ("Paquetes OK       : {0}" -f $OkPackageLines.Count)
Write-Host ("Paquetes sin tests: {0}" -f $NoTestLines.Count)

if ($ExitCode -eq 0) {
    Write-Host "Estado final      : OK" -ForegroundColor Green
    Write-Host ""
    Write-Host "Todos los tests pasaron." -ForegroundColor Green
    Write-Host ""
    exit 0
}

Write-Host "Estado final      : FAIL" -ForegroundColor Red
Write-Host ""
Write-Host "Se detectaron fallos. Resumen de líneas clave:" -ForegroundColor Red

foreach ($Line in $FailLines) {
    Write-Host $Line -ForegroundColor Red
}

exit $ExitCode
