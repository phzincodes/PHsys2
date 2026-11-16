# Incremento de comunidade e perfis — 2026-11-16

Implementado no código: publicações tipadas com chave de idempotência; seguimento individual; edição/exclusão de respostas com controle de versão e retirada da solução; rascunhos desktop protegidos por DPAPI e separados por conta; editor web com prévia Markdown sem HTML executável; perfil público somente por opt-in; compartilhamento sanitizado de saúde autodeclarada e ranking separado da reputação; moderação e administração com autorização no servidor, MFA e confirmação de senha nas ações críticas. O cabeçalho de identidade esperada impede envio de conteúdo privado de uma aba antiga para outra conta; ele não concede permissões.

Evidências: build da solução sem avisos/erros; suíte final 183 aprovados e dois PostgreSQL ignorados nessa rodada, seguida de execução dos cinco testes de integração com PostgreSQL exclusivo: cinco aprovados, nenhum ignorado. São185 testes distintos aprovados considerando ambas as execuções. Publish self-contained win-x64 em `artifacts/community-preview`. Logs privados: `artifacts/community-tests-final.log`, `community-postgres.log`, `community-publish.log`. JavaScript validado com node --check. Migração aditiva CommunityProfilesAndModeration gerada; aplicada e exercitada no banco exclusivo de testes.

Limitações: navegador real e matriz física DPI não validados neste incremento; Google real, e-mail externo, assinatura, instalador em VM limpa, atualizador, integridade DISM/SFC, anexos/avatares e cobertura avançada continuam pendentes. Administração implementada não equivale a auditoria de segurança completa. Nenhuma publicação remota ou instalação executada. O build Docker padrão apresentou biblioteca compartilhada corrompida; a execução explícita com builder phsystem-clean compilou API/migrate. Banco local preservado com backup privado; migração aditiva aplicada. HTTPS /health respondeu ready, migrationState current, API0.3.0/build b0159020388e; cadastro/login/recuperação disponíveis. Isso não comprova entrega externa de e-mail.

---

# Evidências da continuação 0.3.0

- Restore/build Release: sucesso após corrigir o uso de Thickness com dois argumentos; zero avisos/erros no build corrigido.
- Primeiro teste encontrou duas expectativas antigas de15 coletores. Atualizadas para18 sem esconder falhas de coleta. Suíte completa com PostgreSQL de teste exclusivo: **179 aprovados,0 falhas,0 ignorados** (Unit58, Security55, Ui59, Integration5, Windows1, E2E1). TRX continuation-complete.trx.
- Depois: um teste de fluxo Google nativo, um de Google indisponível no desktop, um de navegação acessível e um de firewall ativo adicionados. UI completa61 aprovados (continuation-accessibility.trx), Google5 aprovados, saúde6 aprovados. **183 testes distintos passaram entre execuções**, sem alegar suíte única de183.
- Google usa transporte simulado, não conta/credencial real: cookie browser separado, retorno por PKCE, segredo/verifier fora da URL, replay recusado, sem senha local, email não verificado recusado e colisão sem vinculação.
- Teste UIA externo inicialmente não encontrou HomeAnalyze apesar de botão visível. Causa: peer padrão do TabControl buscava cabeçalhos que o PageHost não renderiza. AccessiblePageHost corrige o conteúdo exposto. Regressão e UIA externa confirmaram o botão e iniciaram coleta real. Nova captura privada continuation-after-scan.png;18 categorias no SQLite e fatos parciais reais dos três novos coletores. Navegação nativa às abas Saúde/Travamentos capturada sem executar reparo.
- A primeira captura mostrou o botão principal abaixo da dobra; movido ao topo e XAML validado. Nenhuma captura foi fabricada ou copiada para site/GitHub. Relatório e imagens podem conter contexto privado: artifacts ignorado.
- Revisão do firewall: somente perfis ativos afetam pontuação; perfil inativo desabilitado tem regressão e não penaliza. Score completo continua indisponível pela falta de integridade; a faixa ponderada é explícita, não normalizada.
- node --check site/app.js passou; consulta NuGet sinalizou0 vulnerabilidades conhecidas. Não é auditoria completa.
- Inno Setup existente (assinatura do compilador válida) gerou instalador de prévia self-contained; nenhuma ferramenta foi instalada e o instalador não foi executado. Arquivo final: 49218914 bytes, assinatura ausente, SHA-256 a4a1d41d00388c50b148db367292a3af33ef79df6401ede886808d0d16ec5562.
- BuildInstaller.ps1 publica, compila e cria receipt/checksum. Download local HTTPS foi obtido e comparado ao hash do receipt. A API/localweb0.3.0 respondeu ready/current; providers Google retornaram false/false por ausência de configuração. Imagem Linux compilada e somente API local recriada, preservando banco/volumes.
- Depois da geração final, o hash deve ser consultado em artifacts/releases/release.json ou /api/v1/releases/current: nova compilação altera o binário. Fonte de app pode ter commit anterior aos commits finais; MVID/build efetivo distingue os pacotes.

Sem teste de instalação limpa/upgrade/desinstalação/assinatura, browser real, Google real ou envio SMTP externo. Não houve correção privilegiada, limpeza pessoal, publicação, CI remota nem implantação pública. Faltam também funcionalidades de código, não apenas credenciais: central profunda de reparos, updater, ranking/score público, perfil/admin/comunidade completos. Essa entrega não encerra todo o prompt.

---

# Validação Red Eclipse — 2026-11-16

Branch feat/red-eclipse, partindo de adb0050. Alterações do usuário em global.json e StartDevelopment.ps1/backup preservadas. PR #1 consultado novamente: draft aberto, head 8ce6661, sem merge/push. Executável anterior em uso era artifacts/development; as prévias novas têm saída separada para não encerrar a sessão do usuário.

## Comandos e resultados V3

- restore e build Release: sucesso, 0 avisos/0 erros.
- Primeiro dotnet test: duas falhas — cores antigas esperadas no teste e manifesto static-assets apontando wwwroot inexistente. Cores esperadas atualizadas para a nova especificação; geração de manifesto desabilitada porque o provedor físico restrito serve os arquivos já copiados. Regressões repetidas passaram.
- dotnet test PHsystem.slnx -c Release --logger trx;LogFileName=red-eclipse-complete.trx: **164 aprovados, 0 falhas, 0 ignorados**. Unit 52; Security 49; Integration 5; Ui 56; Windows 1; E2E 1. PostgreSQL exclusivo phsystem_test_* configurado somente no processo de teste, sem tocar o banco de contas em uso.
- Depois da última correção do presenter, teste novo STA comprovou o rótulo do seletor. dotnet test UiTests --filter RedEclipseTests: **8 aprovados**, incluindo esse novo caso (165 testes distintos passaram entre as execuções). Nenhuma alegação de 165 em uma única suíte completa.
- node --check site/app.js: sucesso. Testes HTTP de /, /entrar, /cadastro, /comunidade, /download, /privacidade, CSS/JS e autorização real passaram. /AGENTS.md permanece 404.
- dotnet publish Desktop -c Release -r win-x64 --self-contained true -o artifacts/desktop: sucesso. Prévia Debug publicada em artifacts/red-eclipse-rc com --artifacts-path separado; abriu e respondeu, captura real privada inspecionada.
- docker build --builder phsystem-clean --load -t phsystem-next-eclipse -f src/PHsystem.Server/Dockerfile .: ASP.NET Linux+frontend publicados. Somente API local recriada; banco/volumes preservados, sem migração nova necessária. /health em https://localhost:5178 retornou ready/current, build 71ab66fcdb17; landing retornou 200 com TLS normal.

## O que estes resultados comprovam

Persistência de presets/customização local e rejeição de cores/contraste/JSON perigoso; painel medido em larguras lógicas correspondentes a 1280/1366/1920/2560/3840 e escalas 1/1,25/1,5/2, sem sobreposição no componente; rótulo correto no ComboBox WPF real. A matriz é lógica e não equivale a monitores físicos/DPI/leitores de tela completos.

Os fluxos TLS/Identity/curtidas e concorrência PostgreSQL existentes continuam passando. Não significa que as novas telas JS foram executadas em navegador. EmailLinkTests verifica token no fragmento e recusa localhost em produção, sem enviar email externo.

Capturas artifacts/eclipse-dashboard.png e eclipse-final-dashboard.png: desktop real. A primeira evidenciou o espaço lateral e cobertura insuficiente; painel adaptativo e texto foram corrigidos, nova captura inspecionada. Nada foi publicado; hardware da imagem é privado.

A aprovação automática rejeitou abertura do navegador com depuração e abertura normal, com motivo genérico blocked by policy. Não houve bypass ou screenshot web inventado. Site está servindo por HTTPS; validação visual browser/mobile, formulário e sincronização completa web/desktop permanecem pendentes. Abra https://localhost:5178/ para revisão manual.

Google, entrega SMTP externa, score completo, Defender scan profundo, instalador/updater/assinatura/instalação limpa, biblioteca completa de personalização e fases A–F integrais continuam pendentes. Não se declara produto pronto ou primeiro marco integral concluído.

---

# Histórico V2

# Validação atual — V2, 2026-11-16

Ambiente Windows, SDK em `D:\PHsystemTools\dotnet`, NuGet em `D:\PHsystemTools\nuget`. Telemetria CLI desabilitada. Baseline: 135 aprovados/1 PostgreSQL ignorado. O aplicativo Debug anterior estava aberto e bloqueava suas DLLs; ele não foi encerrado. Validação nova usa Release e uma prévia Debug com saída isolada.

## Comandos e resultados reais

Comando | Resultado
---|---
`dotnet restore PHsystem.slnx` | sucesso
`dotnet build PHsystem.slnx -c Release --no-restore` | sucesso, 0 avisos/0 erros
`dotnet test PHsystem.slnx -c Release --no-restore --logger "trx;LogFileName=v2-complete.trx"` | 155 aprovados, 0 falhas, 0 ignorados, PostgreSQL exclusivo configurado somente no processo de teste
`dotnet publish src/PHsystem.Desktop -c Release -r win-x64 --self-contained true -o artifacts/desktop` | sucesso
`./tools/PublishDevelopment.ps1 -Dotnet D:\PHsystemTools\dotnet\dotnet.exe` | prévia Debug self-contained publicada em artifacts/development; saída intermediária isolada evita bloquear o Debug da IDE
`./tools/TestDevelopment.ps1 -Builder phsystem-clean` | Docker 29.9.3, Compose 5.7.0, builder existente saudável; sem reset/prune
`docker build --builder phsystem-clean --load -t phsystem-next-v2-validation -f src/PHsystem.Server/Dockerfile .` | publish ASP.NET Linux e imagem concluídos
`docker run --rm --network none --entrypoint sh phsystem-next-v2-validation -c 'id; test -w /home/app/.aspnet/DataProtection-Keys && echo keys-writable'` | UID/GID 1654, diretório gravável sem root e sem rede

`dotnet` acima representa o SDK local indicado. TRX ficam em `tests/<projeto>/TestResults/v2-complete.trx` ignorados pelo Git.

Projeto | Aprovados | Falhas | Ignorados
---|---|---|---
UnitTests | 52 | 0 | 0
SecurityTests | 47 | 0 | 0
IntegrationTests | 5 | 0 | 0
UiTests | 49 | 0 | 0
WindowsTests | 1 | 0 | 0
E2ETests | 1 | 0 | 0

Sem `PHSYSTEM_TEST_POSTGRES`, os dois cenários PostgreSQL são corretamente ignorados. A execução completa acima usou banco novo `phsystem_test_*` no PostgreSQL de desenvolvimento, sem apontar testes para `phsystem_dev`; fixture limpa seus usuários/posts. Credenciais foram lidas do arquivo privado externo e não registradas. A base de teste permanece separada, sem remover volumes.

## O que foi comprovado

- WPF real em STA: shell offline sem consulta online, varredura única, persistência SQLite e preservação do snapshot ao perder sessão.
- Limpeza em fixtures: zero legítimo, fronteira exata de sete dias/TTL, protegidos, hardlinks/junctions, arquivo aberto, substituição com mesmo nome/tamanho/data, limite de entradas e seleção segura. UI executa somente arquivos de teste com confirmação. Nenhum temporário pessoal foi removido.
- Identity/TLS: cadastro/confirmação/recuperação e restauração/revogação já existentes, mais curtida/retirada desktop→Kestrel→Identity com dois usuários. Clique repetido bloqueado, contagem confirmada, contexto preservado e logout limpa a conta.
- PostgreSQL real: migrations/Identity e oito escritas concorrentes idempotentes, remoções, bloqueio, conteúdo próprio/oculto e leitura em conexões novas. Isso não comprova reinício do servidor ou carga pública.
- HTTP: códigos diferenciados, correlação gerada pelo servidor sem refletir identificador arbitrário, detalhes privados omitidos e cooldown Retry-After sem nova requisição nem retry de mutação.

## Aplicação e API em execução

A imagem local foi atualizada por `docker compose ... up -d --no-deps --no-build api`, preservando PostgreSQL, Mailpit e volumes. Nenhuma migration destrutiva foi aplicada ao banco em uso. A porta foi redescoberta por Compose, `/health` validado por TLS normal e descoberta Debug renovada fora do Git. Health retornou `ready`, protocolo 1, versão 1.0.0, build `967ab4c816ce`, migrations `current`, cadastro/login disponíveis. Isso comprova saúde local, não entrega SMTP externa nem produção.

Prévia `artifacts/development/PHsystem.Desktop.exe` abriu e respondeu com diagnóstico local visível sem conta. Atalho **PHsystem Next - Desenvolvimento** criado na área de trabalho para essa prévia. Captura real privada `artifacts/v2-final-local.png` foi inspecionada; não é mockup. Capturas anteriores `v2-before.png`/`v2-current.png` registram contexto, mas não comprovam execução manual de todos os fluxos novos.

Os pacotes foram gerados antes do commit final, portanto commit de origem pode anteceder o código não commitado da compilação; o identificador do módulo/build efetivo distingue esses binários. Consulte Configurações para comparar. O build Release não consulta descoberta de desenvolvimento e ainda não contém domínio de produção configurado.

## Limitações

Não houve login com conta real do usuário nem teste de envio externo. Não foram executados reparos, drivers, instalação limpa, atualização, assinatura, matriz física DPI/4K/leitores de tela, CI remota ou deploy público. O site permanece estático; Google, links automáticos de e-mail, quarentena e central completa de reparos não estão entregues. Estes limites não são testes aprovados.

## Correção final de proveniência

O coordenador ainda gravava versão fixa0.2.0. Agora coleta versão+MVID dos módulos reais e invalida cache com versão diferente. Doze testes do coordenador passaram, incluindo manutenção do cache na mesma versão e recusa de versão legada. Snapshots anteriores foram preservados, sem reescrever sua história. Instalador e prévia foram regenerados; atalho aponta para artifacts/final-preview. O hash acima corresponde a essa geração.
