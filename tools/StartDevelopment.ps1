param(
    [string]$Dotnet = 'dotnet',
    [string]$EnvironmentFile = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'PHsystemNext/development/compose.env'),
    [switch]$NoDesktop
)
$ErrorActionPreference = 'Stop'
$workspacePath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $workspacePath
try {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Docker não está disponível. Este inicializador é exclusivo do desenvolvedor; usuários finais usam a API hospedada. Consulte docs/API-CONNECTION.md.'
    }
    & docker info --format '{{.ServerVersion}}'
    if ($LASTEXITCODE -ne 0) { throw 'O Docker não está iniciado.' }
    & $Dotnet dev-certs https --check --trust
    if ($LASTEXITCODE -ne 0) {
        throw 'Certificado de desenvolvimento ainda não confiável. O responsável pelo ambiente deve configurar o certificado local uma vez; a validação TLS não será ignorada.'
    }
    if (-not (Test-Path -LiteralPath $EnvironmentFile)) { throw 'Provisionamento de desenvolvimento ausente. Prepare o compose.env fora do repositório conforme docs/API-CONNECTION.md.' }
    # Compose reads the operator file outside the repository. Never print docker compose config or secret values.
    & docker compose --env-file $EnvironmentFile build api migrate
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível compilar os serviços.' }
    & docker compose --env-file $EnvironmentFile up -d --wait --wait-timeout 90 postgres mailpit
    if ($LASTEXITCODE -ne 0) { throw 'Banco/e-mail de desenvolvimento indisponíveis.' }
    & docker compose --env-file $EnvironmentFile run --rm migrate
    if ($LASTEXITCODE -ne 0) { throw 'Migrações não concluídas; o desktop não será iniciado.' }
    & docker compose --env-file $EnvironmentFile up -d --no-deps api
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível iniciar a API.' }
    $portOutput = & docker compose --env-file $EnvironmentFile port api 8443
    $portExitCode = $LASTEXITCODE
    $binding = ([string]($portOutput | Select-Object -First 1)).Trim()
    $portMatch = [regex]::Match($binding, '^127\.0\.0\.1:(\d+)$')
    if ($portExitCode -ne 0 -or -not $portMatch.Success) { throw ("Não foi possível descobrir a porta da API. Resposta: [{0}]; código: {1}" -f $binding, $portExitCode) }
    $apiAddress = 'https://localhost:' + $portMatch.Groups[1].Value + '/'
    $ready = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        try {
            $health = Invoke-RestMethod -Uri ($apiAddress + 'health') -TimeoutSec 3
            if ($health.service -eq 'PHsystem.Next' -and $health.protocolVersion -eq 1 -and $health.loginAvailable -and $health.registrationAvailable) { $ready = $true; break }
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    if (-not $ready) { throw 'Não foi possível conectar aos serviços do PHsystem. Verifique o provisionamento de desenvolvimento; TLS e saúde do banco precisam estar válidos.' }
    $discoveryDirectory = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'PHsystemNext/development'
    [void][IO.Directory]::CreateDirectory($discoveryDirectory)
    $discoveryFile = Join-Path $discoveryDirectory 'api-discovery.json'
    $discovery = @{ schemaVersion = 1; apiBaseUrl = $apiAddress; expiresAt = [DateTimeOffset]::UtcNow.AddHours(12).ToString('O') }
    $discovery | ConvertTo-Json | Set-Content -LiteralPath ($discoveryFile + '.tmp') -Encoding utf8
    Move-Item -LiteralPath ($discoveryFile + '.tmp') -Destination $discoveryFile -Force
    & $Dotnet build src/PHsystem.Desktop -c Debug
    if ($LASTEXITCODE -ne 0) { throw 'Compilação desktop falhou.' }
    $inboxPortOutput = & docker compose --env-file $EnvironmentFile port mailpit 8025
    $inboxPortExitCode = $LASTEXITCODE
    $inboxBinding = ([string]($inboxPortOutput | Select-Object -First 1)).Trim()
    $inboxPortMatch = [regex]::Match($inboxBinding, '^127\.0\.0\.1:(\d+)$')
    if ($inboxPortExitCode -eq 0 -and $inboxPortMatch.Success) { Write-Output ('Caixa de e-mail de teste: http://localhost:' + $inboxPortMatch.Groups[1].Value) }
    Write-Output 'Serviços prontos; descoberta automática configurada. Nenhuma URL precisa ser digitada no aplicativo.'
    if (-not $NoDesktop) {
        # Visible interactive app requested by the developer; no background command service is installed.
        Start-Process -FilePath (Join-Path $workspacePath 'src/PHsystem.Desktop/bin/Debug/net10.0-windows/PHsystem.Desktop.exe') -WorkingDirectory $workspacePath
    }
} finally { Pop-Location }
