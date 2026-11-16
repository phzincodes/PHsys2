# Auditoria da conexão — 2026-11-15

Inspeção realizada antes da alteração do fluxo. Baseline: 113 testes aprovados, nenhuma falha, um teste PostgreSQL ignorado. Alteração preexistente de `global.json` preservada.

Problema | Arquivo real | Causa | Correção
---|---|---|---
Cadastro pede URL | src/PHsystem.Desktop/AuthenticationViewModel.cs | ApiUrl vem apenas de PHSYSTEM_API_URL ou campo da tela; SubmitAsync chama Connect com string vazia | configuração central distribuída + descoberta de desenvolvimento
URL na interface | src/PHsystem.Desktop/Views/AuthenticationView.xaml; Views/CommunityView.xaml | telas expõem configuração técnica e reconexão manual | remover campos; estado de conexão e Tentar novamente
Cookies descartados a cada formulário | src/PHsystem.Desktop/CommunityClient.cs | Connect recria HttpClient/CookieContainer em cada etapa | conexão idempotente por origem; não repetir POST automaticamente
Health não verifica dependências | src/PHsystem.Server/Program.cs | /health responde 200 até sem banco; só olha connection string | saúde/readiness real com timeout, banco/esquema/configuração de identidade e protocolo versionado
Configuração de distribuição ausente | src/PHsystem.Desktop/PHsystem.Desktop.csproj | nenhum appsettings/launchSettings de API no inventário, nem endpoint empacotado | manifesto de serviços não secreto; publicação de produção validada
PostgreSQL/e-mail dependem de operação | docker-compose.yml; Server/IdentityServices.cs | containers/credenciais/certificado/lista de senhas precisam ser preparados | inicializador de desenvolvimento separado, portas descobertas e provisionamento documentado
Restauração pode trocar origem | src/PHsystem.Desktop/CommunityClient.cs — RestoreSession | aceita origem guardada no vault como configuração atual | restaurar cookie somente para a origem configurada da distribuição

API real: `PHsystem.Server`, ASP.NET Core Identity/PBKDF2, EF Core/PostgreSQL, CSRF, rate limit e MFA obrigatório para equipe. Contratos em `PHsystem.SharedContracts/CommunityContracts.cs`. Cadastro/confirmação/login/reset em `Server/AccountEndpoints.cs`. Sessão DPAPI em `Desktop/SessionVault.cs`.

Não há `appsettings.json` nem `launchSettings.json` para descoberta do desktop na versão auditada. O backend usa o pipeline padrão de configuração ASP.NET Core e fallback HTTP loopback 5080, inadequado para autenticação. Compose tem TLS, porém portas fixas e configuração operacional manual. Não existe URL pública de produção configurada. Docker/Podman/psql não foram encontrados. Existe certificado ASP.NET de desenvolvimento válido; confiança verificada separadamente, sem desativar TLS.

Nenhum servidor de produção, banco ou serviço de e-mail foi provisionado nesta auditoria. Não publicar nem simular cadastro para ocultar essa dependência.

## Estado após a correção — 2026-11-16

Configuração/descoberta, health real, timeout/retry, tratamento TLS, remoção dos campos manuais, persistência por origem e fluxo Identity corrigidos e testados. Novo endpoint de reenvio de confirmação possui resposta genérica/rate limit. Client desktop testado com API Kestrel real/TLS e fixtures isoladas. Orientações permanentes em API-CONNECTION.md. Infraestrutura de produção, Docker local completo, confiança do certificado de desenvolvimento, SMTP externo e teste de instalação limpa permanecem pendentes; nenhum sucesso foi simulado.
