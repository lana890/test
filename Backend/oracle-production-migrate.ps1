param(
    [Parameter(Mandatory = $true)]
    [string]$SqlPlusConnection,

    [Parameter(Mandatory = $false)]
    [string]$SqlPlusPath = "sqlplus",

    [Parameter(Mandatory = $false)]
    [string]$MigrationScriptPath = "D:\GIT\DCMS\Backend\oracle-production-migration.sql"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $MigrationScriptPath)) {
    throw "Migration script not found: $MigrationScriptPath"
}

Write-Host "Applying Oracle migration script..." -ForegroundColor Cyan
& $SqlPlusPath $SqlPlusConnection "@$MigrationScriptPath"
if ($LASTEXITCODE -ne 0) {
    throw "sqlplus failed while applying $MigrationScriptPath"
}

Write-Host "Oracle migration finished successfully." -ForegroundColor Green
