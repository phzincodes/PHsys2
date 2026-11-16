param([string]$Builder = '')
$ErrorActionPreference = 'Stop'
# Read-only diagnostics. Does not build, change the selected builder, inspect
# private Compose values, modify permissions, migrate databases or reset caches.
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw 'Instale/configure Docker antes de iniciar o ambiente de desenvolvimento.' }
& docker info --format 'Docker {{.ServerVersion}}'
if ($LASTEXITCODE -ne 0) { throw 'O Docker não está disponível.' }
$builderArguments = @('buildx', 'inspect')
if ($Builder) { $builderArguments += $Builder }
& docker @builderArguments
if ($LASTEXITCODE -ne 0) { throw 'Builder indisponível. Selecione um builder saudável; não remova volumes ou caches para contornar a falha.' }
$version = & docker compose version --short
if ($LASTEXITCODE -ne 0) { throw 'Docker Compose não está disponível.' }
Write-Output "Compose $version"
Write-Output 'Preflight concluído. Não houve build nem alteração de serviços. Use StartDevelopment.ps1 para iniciar o ambiente.'
