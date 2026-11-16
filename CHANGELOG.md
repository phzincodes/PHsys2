## Continuação local — comunidade e perfis

Publicações tipadas/idempotentes, seguimento, respostas versionadas, rascunhos privados, perfis opt-in, saúde autodeclarada e painel de moderação/administração. Ver docs/VALIDATION.md para limites e resultados reais.

# Changelog

## 0.3.0-preview — continuação

- Três coletores novos: Defender, firewall ativo e estabilidade com escopo limitado.
- Avaliação ponderada sem completar desconhecidos, Crash Investigator e relatório HTML reduzido.
- Biblioteca de temas, editor ampliado e widgets persistentes.
- Correção real do peer de navegação acessível e botão analisar acima da dobra.
- Google OAuth/PKCE e broker nativo fechado por configuração; testes de protocolo sem credenciais reais.
- Instalador Inno compilado, receipt/SHA-256 e download HTTPS local real; sem assinatura/VM limpa/updater.


## Red Eclipse V3 — 2026-11-16

- Paleta global vermelha/grafite, presets persistentes e import/export de tema validado.
- Dashboard de snapshot/cobertura, painel adaptativo e rótulo correto do seletor.
- Network Lab sob demanda e ferramentas oficiais assistidas.
- Web reconstruída com servidor HTTPS localhost5178 e API compartilhada; formulários, feed e perfil.
- Links de email com fragmentos, HTML/texto e validação de origem/TLS.
- Evidências e limites em docs/VALIDATION.md; nenhum release público/instalador ou OAuth anunciado como pronto.


## V2 — fluxos essenciais — 2026-11-16

- Diagnóstico local acessível offline, sem conta; login separado e sessão encerrada sem perder o snapshot.
- Limpeza com estados explícitos, escopo visível, motivos de exclusão, seleção e resultado por arquivo. Identidade nativa protege contra substituição entre prévia e exclusão; hardlinks e arquivos em uso são recusados.
- Curtida com estado visual, bloqueio de envio repetido e atualização pontual confirmada pela API; upsert atômico/idempotente em PostgreSQL e SQLite.
- Erros HTTP diferenciados, correlação sanitizada e respeito a Retry-After sem repetir mutações.
- Versão/build/contrato e estado de migrações visíveis em Configurações; preparação de chaves para volumes novos e preflight Docker somente leitura.
- Regressões Windows/WPF, TLS/Identity, PostgreSQL real e build Linux. Sem publicação, limpeza de dados reais ou implantação de produção.


## Conexão automática e autenticação — 2026-11-16

- Removidos os campos de URL do cadastro/login e da conta; configuração central de distribuição e descoberta local exclusiva de Debug.
- Health versionado verifica banco/esquema e disponibilidade de login/cadastro; liveness separado, prazo e retries controlados apenas para GET.
- Certificados continuam validados; falhas exibem mensagem clara e Tentar novamente, sem carregamento infinito.
- Conexão por origem preserva cookies; restauração não pode trocar servidor; recuperação após senha incorreta foi corrigida.
- Reenvio de confirmação genérico; falhas de banco/e-mail sanitizadas; duplicidade e hash Identity preservados.
- Inicializador de desenvolvimento descobre portas, aplica migrations e inicia o app após preflight; empacotamento de produção exige API hospedada configurada e saudável.
- Testes desktop/API com Kestrel/TLS em porta dinâmica, Identity real, banco/e-mail de teste isolados e rejeição de certificado não confiável. Sem deploy de produção.


## Reformulação UX, autenticação e comunidade — 2026-11-15 (desenvolvimento)

- Boas-vindas com cadastro/login reais, termos, confirmação/recuperação e MFA no login; shell condicionado à validação da sessão.
- Cookie opcional protegido por DPAPI, expiração/renovação e limpeza de dados sociais entre sessões.
- Início com ação única ANALISAR, progresso factual por coletor e resultados compartilhados; comparação de histórico.
- HUD normalizada, 14 áreas, paleta solicitada, scrollbar fina, transições curtas, redução de movimento e sons originais opcionais.
- Comunidade WPF com feed paginado, pesquisa/filtros, votos, salvos, categorias seguidas, edição própria, threads, perfil e notificações; migrações PostgreSQL.
- Novos testes de autorização/privacidade e matriz lógica de layout. Sem publicação, deploy, instalação ou promessa de cobertura completa. Dependências e recursos pendentes documentados.

## Revisão de interface — 2026-11-15

- Novo shell desktop com dez áreas, navegação lateral recolhível, atalhos e abas por tarefa.
- Visão geral derivada do snapshot; diagnóstico com filtro e lista/detalhe; tabelas, formulários e confirmações redesenhados.
- Cores neutras, ícones vetoriais de linha, controles escuros, foco visível e estados desabilitados.
- Pedidos de atualização do PH CORE agrupados; histórico com cobertura factual; nomes e tamanhos legíveis.
- Capturas privadas de dez áreas e da janela compacta; regressões de binding/navegação cobertas por testes WPF.

## 0.2.0 — em desenvolvimento, sem release pública

- Coordenação de 15 categorias locais: cancelamento, timeout, paralelismo limitado, proteção contra coleta duplicada, circuit breaker e erro parcial.
- SQLite/EF Core com migração inicial, retenção de 100 snapshots, carregamento e exclusão de histórico.
- WPF com diagnóstico, achados, prévia de temporários, inventário de drivers, recomendações, perfil temporário de prioridade por processo, histórico e privacidade.
- Cliente de comunidade HTTPS opt-in, contas, fórum, mensagens e prévia de campos de hardware públicos. Nenhum envio automático do snapshot.
- API v1 com Identity, cookies seguros, CSRF, confirmação/reset por email configurável, MFA/TOTP e antirreuso, autorização por papel, compartilhamento granular, denúncias e revisão humana.
- Migrações PostgreSQL, Dockerfiles/compose locais, painel administrativo separado e CLI de provisionamento do proprietário com MFA.
- Validação de conteúdo de assets, cache limitado com atualização por metadados, catálogo configurável e fallback instalado.
- Site estático local, fonte do instalador por usuário, script de ZIP/checksum e pipelines preparados.
- Suítes reais de unidades, integração SQLite, segurança HTTP, Windows, XAML e fluxo local; PostgreSQL real depende de ambiente de teste.

Esta lista descreve código criado. Não declara M0–M8 completos. Consulte docs/STATUS.md e docs/BACKLOG.md para limites e evidências.

## 0.1.0 — fundação inicial

Solução modular, shell WPF, contrato de snapshot, catálogo de assets, tarefas VS Code e dois testes iniciais de cobertura/score. SDK instalado anteriormente com autorização.
