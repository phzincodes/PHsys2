# Web — Red Eclipse

## Atualização 0.3.0 — prevalece sobre o histórico abaixo

Download agora consulta /api/v1/releases/current e oferece /download/windows quando um artefato real está presente. API serve filename fixo e receipt validado; 404 produz indisponibilidade, não link fictício. Instalador baixado por HTTPS teve hash conferido. Google aparece apenas configurado, com aceite de termos e redirecionamentos compreensíveis para colisão/erro/conta com MFA. Website e desktop mantêm API/Identity/banco únicos. Credenciais Google e browser real continuam não validados; não foi criado site hospedado.


Acesse **https://localhost:5178/** no ambiente Compose local. Esta URL serve o site, não Mailpit/JSON. A API e a web são o mesmo ASP.NET Core, com Identity/PostgreSQL e cookies HttpOnly/Secure; não existe outra comunidade. O bind 5178 é loopback e configurável por PHSYSTEM_DEV_WEB_PORT. A porta dinâmica da API continua disponível. TLS usa o certificado local já confiável; não houve bypass.

Fontes reais: site/index.html, style.css e app.js; servidor PublicWeb.cs. O publish copia somente o diretório site para wwwroot; fontes/segredos do repositório não são publicados. O Dockerfile inclui o frontend. CSP restringe scripts/estilos a self, sem eval, HTML de usuários ou tokens de sessão no localStorage.

Páginas implementadas: landing Red Eclipse, recursos, diagnósticos, download/status honesto, suporte, atualizações, termos/privacidade em revisão; formulários separados de login/cadastro/recuperação/confirmação/redefinição; comunidade com busca/paginação, criar/editar/excluir, curtir/retirar/salvar, respostas, denúncia e solução aceita; perfil, biografia privada editável, logout e notificações. Conteúdo de usuário é inserido com textContent. Autorização e validação permanecem no servidor.

Limites: Perguntas/Tutoriais mostram explicitamente o feed geral, pois tipos de conteúdo ainda faltam no domínio. Editor é texto seguro com prévia, sem rich text/anexos/rascunho persistente. Perfis exigem sessão atualmente, sem avatar/capa/score público. Não há download fictício: a página explica a ausência de instalador público validado. Site não examina o computador.

Execução: tools/StartDevelopment.ps1 com arquivo privado externo já provisionado; Docker recria apenas serviços solicitados, preservando volumes. Em 2026-11-16 foi atualizada somente a API local e /health respondeu ready/current, landing HTTP 200 por HTTPS normal.

Verificação: testes HTTP de páginas/CSS/JS/autorização e node --check. A aprovação automática rejeitou a abertura do navegador normal e com depuração, com motivo genérico blocked by policy. Portanto não há alegação de navegador/mobile/E2E web validado nem captura fabricada. Abra o endereço acima manualmente para revisão visual. Produção precisa domínio, certificados, SMTP, backups, custos/autorização e revisão; nenhuma hospedagem externa realizada.

A skill Sites foi aplicada somente ao fluxo local de edição/verificação; não foi criado Site hospedado nem sincronização externa. O Pages antigo não hospeda os fluxos autenticados: exige o servidor ASP.NET Core.


# Incremento de comunidade e perfis — 2026-11-16

Implementado no código: publicações tipadas com chave de idempotência; seguimento individual; edição/exclusão de respostas com controle de versão e retirada da solução; rascunhos desktop protegidos por DPAPI e separados por conta; editor web com prévia Markdown sem HTML executável; perfil público somente por opt-in; compartilhamento sanitizado de saúde autodeclarada e ranking separado da reputação; moderação e administração com autorização no servidor, MFA e confirmação de senha nas ações críticas. O cabeçalho de identidade esperada impede envio de conteúdo privado de uma aba antiga para outra conta; ele não concede permissões.

Evidências: build da solução sem avisos/erros; suíte final 183 aprovados e dois PostgreSQL ignorados nessa rodada, seguida de execução dos cinco testes de integração com PostgreSQL exclusivo: cinco aprovados, nenhum ignorado. São185 testes distintos aprovados considerando ambas as execuções. Publish self-contained win-x64 em `artifacts/community-preview`. Logs privados: `artifacts/community-tests-final.log`, `community-postgres.log`, `community-publish.log`. JavaScript validado com node --check. Migração aditiva CommunityProfilesAndModeration gerada; aplicada e exercitada no banco exclusivo de testes.

Limitações: navegador real e matriz física DPI não validados neste incremento; Google real, e-mail externo, assinatura, instalador em VM limpa, atualizador, integridade DISM/SFC, anexos/avatares e cobertura avançada continuam pendentes. Administração implementada não equivale a auditoria de segurança completa. Nenhuma publicação remota ou instalação executada. O build Docker padrão apresentou biblioteca compartilhada corrompida; a execução explícita com builder phsystem-clean compilou API/migrate. Banco local preservado com backup privado; migração aditiva aplicada. HTTPS /health respondeu ready, migrationState current, API0.3.0/build b0159020388e; cadastro/login/recuperação disponíveis. Isso não comprova entrega externa de e-mail.

---

