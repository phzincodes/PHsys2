# Arquitetura

## Atualização 0.3.0 — prevalece sobre o histórico abaixo

Saúde ponderada reside em Core e só retorna faixa com dimensões desconhecidas; CrashInvestigator usa o snapshot, não relê logs por aba. Windows tem três coletores adicionais de leitura. Personalization guarda biblioteca/widgets no JSON local. AccessiblePageHost expõe conteúdo real à automação. Google usa Identity externo/sub e broker efêmero PKCE, fechado por configuração. ReleaseDownloads só serve caminho fixo e receipt da distribuição; não aceita URL de binário do cliente.


Monorepo novo, monólito modular .NET 10. Sem migração de motores antigos e sem PowerShell como motor do produto.

## Dependências e fronteiras

- Core contém snapshots imutáveis, coletores/repositório/asset contracts e achados, sem WPF.
- Windows adapta APIs locais .NET, Registry, memória nativa, WMI e Event Log. Não transmite dados nem executa comandos da comunidade.
- Diagnostics coordena uma varredura compartilhada, com concorrência limitada, timeout, cancelamento, circuit breaker e regras conservadoras.
- LocalData persiste SQLite/EF Core com migrações e retenção, e carrega assets cadastrados com limites/cache/validação/fallback.
- Desktop WPF usa MVVM e DI. SnapshotStore singleton alimenta diagnóstico, achados, drivers, recomendações e histórico. Cliente de comunidade está isolado do motor de leitura e começa desconectado.
- Cleanup oferece prévia/exclusão restrita de temporários e análise read-only opt-in de pasta/duplicados. Listas de arquivos privados não entram no snapshot nem na nuvem.
- Drivers possui inventário e política de evidências para pacotes, ainda sem pipeline real de download/assinatura/instalação.
- Optimizer contém regras versionadas e perfil temporário de prioridade por processo. Não altera segurança, serviços ou afinidade.
- Community contém política de nomes, conteúdo e resumo reduzido. SharedContracts contém DTOs v1, separados das entidades internas.
- Server usa ASP.NET Core Identity, EF Core/PostgreSQL e SignalR. Dados públicos escolhidos ficam separados do snapshot local, que a API não recebe.
- Admin é um host web separado, com proxy allowlist HTTPS. Autoridade está na API: papéis, MFA e sessão recente. CLI de bootstrap separada não reassume contas por nome.
- PrivilegedHelper continua recusando operações; ainda não há IPC seguro/ACL/UAC implementado. Nenhum módulo deve contornar isso lançando shell arbitrário.

## Persistência e comunicação

SQLite local em `%LOCALAPPDATA%/PHsystemNext/snapshots.db`, sem criptografia própria. Migração local cria schema e retenção de até 100 snapshots. Estado publicado no store somente após gravação bem-sucedida. Exclusão local não remove exportações externas.

PostgreSQL do servidor só é usado quando configurado. Migrações são explícitas (`--migrate`), sem troca silenciosa para SQLite. SQLite de backend existe apenas nos testes HTTP. Cookies do cliente desktop ficam na memória e usam HTTPS/CSRF; Data Protection keys do servidor precisam de proteção/persistência operacional.

Nenhum microserviço, cache distribuído ou fila foi introduzido sem necessidade medida. A API não tem acesso privilegiado ao Windows do usuário.

Consulte STATUS.md para o que foi efetivamente executado, BACKLOG.md para lacunas e SECURITY.md/PRIVACY.md para limites. Existência do projeto não comprova entrega de um módulo completo.

## Correções V2

Identidade online é independente do shell local. MainWindow e AuthenticationViewModel compartilham CommunityClient, enquanto SnapshotStore/SQLite permanecem utilizáveis sem API. PostVoteStore concentra a escrita parametrizada idempotente, e PostReactionDto confirma o estado sem recarregar o feed. ApiErrors produz correlação sanitizada e o cliente mantém cooldown por origem. BuildIdentity usa metadata de assembly e MVID para distinguir compilação efetiva sem ler arquivos pessoais. Ver VALIDATION para limites comprovados.

## V3 — web compartilhada e temas

PublicWeb serve somente arquivos curados publicados de site/ para wwwroot e rotas explícitas. Frontend sem framework adicional consome API v1 same-origin; Identity/CSRF/cookies/PostgreSQL são os mesmos do WPF. Não há proxy token nem armazenamento de sessão em localStorage. Porta web local5178 configurável, binding loopback. Produção não pode depender desse compose/certificado. ThemeManager/UiPreferences são locais, sem sincronização automática.
