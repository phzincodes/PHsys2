param([string]$Dotnet='dotnet', [string]$Compiler='C:\Program Files (x86)\Inno Setup 6\ISCC.exe')
$ErrorActionPreference='Stop'
$workspacePath=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if(-not(Test-Path -LiteralPath $Compiler)){throw 'Inno Setup não encontrado. Nenhuma instalação será realizada por este script.'}
Push-Location $workspacePath
try {
    & ./tools/PackageDesktop.ps1 -Dotnet $Dotnet
    if($LASTEXITCODE -ne 0){throw 'Empacotamento falhou.'}
    & $Compiler /Qp installer/PHsystem.iss
    if($LASTEXITCODE -ne 0){throw 'Compilação Inno Setup falhou.'}
    $source=Join-Path $workspacePath 'artifacts/installer/PHsystem-Next-0.3.0-win-x64-preview.exe'
    $releaseDirectory=Join-Path $workspacePath 'artifacts/releases'
    [void][IO.Directory]::CreateDirectory($releaseDirectory)
    $destination=Join-Path $releaseDirectory 'phsystem-next-preview-win-x64.exe'
    Copy-Item -LiteralPath $source -Destination ($destination+'.tmp') -Force
    Move-Item -LiteralPath ($destination+'.tmp') -Destination $destination -Force
    $hash=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
    $signature=Get-AuthenticodeSignature -LiteralPath $destination
    $receipt=@{schemaVersion=1;version='0.3.0-preview';architecture='win-x64';bytes=(Get-Item -LiteralPath $destination).Length;sha256=$hash;signed=($signature.Status -eq 'Valid');preview=$true;requirements='Windows x64; prévia sem instalação limpa validada';notes='Diagnóstico local offline; API de produção ainda não configurada.'}
    $metadata=Join-Path $releaseDirectory 'release.json'
    $receipt|ConvertTo-Json|Set-Content -LiteralPath ($metadata+'.tmp') -Encoding utf8
    Move-Item -LiteralPath ($metadata+'.tmp') -Destination $metadata -Force
    "$hash  phsystem-next-preview-win-x64.exe"|Set-Content -LiteralPath (Join-Path $releaseDirectory 'SHA256SUMS.txt') -Encoding ascii
    Write-Output "Instalador de prévia gerado em $destination. Não foi executado nem publicado."
} finally {Pop-Location}
