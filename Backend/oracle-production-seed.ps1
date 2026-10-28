param(
    [Parameter(Mandatory = $true)]
    [string]$WebHostDllPath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $WebHostDllPath)) {
    throw "WebHost DLL not found: $WebHostDllPath"
}

Write-Host "Running Oracle seed flow..." -ForegroundColor Cyan
dotnet $WebHostDllPath /seed
if ($LASTEXITCODE -ne 0) {
    throw "Seed command failed for $WebHostDllPath"
}

Write-Host "Oracle seed finished successfully." -ForegroundColor Green
