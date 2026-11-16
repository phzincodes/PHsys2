# Comunidade — contratos e comportamento atual

## Atualização 0.3.0 — prevalece sobre o histórico abaixo

A biblioteca/relatório/avaliação de saúde são locais. Nenhum score foi publicado em perfil e nenhum ranking foi criado. Google passa a ser opção condicional real de infraestrutura; as contas usam as mesmas tabelas Identity. Feed/categorias existentes não foram substituídos nem preenchidos com dados falsos. Tipos de conteúdo, anexos, revisão social completa, avatar/capa e ranking seguem pendentes.


O feed WPF consome `SocialEndpoints` no backend Identity/PostgreSQL existente. Não há uma segunda base de contas. O site estático ainda não oferece esse feed: uma camada web autenticada permanece pendente.

A ação principal é Curtir/retirar curtida. `Likes` conta reações positivas; `Score` mantém saldo legado de votos, inclusive negativos, por compatibilidade. DTO antigo sem Likes é exibido como votos, não como curtidas inventadas. Solução aceita pelo autor e revisão técnica são estados separados.

`PostVoteStore.SetAsync` usa SQL parametrizado com índice composto e ON CONFLICT para PostgreSQL/SQLite. Escritas repetidas do mesmo estado são idempotentes; valor zero remove a reação. Conteúdo oculto, bloqueio entre contas e reação própria são recusados. Salvar/seguir categoria também não duplicam relações.

O desktop bloqueia novo clique durante o envio e só altera a contagem após consultar `/posts/{id}/reaction`. A confirmação atualiza o item e o detalhe, preservando a paginação. Falha não inventa sucesso nem incremento. Logout troca o estado da conta e limpa conteúdo privado/rascunhos em memória.

Testes incluem dois usuários reais de Identity sobre TLS e concorrência entre oito conexões PostgreSQL em banco reservado de teste. Isso não equivale a teste público de carga ou de reinício do serviço. Sem anexos seguros/armazenamento provisionado, não anunciar upload funcional.

Pendências: plataforma web compartilhada, rascunhos persistentes isolados por conta, anexos reprocessados, menções, seguir publicações individuais, edição de respostas e notificações push completas. Moderação e riscos: [COMMUNITY-MODERATION](COMMUNITY-MODERATION.md).

## V3 — cliente web Red Eclipse

site/app.js usa os mesmos endpoints/Identity/banco e reações persistentes, sem nova comunidade. Editor texto seguro e prévia, feed paginado/busca, respostas/aceite, perfil/biografia/notificações implementados no código web. Testes HTTP/servidor passam, mas browser/E2E e sincronização visual entre clientes ainda não comprovados. Perguntas/Tutoriais mostram aviso de feed geral; rich text/anexos/reputação/avatares/rascunhos persistentes continuam pendentes.


# Incremento de comunidade e perfis — 2026-11-16

Implementado no código: publicações tipadas com chave de idempotência; seguimento individual; edição/exclusão de respostas com controle de versão e retirada da solução; rascunhos desktop protegidos por DPAPI e separados por conta; editor web com prévia Markdown sem HTML executável; perfil público somente por opt-in; compartilhamento sanitizado de saúde autodeclarada e ranking separado da reputação; moderação e administração com autorização no servidor, MFA e confirmação de senha nas ações críticas. O cabeçalho de identidade esperada impede envio de conteúdo privado de uma aba antiga para outra conta; ele não concede permissões.

Evidências: build da solução sem avisos/erros; suíte final 183 aprovados e dois PostgreSQL ignorados nessa rodada, seguida de execução dos cinco testes de integração com PostgreSQL exclusivo: cinco aprovados, nenhum ignorado. São185 testes distintos aprovados considerando ambas as execuções. Publish self-contained win-x64 em `artifacts/community-preview`. Logs privados: `artifacts/community-tests-final.log`, `community-postgres.log`, `community-publish.log`. JavaScript validado com node --check. Migração aditiva CommunityProfilesAndModeration gerada; aplicada e exercitada no banco exclusivo de testes.

Limitações: navegador real e matriz física DPI não validados neste incremento; Google real, e-mail externo, assinatura, instalador em VM limpa, atualizador, integridade DISM/SFC, anexos/avatares e cobertura avançada continuam pendentes. Administração implementada não equivale a auditoria de segurança completa. Nenhuma publicação remota ou instalação executada. O build Docker padrão apresentou biblioteca compartilhada corrompida; a execução explícita com builder phsystem-clean compilou API/migrate. Banco local preservado com backup privado; migração aditiva aplicada. HTTPS /health respondeu ready, migrationState current, API0.3.0/build b0159020388e; cadastro/login/recuperação disponíveis. Isso não comprova entrega externa de e-mail.

---

