# Incremento de comunidade e perfis — 2026-11-16

Implementado no código: publicações tipadas com chave de idempotência; seguimento individual; edição/exclusão de respostas com controle de versão e retirada da solução; rascunhos desktop protegidos por DPAPI e separados por conta; editor web com prévia Markdown sem HTML executável; perfil público somente por opt-in; compartilhamento sanitizado de saúde autodeclarada e ranking separado da reputação; moderação e administração com autorização no servidor, MFA e confirmação de senha nas ações críticas. O cabeçalho de identidade esperada impede envio de conteúdo privado de uma aba antiga para outra conta; ele não concede permissões.

Evidências: build da solução sem avisos/erros; suíte final 183 aprovados e dois PostgreSQL ignorados nessa rodada, seguida de execução dos cinco testes de integração com PostgreSQL exclusivo: cinco aprovados, nenhum ignorado. São185 testes distintos aprovados considerando ambas as execuções. Publish self-contained win-x64 em `artifacts/community-preview`. Logs privados: `artifacts/community-tests-final.log`, `community-postgres.log`, `community-publish.log`. JavaScript validado com node --check. Migração aditiva CommunityProfilesAndModeration gerada; aplicada e exercitada no banco exclusivo de testes.

Limitações: navegador real e matriz física DPI não validados neste incremento; Google real, e-mail externo, assinatura, instalador em VM limpa, atualizador, integridade DISM/SFC, anexos/avatares e cobertura avançada continuam pendentes. Administração implementada não equivale a auditoria de segurança completa. Nenhuma publicação remota ou instalação executada. O build Docker padrão apresentou biblioteca compartilhada corrompida; a execução explícita com builder phsystem-clean compilou API/migrate. Banco local preservado com backup privado; migração aditiva aplicada. HTTPS /health respondeu ready, migrationState current, API0.3.0/build b0159020388e; cadastro/login/recuperação disponíveis. Isso não comprova entrega externa de e-mail.

---

# Auditoria e correções V2 — 2026-11-16

## Proveniência e proteção do trabalho existente

- Branch inicial `fix/api-auto-connection`, HEAD `b372bf7`. Sem remoto configurado no Git local. GitHub privado `phzincodes/PHsys` acessível pelo conector; main remoto difere desta implementação. PR #1 aberto/draft, `fix/windows-validation`, head `8ce6661`, contém outro motor de limpeza/quarentena. Não houve merge nem substituição de fontes locais.
- Alterações preexistentes preservadas: `global.json`, `tools/StartDevelopment.ps1` e backup não versionado `StartDevelopment.ps1.backup-20261116-022810`. O script contém uma correção legítima de parsing da porta por Regex.Match; não será revertida.
- Processo em execução: PID 16936, Desktop **Debug**, versão `1.0.0+b372bf70a08abb8cef09786d86781e875acf3608`, SHA-256 da DLL `CE236C344FF48ADB69E97E37C0AFBFF6A5A0E926943A24A02282A3804B34DE2E`. Não presumir igualdade com ZIP/Release/site remoto. Captura privada `artifacts/v2-before.png` reproduz 0 elegíveis/246 ignorados e mensagem contraditória.
- Baseline Debug encontrou DLLs bloqueadas pelo aplicativo aberto. Não foi encerrada a sessão do usuário: baseline Release executado com **135 aprovados, 1 PostgreSQL ignorado**, nenhuma falha. Testes de UI/API usam fixtures; não comprovam votação de contas reais no banco em uso.
- Ambiente atual: Docker 29.9.3, API/PostgreSQL/Mailpit ativos, portas loopback dinâmicas; `/health` retornou `ready` por TLS normal. Builder selecionado `phsystem-clean`; volume de chaves gravável por UID 1654. Não remover builder/cache/volumes nem alterar permissões às cegas. Documentação antiga de Docker ausente está desatualizada.
- PR e issues foram lidos, sem comentário, push, publicação ou mutação remota. Não copiar artefatos temporários do PR como distribuição permanente.

## Problemas prioritários e reprodução

Sintoma | Evidência / reprodução | Causa confirmada ou hipótese | Arquivos | Severidade | Correção / teste / resultado
---|---|---|---|---|---
Diagnóstico exige conta | Abrir sem cookie/API; shell inicia oculto | Gate global de autenticação; falha online oculta operações locais | Desktop/MainWindow.xaml(.cs), CommunityShell | P0 | Separar identidade online do shell local; teste offline sem requisição aprovado; shell permanece local no logout
0 arquivos mas pede analisar/revisar/excluir | Captura real e CleanupFiles.Count=0 após prévia | Mesmo estado vazio para não analisado e concluído; mensagens incondicionais | Desktop/Views/CleanupView.xaml; MainViewModel | P1 | Estados explícitos, razões de exclusão, escopo e ações condicionais; testes de UI e filesystem aprovados
246 ignorados sem motivos | PreviewAsync soma todas as razões em Skipped | Scanner só arquivos no primeiro nível, >7 dias, não mede motivos/subpastas/limite | Cleanup/TempCleanupService | P1 | Contadores coerentes sem ampliar escopo; regressões de fronteira, links e recentes
Bytes removidos confundidos com espaço livre | Resultado soma tamanhos de arquivos deletados | FreedBytes nominal, sem medição de volume | Cleanup/TempCleanupService; WindowsDeletionScope | P1 | Distinguir bytes processados de variação observada; quarentena não pode declarar espaço liberado
Voto recarrega todo feed e pode perder contexto | Vote faz PUT e LoadFeedAsync(true) | Reset de paginação e DTO antigo selecionado; estado visual não mostra MyVote | Desktop/SocialViewModel; Components/PostCard | P0/P1 | Testar clique real e atualizar item confirmado, evitar contagem otimista falsa
Votos concorrentes podem conflitar | FindAsync seguido de insert, índice composto | Corrida confirmável entre requests; 409 genérico no insert duplicado | Server/SocialEndpoints | P1 | Escrita idempotente/controle transacional e teste A/B, bloqueio/oculto/próprio
Erros HTTP pouco acionáveis | 409/422/500 usam texto genérico; 429 ignora Retry-After | Cliente não preserva código/correlação/espera | Desktop/CommunityClient; Server/Program | P1 | ProblemDetails sanitizado e correlação, sem tratar 401/403 como queda de API
Infra depende de builder específico | Docker usa phsystem-clean; script não tem seleção explícita | Histórico de BuildKit não reproduzido nesta execução; chaves agora graváveis | tools/StartDevelopment.ps1; Server/Dockerfile; compose | P1 | Preflight/seleção explícita sem limpar dados; preservar correção de porta do usuário
Sem identificação visível de build | Versão só em metadata da DLL | UI/API não apresentam proveniência/compatibilidade suficiente | Desktop/SettingsView; SharedContracts; Server/ServiceHealth | P1 | Versão/commit/contrato, sem paths/segredos; testes de protocolo
Site só estático | site/index.html e guia.html | Não existe plataforma web autenticada compartilhada | site; Server; Admin | P2 | Avaliar Razor Pages no backend existente; não chamar landing page de comunidade pronta

## Limites da execução

Nenhuma limpeza/reparo foi executada nos arquivos do usuário. Testes destrutivos ficam em diretórios/bancos exclusivos de teste. Produção, Google OAuth, SMTP externo, instalador limpo/assinatura e acessibilidade física continuam dependências ou etapas futuras até haver evidência. Resultados desta entrega serão adicionados após cada validação.

## Resultado confirmado da entrega

- Limpeza: causa de zero elegíveis não foi considerada bug. Contadores/estados e explicação foram corrigidos mantendo o escopo. Nova prova nativa de identidade protege substituição entre prévia e execução; teste de arquivo em uso encontrou e corrigiu acesso insuficiente no probe antes da entrega.
- Reações: escrita atômica testada contra PostgreSQL real com oito conexões; cliente desktop real sobre TLS/Identity confirma curtida/retirada sem resetar feed. Solução e revisão continuam distintas. Não foi necessário alterar schema/migrations para os DTOs e operações novas.
- API: respostas sanitizadas/correlacionadas, 401/403 separados de indisponibilidade e Retry-After respeitado. Health/About identificam versão/build/contrato. Health da API local nova retornou ready/current após atualização preservando volumes e dados.
- Infra: builder existente funciona, falha antiga de armazenamento não foi reproduzida; não se pode declarar causa corrigida por apagar caches. Preflight read-only adicionado, imagem testada sem root. Dockerfile corrige ownership para diretórios/volumes novos; volumes existentes não foram alterados. O script de porta do usuário segue intacto.
- Prévia separada usa descoberta Debug automática e abriu sem conta; atalho específico criado sem substituir o atalho anterior. Imagens privadas em artifacts, nenhuma publicação.
- Validação final: **155 aprovados, zero falhas e zero ignorados**, restore/build/publish Windows e imagem Linux concluídos. Comandos e limites em [VALIDATION](VALIDATION.md).

As fases V2 2–6 permanecem parciais/pendentes: não há plataforma web autenticada, Google, instalador validado ou central integral de reparos nesta entrega. A divergência do PR #1 exige revisão futura; nenhum merge/push foi feito.

## Auditoria V3 — Red Eclipse

Sintoma | Causa/evidência | Arquivos/correção | Prova/limite
---|---|---|---
FeedOption aparece | presenters não propagavam selector/template/string format | Theme.xaml, regressão RedEclipseTests | controle STA mostra Recentes sem nome interno
Identidade teal fixa | brushes e foregrounds hardcoded | ThemeManager, UiPreferences, Theme, App, Settings | seis presets validados, persistência/import seguro; editor completo pendente
Dashboard vazio | hero isolado e larguras fixas | OverviewView, ResponsivePanel, ShellViewModel | captura real e matriz lógica, sem dados fictícios
Sem alertas com cobertura zero | resumo só contava achados | FindingsSummary agora explicita cobertura insuficiente | captura final real
Site antigo | landing estática sem host integrado | site/*, PublicWeb, csproj/Dockerfile/Compose | localhost5178 retorna site200, mesma API; navegador bloqueado
Links email manuais | corpo só ID/token | AccountEmailContent, IdentityServices, endpoints | fragmentos/testes e HTML/texto; entrega externa pendente
Web Development falhou no primeiro teste | manifest gerado com root inexistente | StaticWebAssetsEnabled=false e physical provider limitado | regressão ambiente sem banco passou

Não houve mudança destrutiva em dados ou credenciais. PR #1 segue divergente/draft; não foi importado. Falhas visuais de todas as demais telas não são declaradas resolvidas por essa amostra.

Na revisão final, aplicar cor inválida de tema expunha InvalidDataException fora do filtro de erro de UI. Incluído explicitamente no filtro; regressão do clique confirma mensagem e preservação do tema salvo. Oito testes RedEclipse repetidos com sucesso. Pronto/Completo usam verde semântico independente do vermelho de marca.

## Continuação0.3.0 — causas e correções verificadas

Problema | Causa confirmada | Arquivo/classe | Correção e prova
---|---|---|---
Botão não exposto à automação apesar de visível | peer padrão TabControl depende de headers que PageHost omite | AccessiblePageHost, MainWindow, RedEclipseTests | peer de conteúdo selecionado; teste e UIA externa encontraram/acionaram HomeAnalyze
Ação analisar abaixo da dobra com widgets | hero ficava após os módulos | OverviewView | ação movida ao topo; captura real e carga XAML passaram
Modelo não separava pontos conhecidos de ausentes | inexistência de motor/rubrica | Core/HealthAssessment | faixa ponderada, validade/confiança/fatos; integridade não inventada, testes de fronteira/ausência
Firewall inativo poderia penalizar saúde | avaliar todos os perfis em vez dos atualmente ativos | SecurityEvidence, HealthAssessmentEngine, FindingEvaluator | ActiveProfilesEnabled via CurrentProfileTypes; regressão de perfil inativo passou
Sem investigação orientada de logs | inventário só listava50 eventos sem agrupamento | StabilityEvidence, CrashInvestigator, HealthViewModel | janela7dias/limite500, timeline40, correlação explícita sem causalidade; fixture e navegação real
Google só pendência de configuração sem infraestrutura | nenhum fluxo de provedor | GoogleAuthentication, NativeLoginBroker, GoogleLoginViewModel | OAuth/PKCE/sub/email verificado, sem vincular por email; mocks de Identity/transporte e dois clientes, uso único/expiração
Sem instalador disponível apesar de fonte existente | ferramenta não era detectada no PATH | BuildInstaller, PHsystem.iss, ReleaseDownloads | compilador encontrado por caminho instalado/assinatura válida; executável gerado e download/hash real; instalação não foi testada
Falhas iniciais de compilação/teste | Thickness com2args e contagens antigas15 | ThemeManager, CollectorTests, LocalWorkflowTests | quatro argumentos e18 coletores; build/suíte corrigidos

Fonte/dados do usuário preservados. Cada ferramenta assistida continua distinta de reparo concluído. A geração do instalador não resolve updater, assinatura, API pública ou VM. Produção e o prompt integral não estão concluídos.

Proveniência final: ScanCoordinator ainda usava string0.2.0 fixa. Substituída por versão/build real dos módulos dos coletores e cache condicionado à mesma versão; regressão no ScanCoordinatorTests passou (12 testes). Histórico existente não foi adulterado.
