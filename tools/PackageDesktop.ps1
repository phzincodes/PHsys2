param([string]$Dotnet = 'dotnet', [string]$ApiBaseUrl, [switch]$Production)
$ErrorActionPreference = 'Stop'
$workspacePath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $workspacePath
try {
    if ($Production -and [string]::IsNullOrWhiteSpace($ApiBaseUrl)) { throw 'Uma distribuição de produção exige o domínio HTTPS da API hospedada. Nenhum pacote será gerado.' }
    $apiUri = $null
    if (-not [string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
        if (-not [Uri]::TryCreate($ApiBaseUrl, [UriKind]::Absolute, [ref]$apiUri) -or $apiUri.Scheme -ne 'https' -or $apiUri.AbsolutePath -ne '/' -or $apiUri.UserInfo -or $apiUri.Query -or $apiUri.Fragment -or $apiUri.IsLoopback -or $apiUri.HostNameType -ne [UriHostNameType]::Dns -or $apiUri.Host.EndsWith('.invalid') -or $apiUri.Host.EndsWith('.localhost')) {
            throw 'A distribuição exige uma raiz HTTPS com domínio válido, sem credenciais, parâmetros, fragmentos ou loopback.'
        }
        if ($Production) {
            try { $health = Invoke-RestMethod -Uri ($apiUri.AbsoluteUri + 'health') -TimeoutSec 8 }
            catch { throw 'Não foi possível validar a API de produção por HTTPS. O pacote não será gerado.' }
            if ($health.service -ne 'PHsystem.Next' -or $health.protocolVersion -ne 1 -or -not $health.loginAvailable -or -not $health.registrationAvailable) { throw 'A API ainda não confirmou banco, autenticação e cadastro disponíveis.' }
        }
    }
    & $Dotnet publish src/PHsystem.Desktop -c Release -r win-x64 --self-contained true -o artifacts/desktop
    if ($LASTEXITCODE -ne 0) { throw 'Publish falhou; pacote não gerado.' }
    $settings = @{ schemaVersion = 1; environment = 'Production'; apiBaseUrl = $(if ($apiUri) { $apiUri.AbsoluteUri } else { $null }) }
    $settings | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $workspacePath 'artifacts/desktop/services.json') -Encoding utf8
    $destination = Join-Path $workspacePath 'artifacts/PHsystem-Next-0.3.0-win-x64-preview.zip'
    Compress-Archive -Path (Join-Path $workspacePath 'artifacts/desktop/*') -DestinationPath $destination -Force
    $hash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([IO.Path]::GetFileName($destination))" | Set-Content (Join-Path $workspacePath 'artifacts/SHA256SUMS.txt') -Encoding ascii
    Write-Output 'Pacote local gerado. Assinatura e validação em máquina limpa continuam obrigatórias antes de release pública.'
} finally { Pop-Location }
