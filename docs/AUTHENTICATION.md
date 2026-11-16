# Autenticação — estado V2

## Atualização 0.3.0 — prevalece sobre o histórico abaixo

Infraestrutura Google agora implementada e testada por backchannel isolado com Identity/cookies. Confira GOOGLE-SETUP: credenciais e teste real ainda faltam. Desktop usa navegador do sistema e broker PKCE, sem client secret local; broker desligado por padrão. Conta nova não exige senha local, email verificado pelo provedor não recebe confirmação redundante. Emails coincidentes não vinculam; MFA/equipe usam o método original. Gerência de métodos, vinculação explícita e sessões individuais não foram concluídas.


O desktop abre os recursos locais sem conta. `MainWindow` só apresenta `AuthenticationView` por ação de acesso online. `AuthenticationViewModel` usa o mesmo `CommunityClient` da comunidade; nenhum inventário é anexado ao cadastro/login.

O cliente resolve a origem em `ApiEndpointConfiguration`, verifica `/health` e aceita somente raiz HTTPS com validação normal de certificado. Release usa configuração de distribuição; descoberta local é exclusiva de Debug. Falhas têm limite de tempo e opção de tentar novamente. 401/403 não são classificados como queda de servidor; 429 respeita Retry-After. Mutações não são repetidas automaticamente.

Identity no servidor aplica senha 8–128, maiúscula/minúscula/número/símbolo, triagem configurável de senhas comprometidas e PBKDF2 de 600.000 iterações. Cookies são HttpOnly/Secure, com CSRF e security stamp. Lembrar sessão protege o cookie via DPAPI CurrentUser, vinculado à origem, e revalida a identidade. Logout/revogação limpam dados privados da comunidade, preservando diagnóstico local.

Confirmação e recuperação atualmente usam ID/token enviados por e-mail. Migração para links de uso único ainda não foi implementada. Mailpit é exclusivo do desenvolvimento; testes de mailbox não comprovam entrega SMTP externa. MFA protege políticas de equipe/admin, mas enrollment/recuperação desktop completos permanecem pendentes.

Google/OIDC, vinculação segura de métodos, lista de dispositivos e renovação automática completa são pendentes. Não existe login Google simulado nem client secret embutido. Não anunciar esses recursos como disponíveis.

Evidências: `DesktopBackendTests`, `ApiErrorTests`, `CommunityApiTests`, `PostgresTests` e [VALIDATION](VALIDATION.md). Produção exige o roteiro de [API-CONNECTION](API-CONNECTION.md).

## V3 — frontend e links

Formulários web separados, com CSRF same-origin, sem guardar sessão no localStorage. Links de email abrem /confirmar ou /redefinir com ID/token no fragmento e enviam POST. Template texto/HTML e origem por ambiente; tokens legados permanecem no corpo para compatibilidade. Google/OIDC e teste de entrega externa continuam pendentes, sem botão fingindo login. Consulte EMAIL e WEB.


# Incremento de comunidade e perfis — 2026-11-16

Implementado no código: publicações tipadas com chave de idempotência; seguimento individual; edição/exclusão de respostas com controle de versão e retirada da solução; rascunhos desktop protegidos por DPAPI e separados por conta; editor web com prévia Markdown sem HTML executável; perfil público somente por opt-in; compartilhamento sanitizado de saúde autodeclarada e ranking separado da reputação; moderação e administração com autorização no servidor, MFA e confirmação de senha nas ações críticas. O cabeçalho de identidade esperada impede envio de conteúdo privado de uma aba antiga para outra conta; ele não concede permissões.

Evidências: build da solução sem avisos/erros; suíte final 183 aprovados e dois PostgreSQL ignorados nessa rodada, seguida de execução dos cinco testes de integração com PostgreSQL exclusivo: cinco aprovados, nenhum ignorado. São185 testes distintos aprovados considerando ambas as execuções. Publish self-contained win-x64 em `artifacts/community-preview`. Logs privados: `artifacts/community-tests-final.log`, `community-postgres.log`, `community-publish.log`. JavaScript validado com node --check. Migração aditiva CommunityProfilesAndModeration gerada; aplicada e exercitada no banco exclusivo de testes.

Limitações: navegador real e matriz física DPI não validados neste incremento; Google real, e-mail externo, assinatura, instalador em VM limpa, atualizador, integridade DISM/SFC, anexos/avatares e cobertura avançada continuam pendentes. Administração implementada não equivale a auditoria de segurança completa. Nenhuma publicação remota ou instalação executada. O build Docker padrão apresentou biblioteca compartilhada corrompida; a execução explícita com builder phsystem-clean compilou API/migrate. Banco local preservado com backup privado; migração aditiva aplicada. HTTPS /health respondeu ready, migrationState current, API0.3.0/build b0159020388e; cadastro/login/recuperação disponíveis. Isso não comprova entrega externa de e-mail.

---

