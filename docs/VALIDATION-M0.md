# Validação M0 — 2026-11-15

Registro histórico das primeiras etapas. Para a implementação posterior ao prompt mestre, consulte [VALIDATION-2026-11-15](VALIDATION-2026-11-15.md) e [STATUS](STATUS.md); números e pendências abaixo refletem cada momento da fundação.

## Instalação autorizada

Script oficial https://dot.net/v1/dotnet-install.ps1, canal 10.0 GA, instalação sem administrador. Tentativa em C: interrompida por falta de espaço. Instalação concluída em D:\PHsystemTools\dotnet, SDK 10.0.404. Cache de pacotes separado em D:\PHsystemTools\nuget. Nenhuma mudança no PATH global.

## Resultados reais

- `dotnet --version`: 10.0.404 (executável no D:).
- `dotnet restore PHsystem.slnx`: 19 projetos restaurados.
- `dotnet build PHsystem.slnx --no-restore`: aprovado, 0 avisos, 0 erros.
- `dotnet test PHsystem.slnx --no-build --logger trx`: 2 testes unitários aprovados. Cinco projetos sem testes descobertos; não representam validação dessas áreas. Resultados em tests/*/TestResults, ignorados pelo Git.
- `dotnet publish src/PHsystem.Desktop -c Release -r win-x64 --self-contained true -o artifacts/desktop`: aprovado. Executável e dependências na pasta; não é single-file nem instalador.
- Executável publicado iniciado com sucesso: WaitForInputIdle=True, HasExited=False, MainWindowTitle=PHsystem Next, MainWindowHandle diferente de zero. Janela mantida aberta. Não equivale a avaliação visual, DPI ou acessibilidade.
- Parsing estrutural aprovado de 24 arquivos XML e 6 JSON antes da compilação.
- Workspace VS Code aberto por comando e configurado para SDK/cache no D:.
- Git inicializado, sem remoto. Commit pendente por ausência de identidade Git.

## Ainda não validado

Máquina limpa, Windows 10, DPI, UI automatizada, integração, segurança, PostgreSQL, CI remoto, assets e instalador. M0 ainda não está concluído.

## Validação após transferência para D:

Workspace em D:\phsystem-next; Git preservado. Conferência SHA-256 dos 1.969 arquivos restantes aprovada. Build completo no destino: 0 avisos, 0 erros; 2 testes unitários aprovados. Cópia residual no C: mantida porque a revisão automática rejeitou o comando de exclusão recursiva.

## Continuação M0 — AssetProvider local

- SDK usado: `D:\PHsystemTools\dotnet\dotnet.exe`; cache NuGet em `D:\PHsystemTools\nuget`, telemetria CLI desativada.
- `dotnet restore PHsystem.slnx`: aprovado para os 19 projetos.
- `dotnet build PHsystem.slnx`: aprovado, 0 avisos e 0 erros após as alterações finais.
- `dotnet test PHsystem.slnx --no-build --logger trx`: 22 testes aprovados, 0 falhas, 0 ignorados. São 2 casos de snapshot e 20 de assets (incluindo linhas de Theory). TRX final: `tests/PHsystem.UnitTests/TestResults/PH_PH_2026-11-15_18_07_48_net10.0.trx`.
- Casos de assets verificam fallback/retentativa, leitura, cache e invalidação, limite agregado de cache, caminhos externos/traversal/ADS, ausência de cadastro, arquivo vazio/grande, versão do catálogo, entradas inválidas/duplicadas e nomes de dispositivos Windows.
- Os projetos IntegrationTests, SecurityTests, UiTests, E2ETests e WindowsTests continuam sem testes descobertos. Os casos de caminhos estão na suíte unitária, sem alegação de auditoria de segurança.
- `dotnet publish src/PHsystem.Desktop -c Release -r win-x64 --self-contained true -o artifacts/desktop`: aprovado após as alterações finais. Catálogo copiado para `artifacts/desktop/assets/assets-manifest.json`, 5.101 bytes. Este comando gera um pacote local; não instala nem distribui externamente.
- O provider está registrado no DI, mas a UI ainda usa texto/geometria e não o consome. Não houve nova avaliação visual ou execução da janela nesta etapa.

Contrato: apenas caminhos relativos cadastrados com extensões PNG/SVG/WebP/WAV; limite de 4 MiB por arquivo e 16 MiB no cache. Cache retorna bytes imutáveis e permanece válido até `ClearCache` ou descarte por orçamento; ausências/erros não são cacheados. Arquivo ausente, inclusive `required`, retorna estado Missing e fallback; `required` permanece metadado do catálogo. Arquivos inválidos/restritos retornam Rejected; catálogo inválido impede construir o provider.

Limites: o provider entrega bytes inertes e não valida assinaturas nem decodifica formatos. A verificação de reparse points está implementada, mas não foi testada com junctions/symlinks; não garante isolamento contra troca concorrente de diretórios por outro processo. Renderização segura, codecs, testes de links, arquivos reais e integração visual permanecem pendentes.
