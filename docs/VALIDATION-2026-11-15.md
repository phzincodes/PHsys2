# Evidências da continuação — prompt mestre

Ambiente inspecionado: Windows 11 Pro 10.0.26200 x64, SDK .NET 10.0.404 em D:\PHsystemTools\dotnet, runtimes 10.0.15. Git 2.56.1.windows.1. Workspace aberto no VS Code; extensões C#, C# Dev Kit e Codex presentes. gh, Docker, Podman e ISCC não encontrados. A alteração do usuário em global.json para 10.0.400/latestFeature foi preservada.

## Comandos e resultados

- Restore de PHsystem.slnx: aprovado; solução com 20 projetos após inclusão de ProvisionAdmin.
- Build de PHsystem.slnx: aprovado, zero avisos e zero erros. Os testes posteriores recompilaram as últimas alterações de código sem erro.
- `dotnet test PHsystem.slnx --logger trx`: 80 aprovados, zero falhas, um ignorado. Últimos TRX completos de 2026-11-15, 19:17 local.
- `dotnet ef migrations has-pending-model-changes --project src/PHsystem.Server`: nenhuma mudança pendente em relação à migração. Não conectou a PostgreSQL real.
- Consulta de pacotes vulneráveis conhecidos, incluindo transitivos, em JSON: zero pacotes sinalizados na consulta; não é garantia de ausência de vulnerabilidades.
- Publish self-contained win-x64 aprovado para artifacts/desktop após as correções visuais e de cliente.
- `tools/PackageDesktop.ps1`: ZIP local de 68.860.056 bytes gerado; SHA-256 `9c7b0459df25c9478f9338c9bc55afbf8c2618d95180690f2cc26912ccbf877c`. Arquivo SHA256SUMS.txt acompanha o pacote. Sem assinatura ou publicação.
- Executável publicado iniciado: InputIdle=True, Exited=False, título PHsystem Next e handle de janela não nulo. Captura posterior confirmou tema escuro, texto legível e barra de rolagem sem setas. Captura inicial ocorreu cedo demais e veio branca; a captura após renderização foi inspecionada. O app permanece sob controle do usuário.
- Dois HTML do site com links locais existentes; XML dos projetos e XAML parseados. Não houve teste visual em navegador do site nem compilação Inno/Docker.

## Distribuição dos testes

Suíte | Aprovados | Falhas | Ignorados | TRX final
---|---:|---:|---:|---
UnitTests | 51 | 0 | 0 | PH_PH_2026-11-15_19_17_23_net10.0.trx
SecurityTests | 22 | 0 | 0 | PH_PH_2026-11-15_19_17_29_net10.0.trx
IntegrationTests | 3 | 0 | 1 | PH_PH_2026-11-15_19_17_29_net10.0.trx
UiTests | 2 | 0 | 0 | PH_PH_2026-11-15_19_17_28_net10.0.trx
WindowsTests | 1 | 0 | 0 | PH_PH_2026-11-15_19_17_27_net10.0.trx
E2ETests | 1 | 0 | 0 | PH_PH_2026-11-15_19_17_31_net10.0.trx

O ignorado é RealPostgresAppliesMigrationsAndEnforcesIdentityUniqueness, pois PHSYSTEM_TEST_POSTGRES não foi configurado. O adapter TRX registra total=4/executed=3 para IntegrationTests e resultado NotExecuted no teste PostgreSQL; o atributo agregado notExecuted do adapter fica zero. A contagem de ignorados foi conferida pelos resultados individuais e pelo console, não inferida como aprovação.

Os testes HTTP usam Identity real com SQLite isolado e email de teste, incluindo MFA/TOTP, antirreuso, token de reset de uso único, CSRF, roles, privacidade entre usuários, mensagens opt-in, solução por autor, retirada de compartilhamento e indisponibilidade de triagem de senha. Não representam SMTP, PostgreSQL ou produção reais.

## Artefatos e privacidade

Pacote, auditoria de dependências, resumo de testes e captura da janela estão em artifacts, ignorado pelo Git. Capturas podem conter CPU/hardware do snapshot local e não foram copiadas para o site ou publicadas. Senhas, TOTP de usuário real e credenciais de infraestrutura não foram solicitados nem usados. Nenhuma limpeza de dados reais ou instalação de driver foi executada; testes de exclusão ficaram em pastas exclusivas de fixture.

Persistem as lacunas descritas em STATUS.md e BACKLOG.md. Não houve CI remota, deploy, repositório público, assinatura, teste de instalador/VM limpa, conformidade jurídica validada ou alegação de produto completo.
