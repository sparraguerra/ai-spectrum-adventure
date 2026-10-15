[CmdletBinding()]
param()

$volumeNames = @(
    "spectrum-adventure-postgres",
    "spectrum-adventure-azurite"
)

$dataProtectionKeyRingPath = Join-Path $env:LOCALAPPDATA "AI.SpectrumAdventure\data-protection-keys"

Write-Warning "This permanently removes only the named local Aspire PostgreSQL and Azurite volumes plus the local Data Protection key ring."
$confirmation = Read-Host "Type RESET to continue"

if ($confirmation -cne "RESET") {
    Write-Host "Reset cancelled."
    exit 1
}

foreach ($volumeName in $volumeNames) {
    docker volume inspect $volumeName *> $null

    if ($LASTEXITCODE -eq 0) {
        docker volume rm $volumeName
    }
}

if (Test-Path $dataProtectionKeyRingPath) {
    Remove-Item -Path $dataProtectionKeyRingPath -Recurse -Force
}

Write-Host "Local Aspire state reset completed."