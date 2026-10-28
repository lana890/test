param(
    [Parameter(Mandatory = $true)]
    [string]$SqlPlusConnection,

    [Parameter(Mandatory = $false)]
    [string]$SqlPlusPath = "sqlplus",

    [Parameter(Mandatory = $false)]
    [string]$MigrationScriptPath = "D:\GIT\DCMS\Backend\oracle-production-migration.sql",

    [Parameter(Mandatory = $true)]
    [string]$WebHostDllPath,

    [Parameter(Mandatory = $false)]
    [switch]$SkipMigration,

    [Parameter(Mandatory = $false)]
    [switch]$SkipSeed
)

$ErrorActionPreference = "Stop"

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,

        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
    & $Action
}

if (-not $SkipMigration) {
    if (-not (Test-Path -LiteralPath $MigrationScriptPath)) {
        throw "Migration script not found: $MigrationScriptPath"
    }

    Invoke-Step -Message "Applying Oracle migration script" -Action {
        & $SqlPlusPath $SqlPlusConnection "@$MigrationScriptPath"
        if ($LASTEXITCODE -ne 0) {
            throw "sqlplus failed while applying $MigrationScriptPath"
        }
    }
}

if (-not $SkipSeed) {
    if (-not (Test-Path -LiteralPath $WebHostDllPath)) {
        throw "WebHost DLL not found: $WebHostDllPath"
    }

    Invoke-Step -Message "Running Oracle seed flow" -Action {
        dotnet $WebHostDllPath /seed
        if ($LASTEXITCODE -ne 0) {
            throw "Seed command failed for $WebHostDllPath"
        }
    }
}

Write-Host ""
Write-Host "Oracle production deployment finished successfully." -ForegroundColor Green
