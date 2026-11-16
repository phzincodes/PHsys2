# Incremento de comunidade e perfis — 2026-11-16

Implementado no código: publicações tipadas com chave de idempotência; seguimento individual; edição/exclusão de respostas com controle de versão e retirada da solução; rascunhos desktop protegidos por DPAPI e separados por conta; editor web com prévia Markdown sem HTML executável; perfil público somente por opt-in; compartilhamento sanitizado de saúde autodeclarada e ranking separado da reputação; moderação e administração com autorização no servidor, MFA e confirmação de senha nas ações críticas. O cabeçalho de identidade esperada impede envio de conteúdo privado de uma aba antiga para outra conta; ele não concede permissões.

Evidências: build da solução sem avisos/erros; suíte final 183 aprovados e dois PostgreSQL ignorados nessa rodada, seguida de execução dos cinco testes de integração com PostgreSQL exclusivo: cinco aprovados, nenhum ignorado. São185 testes distintos aprovados considerando ambas as execuções. Publish self-contained win-x64 em `artifacts/community-preview`. Logs privados: `artifacts/community-tests-final.log`, `community-postgres.log`, `community-publish.log`. JavaScript validado com node --check. Migração aditiva CommunityProfilesAndModeration gerada; aplicada e exercitada no banco exclusivo de testes.

Limitações: navegador real e matriz física DPI não validados neste incremento; Google real, e-mail externo, assinatura, instalador em VM limpa, atualizador, integridade DISM/SFC, anexos/avatares e cobertura avançada continuam pendentes. Administração implementada não equivale a auditoria de segurança completa. Nenhuma publicação remota ou instalação executada. O build Docker padrão apresentou biblioteca compartilhada corrompida; a execução explícita com builder phsystem-clean compilou API/migrate. Banco local preservado com backup privado; migração aditiva aplicada. HTTPS /health respondeu ready, migrationState current, API0.3.0/build b0159020388e; cadastro/login/recuperação disponíveis. Isso não comprova entrega externa de e-mail.

---

# Continuação Red Eclipse 0.3.0 — estado atual

Entrega local incremental; o prompt inteiro NÃO está concluído. Fontes anteriores abaixo são históricas quando contradizem este bloco.

Área | Código/funcionamento demonstrado | Validação | Falta real
---|---|---|---
Personalização | biblioteca até20 temas, cores/superfícies/textos/bordas, fonte/raio/densidade/sidebar, ordem e visibilidade dos quatro widgets persistentes; saúde/alertas não ocultáveis | testes de persistência, import perigoso e widgets | sincronização opt-in e editor de animação/detalhamento completos
Diagnóstico | 18 coletores, novos Defender/firewall/estabilidade; snapshot local único com versão real de módulo/cache compatível | scan pela interface real,18 categorias persistidas, três coletores novos com fatos parciais | sensores/SMART/WUA abrangentes, DISM/SFC, medição/performance de todos os módulos
Saúde | pesos/regras auditáveis, evidência expirada/ausente desconhecida, intervalo e comparação histórica | testes de limites, pesos, dados inválidos e perfis ativos | nota única completa depende da integridade não implementada; não há score público/ranking
Crash Investigator | até40 eventos, janela7dias/limite500 por log, proximidade temporal sem inferir causa | fixture e UI com dados locais reais | dumps/BSOD/correlação avançada e validação em VMs
Relatório reduzido | HTML local sem fatos/identificadores/logs brutos | teste contra vazamento de sentinelas | revisão seletiva por campo e formatos adicionais
Acessibilidade | PageHost expõe conteúdo selecionado a UIA; ação analisar no topo | regressão e acionamento UIA externo real, navegação saúde/travamentos | matriz física DPI/4K e leitores de tela completos
Google | servidor OAuth/PKCE e broker nativo com proteção/replay, telas e estado indisponível honesto | mocks de transporte + Identity/cookies; testes de colisão e recusa de email não verificado | credenciais externas e teste real; vinculação/MFA externos e sessões por dispositivo não completos
Distribuição | instalador Inno self-contained compilado, pasta selecionável no script, metadados e download HTTPS local com hash conferido | build/local download real; não instalado | assinatura, VM limpa/update/desinstalação, updater e produção
Web/comunidade | mesma API/Identity/banco; download agora real, Google condicional, feed/reação existentes preservados | HTTP/contratos e testes anteriores | browser real, tipos de conteúdo, anexos, perfis/reputação completos, administração completa

Validação: 179 aprovados na suíte completa com PostgreSQL exclusivo; regressões subsequentes elevaram a cobertura a183 testes distintos aprovados em execuções separadas. UI completa61, Google5 e saúde6 repetidos após as correções pertinentes. Não se declara uma execução única de183. Fonte/outputs em VALIDATION.md.

Atalho Red Eclipse aponta para artifacts/final-preview/PHsystem.Desktop.exe. Site local: https://localhost:5178/. Instalador: /download/windows. API respondeu ready, versão0.3.0; domínio/SMTP/Google de produção NÃO configurados. Sem publicação/push, elevação, instalação do pacote ou alteração destrutiva em dados reais. global.json e StartDevelopment.ps1 do usuário preservados.

---

# Estado verificável — 2026-11-16

Entrega V3 parcial na branch `feat/red-eclipse`. A tabela V2 abaixo é referência histórica; o estado Red Eclipse atual está neste resumo. Nenhuma fase ampla ou produto completo é declarado pronto. [Auditoria](AUDITORIA-CORRECOES.md), [evidências atuais](VALIDATION.md) e [backlog](BACKLOG.md).

## Red Eclipse — implementação atual

- WPF: tema padrão vermelho/grafite central, seis presets, persistência offline, cor principal personalizada, nome, fonte/raio, import/export JSON validado e contraste 4,5:1. Biblioteca de vários temas, todos os tokens editáveis, densidade/sidebar/widgets e sincronização ainda pendentes.
- Dashboard: snapshot real, cobertura, achados, CPU/RAM e ações; painel responsivo medido. Sem score inventado. Sem alertas + baixa cobertura agora informa impossibilidade de concluir.
- Seletor: corrigidos ContentTemplate/selector/StringFormat dos presenters; teste WPF real comprova Recentes sem FeedOption. Navegação geral permanece e requer revisão visual completa.
- Rede/reparos: DNS e cinco pings somente por ação do usuário, timeout/cancelamento, perda/latência e limites; três destinos oficiais allowlist para Windows Security/Update/inicialização. Não é antivírus novo ou reparo privilegiado.
- Web: site real reformulado em https://localhost:5178/, servido pelo ASP.NET existente, API/Identity/PostgreSQL compartilhados, telas de conta, feed/interações/perfil/notificações. HTTP/CSS/JS e autorização testados. Navegador foi bloqueado pela aprovação automática; E2E browser/mobile não validado.
- Email: link por fragmento para confirmação/reset, texto+HTML, SMTP TLS configurável e origem pública exigida em produção. Mailpit só Development. Google/Gmail OAuth e entrega real ainda pendentes.
- Evidências: suíte completa 164 aprovados (PostgreSQL real), depois oito regressões RedEclipse aprovadas incluindo um teste novo do seletor, somando 165 testes distintos aprovados; nenhuma falha conhecida. Primeiro passe detectou duas falhas, corrigidas. Publish Windows/Linux concluídos.
- Prévia: artifacts/red-eclipse-rc/PHsystem.Desktop.exe abriu; atalho PHsystem Next - Red Eclipse criado. Captura real privada eclipse-final-dashboard.png. Produção, instalador/assinatura/updater/VM, score auditável, Google e central profunda não estão entregues.

O primeiro marco V3 é parcial: falta verificação visual web e matriz física das telas. Não foram concluídas todas as fases A–F. Ver WEB, EMAIL e VALIDATION para os limites exatos.

## Referência V2

Funcionalidade | Implementado | Testado localmente | Testado Windows | Pendente | Risco
---|---|---|---|---|---
Fundação | .NET 10, WPF/MVVM, monólito modular, VS Code/CI | restore/build Release | sim | CI remota | Debug aberto bloqueia recompilação de DLLs; não foi encerrado
Acesso local | shell sem conta, restauração opcional da sessão online, logout sem perder snapshot | teste WPF offline sem requisição e sem varredura duplicada | sim | matriz física DPI/leitor de tela | testes lógicos não comprovam toda acessibilidade
Diagnóstico/histórico | 15 categorias, snapshot compartilhado, SQLite, cancelamento e cobertura | simuladores e fluxo Windows→SQLite | sim | WUA/SMART/telemetria, atualização parcial, score validado | sem score perfeito por ausência de dados
Limpeza | escopo temporários de primeiro nível >7 dias, razões/contadores, seleção, confirmação, resultado por arquivo e identidade nativa | fixtures de limite, links, hardlinks, bloqueio, substituição, TTL e UI | sim; sem excluir dados reais | quarentena, caches revisados/lixeira | permanente e irreversível; variação livre observada não é prova de atribuição
Armazenamento | análise de pasta/maiores e hash opt-in | fixtures | sim | árvore visual e mais matrizes ACL | paths privados locais
Drivers/otimização | inventário, política de evidências, regras/PH CORE e prioridade temporária limitada | simuladores/regras | inventário | instalação oficial/helper/rollback, medição e VM | não há reparo privilegiado operacional
Identidade/API | descoberta segura, health, Identity, TLS, CSRF, DPAPI, expiração/revogação/reset e erros correlacionados | Kestrel TLS real + Identity/SQLite; migrações/Identity PostgreSQL real | sim em fixtures | Google, confirmação por link, entrega externa, sessão/dispositivos completos | produção não provisionada
Comunidade | feed, posts, threads, solução, curtidas/salvos/seguidos, perfis/DM e denúncias existentes | cliente desktop→API com dois usuários; concorrência PostgreSQL | sim | web dinâmica, anexos, menções e push completo | curtida não equivale a revisão técnica; sem E2EE
Privacidade/admin | hardware privado, compartilhamento campo a campo; papéis/MFA/políticas e painel separado | autorização e isolamento HTTP | sim | console administrativo completo, auditoria externa/LGPD jurídica | nenhuma conta proprietária foi criada
Infraestrutura | Docker/Compose ativos; build Linux; volume novo preparado para UID não root | preflight e imagem isolada sem rede; API atualizada health ready/current | host Windows + container Linux | validar bootstrap inteiramente limpo, assinatura/instalador/produção | nenhum volume existente alterado
Site/distribuição | site estático, fonte Inno e publish self-contained | publish local | executável de prévia abriu | web autenticada, instalador/VM, updater e download público real | não é release pública nem pacote assinado
GitHub | conector leu repo privado/PR #1/issues | leitura real | não se aplica | reconciliar branches divergentes, CI/publicação autorizada | não houve merge, push, deploy ou publicação

**155 testes aprovados, 0 falhas e 0 ignorados**, incluindo PostgreSQL exclusivo. Resultados atuais e comandos exatos estão em [VALIDATION](VALIDATION.md). `global.json`, correções preexistentes de `StartDevelopment.ps1` e seu backup foram preservados. Capturas privadas ficam em `artifacts` ignorado. Os registros abaixo são históricos e não substituem esta tabela: a exigência antiga de login para diagnóstico foi removida pela especificação V2.

## Histórico de entregas anteriores

## Reformulação UX — marco 1 (2026-11-15)
- Componente `Components/IconControl.cs`: viewport lógico 24 × 24, limites reais da geometria incluindo traço, alinhamento central e arredondamento de layout.
- HUD: trilho de ícones de largura fixa; removido padding diferente ao recolher; template de grupo sem recuo herdado. Ícones das ações do cabeçalho reutilizam o componente.
- Paleta solicitada aplicada ao tema central. Alteração existente de global.json preservada fora dos commits.
- Evidência: build da solução 0 erros/0 avisos; testes UI 7 aprovados. O primeiro teste detectou um ContentSource inválido no GroupItem; corrigido e repetido com sucesso.
- Teste matemático de limites nas escalas 100/125/150/200%; isso NÃO comprova DPI físico nem a matriz completa de resoluções. Demais ícones dos templates e verificação visual permanecem pendentes.
- Próximo marco: entrada autenticada usando a API existente, sem simular sessão.

## Reformulação UX — marco 2, base autenticada (2026-11-15)
- Entrada real: `AuthenticationView`/`AuthenticationViewModel`, boas-vindas, cadastro, confirmação, login/MFA, recuperação e redefinição. Shell só é exibido após `/account/me`; respostas 401 e logout retornam à entrada.
- Cadastro exige confirmação de senha e aceite explícito. Backend rejeita ausência de aceite e registra versão dos termos na auditoria. PasswordBox não trunca senhas; limite 128 validado explicitamente; triagem comprometidas permanece no servidor.
- Cliente HTTPS compartilhado por DI, cookie HttpOnly; opção lembrar sessão usa DPAPI CurrentUser e revalida com a API. Sem senha/token de formulário persistido. Renovação explícita tem teto absoluto de 12h e preserva auth_time/MFA administrativo.
- Evidência: solução compila; 92 testes aprovados, 1 PostgreSQL ignorado. Inclui validação sem backend, confirmação/consentimento, limite de senha, cofre DPAPI adulterado/expirado e exigência de termos no backend. XAML carregado por teste STA real.
- Incidentes corrigidos: binding OneWay do indicador; leitura de arquivos com encoding Windows causou caracteres corrompidos, detectados por teste e reparados usando UTF-8 explícito. Nenhum teste está com falha conhecida.
- Pendente: fluxo desktop completo contra ambiente PostgreSQL/SMTP/TLS implantado, UX de link automático de e-mail, revisão jurídica dos textos, expiração de UI sem requisição e animação de entrada. Sem infraestrutura configurada o app informa indisponibilidade; não inventa conta nem permite bypass da entrada.

## Reformulação UX — marco 3, análise central (2026-11-15)
- Início substituído por PH CORE vetorial, botão único ANALISAR, estado, cancelamento e lista de coletores durante execução. Cabeçalho com segunda ação de análise não aparece no início.
- Resumo anterior preservado em Diagnóstico > Resumo, separado dos componentes/achados. Removidas setas Unicode desse resumo.
- ScanCoordinator publica eventos reais (fila, execução, resultado inclusive cache/erro). Progresso é número de coletores concluídos, não estimativa de tempo nem score de saúde.
- Coleta executa fora da thread da UI; snapshot persistido continua único. Conclusão abre os resultados; cancelamento/erro possuem estados próprios e preservam o snapshot anterior.
- Evidência: build 0 erros/0 avisos; testes unitários 52 aprovados e UI 13 aprovados, incluindo regressão do progresso/cache/falha. Outros testes: segurança 23, integração 3 + 1 ignorado, Windows 1, E2E 1. Recurso BoolVisibility local corrigido após o teste real de XAML detectar falta de recurso em carga isolada.
- Pendente: movimento decorativo, sons, teste completo da interação desktop autenticada em ambiente configurado e revisão visual de todas as escalas físicas.

## Reformulação UX — marco 4, resultados e navegação (2026-11-15)
- Abas Hardware, Segurança e Desempenho filtram as categorias do mesmo snapshot. Componente expansível CategoryResults reutilizado; fontes, expiração, estados e erros preservados. Sensores/placa-mãe ausentes e métricas não contínuas são declarados, sem números inventados.
- NavigationService com destinos tipados e validação; 14 destinos vinculados à navegação. Histórico permite selecionar duas análises e comparar fatos/estado sem substituir snapshot atual; ausências são identificadas como Sem dado.
- Configurações locais de movimento e som criadas com persistência JSON, limites de volume, silêncio e aviso se disco falhar; integração audiovisual segue no marco 5.
- Evidência: build 0 erros/0 avisos; testes UI 15 aprovados, incluindo navegação das 14 páginas, comparação imutável e validação de destinos. Não houve nova varredura ao navegar.
- Pendente: telemetria contínua, sensores, placa-mãe, gráficos avançados, inspeção física de DPI e refinamento visual completo dos cartões.

## Reformulação UX — marco 5, movimento e áudio (2026-11-15)
- Hover/clique 80–120 ms, fade de páginas/entrada 200 ms. Respeitam desativação no app, ClientAreaAnimation do Windows, alto contraste e visibilidade. Arco PH CORE gira apenas durante coleta visível e janela ativa; descarrega eventos ao sair.
- 10 sons PCM originais locais, síntese reproduzível em tools/create-interface-sounds.py. Catálogo atualizado; nenhuma arte/imagem gerada. Assets ausentes/rejeitados não quebram UI. Cache por hash, preload MediaPlayer, um som por vez e throttle 100 ms.
- Preferências de volume geral/notificações, interface, silêncio e movimento aplicadas; som desabilitado por padrão. Integração atual: clique, navegação, autenticação, início/conclusão/erro de análise. Notificações/respostas/painéis específicos ainda não disparam seus cues.
- Evidência: build 0 erros/0 avisos; 96 testes aprovados, 1 PostgreSQL ignorado. UI 16 inclui persistência, limites de volume e bloqueio de todos os cues por modo silencioso. Não foi realizada avaliação auditiva humana nem validação de todos os dispositivos de áudio.

## Reformulação UX — marcos 6 e 7, comunidade e privacidade (2026-11-15)
- Ambiente WPF integrado CommunityShell/CommunityFeedView/PostCard: feed paginado, pesquisa, recentes/relevância/seguidos/salvos/meus posts, criação, edição própria, remoção confirmada, respostas encadeadas (até 4 níveis), solução aceita, denúncia, perfis públicos e notificações persistidas de resposta/solução. Conta/mensagens/compartilhamento anteriores preservados.
- Backend real SocialEndpoints e entidades EF; migrações SocialFeedInteractions e ThreadedReplies geradas para PostgreSQL. Votos idempotentes, sem voto próprio, sem converter votos em revisão técnica. Histórico de edição privado ao autor, edição invalida aceite/revisão contextual. Categorias técnicas ampliadas. Nenhum post de demonstração no app.
- BOLA: edição, revisões, salvos e notificações validados no servidor. Perfil público só consulta a tabela de hardware compartilhado. Exportação/exclusão incluem novos dados sociais. Filtros do feed respeitam bloqueios.
- HardwareSharingPolicy rejeita padrões de IP/MAC/e-mail/UUID/caminho/credencial antes de compartilhar, tanto na prévia local quanto no backend; não é prova universal de ausência de PII. Logs brutos não são anexados automaticamente.
- Sessão: logout/401/exclusão limpam dados sociais em memória, mantendo snapshot local. Expiração retornada pelo servidor é acompanhada na UI a cada 30s. Cofre DPAPI opt-in permanece separado das preferências.
- Evidência: integração HTTP Identity + SQLite controlado testa 25 posts, páginas sem duplicação, voto repetido, salvos/seguidos privados, IDOR de edição/revisões/notificações, threads inválidas/profundidade, aceite sem selo técnico, perfil e exclusão de dados novos. Segurança 38 testes aprovados; UI 18 aprovados; demais unitários 52, integração 3 + 1 PostgreSQL ignorado, Windows 1, E2E 1.
- Limites: PostgreSQL/SMTP/TLS reais não implantados/testados nesta sessão. Upload/avatares seguem indisponíveis até storage, reprocessamento e quarentena seguros. Sem push SignalR na nova timeline (notificações atualizadas pelo botão); menções, seguir publicação individual, edição de respostas, reputação completa e painel de moderação ampliado pendentes. A API existente de revisão técnica/MFA/admin foi preservada, não se declara social completo.

## Validação final desta revisão — 2026-11-15

- Comandos reais: restore da solução, build sem restore, test sem build com TRX e publish Release/win-x64/self-contained em artifacts/desktop. Todos concluíram com código 0; 113 testes aprovados, 1 PostgreSQL ignorado, nenhuma falha.
- EF: has-pending-model-changes sem divergências; script PostgreSQL idempotente gerado em artifacts/community-migrations.sql. Não aplicado a banco real nesta sessão.
- Janela publicada iniciada (PID 33808), boas-vindas → Entrar → Voltar verificadas por UIAutomation. Capturas reais artifacts/ux-published-login.png e ux-published-welcome.png. Login autenticado em serviço implantado NÃO foi testado.
- Matriz lógica: 1366×768, 1600×900, 1920×1080, 2560×1440 × 100/125/150/200%. Centralização horizontal preservada nos modos expandido/recolhido e largura da barra de rolagem validada. Mudança de monitor/DPI físico não comprovada.
- Captura de shell vazio de teste separada de capturas da aplicação publicada; nenhuma fixture é conteúdo de produção. Testes STA serializados devido ao cache BAML global.
- VS Code reaberto no workspace. CI Windows prepara upload de TRX/capturas de teste, mas não foi acionada remotamente. Nenhum push, deploy, publicação, instalação ou ação elevada realizado. Global.json preexistente não foi incluído nos commits.
- Próximos trabalhos estão em ROADMAP.md: infraestrutura de desenvolvimento, E2E autenticado, uploads/avatares seguros, expansão social/moderação, telemetria e acessibilidade. Não se declara produto inteiro concluído.
- Pacote local: artifacts/PHsystem-Next-UX-preview-win-x64.zip (69.052.294 bytes). SHA-256: 803715d709ad61537901bee9b006e1040f595021fd916f54be6939a08ea358ed. SHA256SUMS-UX.txt acompanha o ZIP; não é assinatura digital.

## Correção de cadastro/login/conexão — 2026-11-16

- Causa: AuthVM chamava Connect com ApiUrl vazio, pois só havia campo manual/variável de ambiente. Health anterior não verificava banco; Connect descartava cookies entre etapas. Auditoria completa em AUDITORIA-CONEXAO-API.md.
- Configuração central: services.json da distribuição. Debug permite automação de desenvolvimento e descoberta de porta HTTPS loopback com expiração. Release ignora esses caminhos e exige domínio de produção no empacotamento -Production. Nenhuma URL é solicitada ao usuário.
- Startup verifica /health versionado, banco/esquema e flags de identidade. Três GETs no máximo, prazo total 11s; erro TLS interrompe. POST não é repetido. UI exibe a mensagem solicitada e Tentar novamente; encerra carregamento.
- Regressões corrigidas: cookies preservados na mesma origem, vault não escolhe servidor, reconexão após senha incorreta e troca de endpoint anônimo sem desviar cadastro para login. Origem autenticada alterada invalida sessão.
- Testes: cadastro/confirmação/login, duplicidade de nome/e-mail, senha fraca no servidor, restauração DPAPI, revogação, recuperação e logout via desktop/client → Kestrel real com TLS e porta dinâmica. O trust da CA fica exclusivamente no handler do teste; lojas de confiança do Windows não foram modificadas. SQLite/mailbox são fixtures e não prova de PostgreSQL/SMTP de produção.
- Banco indisponível resulta em 503 sanitizado, sem SQL/tabelas/senha na resposta. Modelo EF sem alterações pendentes; migrations existentes preservadas.
- Preflight exercitado: Docker ausente bloqueia inicializador; certificado de desenvolvimento válido mas não confiável. Não foram instalados componentes ou confiados certificados. Script StartDevelopment e tarefas VS Code preparados, porém Compose completo não executado nesta máquina.
- Pacote de produção sem domínio ou com HTTP/loopback é bloqueado antes do publish. Não houve deploy/push/criação de conta pública. Publish local win-x64 self-contained aprovado; usuário ainda verá indisponibilidade até a API hospedada ser configurada pelo distribuidor.
- Aplicativo publicado exercitado por UIAutomation: texto correto, Retry disponível, sem campo de URL, sem carregamento infinito. Captura privada artifacts/api-auto-unavailable.png. Instalação limpa em VM e servidor/e-mail externos não foram testados.
- Operação, arquivos e instruções de provisionamento: API-CONNECTION.md. Alteração preexistente de global.json preservada fora dos commits.
- Artefato desta correção: artifacts/PHsystem-Next-API-preview-win-x64.zip, 69.064.661 bytes. SHA-256: 7af87db5a93bb8ab3375eb6b8c6063d6ea177c3706b66858e6a5cd2ff2c73530 (também em SHA256SUMS-API.txt). Não é instalador assinado; teste em instalação limpa segue pendente.

## V2 — primeira entrega vertical, 2026-11-16
- Diagnóstico/histórico/limpeza locais liberados sem conta ou API. A comunidade tem gate próprio; logout/401 não oculta ferramentas locais. Formulário autenticado antigo foi substituído por conta e exclusão explícita, sem tarefa manual de renovar token.
- Limpeza: estados explícitos, seleção por arquivo, contagens/motivos coerentes, limite informado, proteção por identidade de arquivo e prévia expira exatamente aos 10 minutos. Zero legítimo não pede executar nova análise como se nada tivesse ocorrido. Bytes nominais separados da variação observada de espaço; nenhuma promessa de quarentena/recuperação.
- Evidência: build Release sem avisos/erros; testes de limpeza existentes 40 aprovados, seis novos testes de scanner/nativo aprovados, testes WPF offline e limpeza com SQLite real e coleta única aprovados. Falha de teste de bloqueio revelou que READ_ATTRIBUTES não detectava arquivo em uso; corrigida a inspeção para abertura de leitura e repetido com sucesso.
- A primeira tentativa de baseline Debug falhou por DLLs bloqueadas pelo app do usuário; sessão preservada e baseline Release executado: 135 aprovados, 1 PostgreSQL ignorado. Não confundir esse impedimento de build com erro funcional.
- Ambiente agora tem Docker/API/PostgreSQL/Mailpit ativos; health HTTPS ready. Builder selecionado phsystem-clean e chaves graváveis, sem remover cache/volumes. Alteração do usuário em StartDevelopment.ps1 e backup preservados fora destes commits.
- Capturas privadas: v2-before.png (app Debug original) e v2-current.png (nova tela ainda não analisada). Não foi acionada exclusão no computador do usuário; exclusões de teste ocorreram apenas em sandbox.
- Próximo: reações confirmadas e concorrência, correlação de erros, teste PostgreSQL isolado, identificação de versões e documentação consolidada. Site dinâmico/Google/reparos avançados/instalador ainda não implementados nesta entrega.
