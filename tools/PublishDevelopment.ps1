param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$workspacePath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $workspacePath
try {
    # Separate output avoids replacing the IDE's running Debug executable.
    # Debug intentionally uses the localhost discovery produced by StartDevelopment.
    & $Dotnet publish src/PHsystem.Desktop -c Debug --artifacts-path artifacts/development-build -r win-x64 --self-contained true -o artifacts/development
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível publicar a prévia de desenvolvimento.' }
    Write-Output 'Prévia local: artifacts/development/PHsystem.Desktop.exe. Não distribuir como produção; usa descoberta de desenvolvimento.'
} finally { Pop-Location }
