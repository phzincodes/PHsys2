# Desenvolvimento e distribuição

## Atualização 0.3.0 — prevalece sobre o histórico abaixo

Versão local0.3.0 e instalador preview gerados, HTTPS5178 e download real ativos somente em loopback. Compose monta artifacts/releases como readonly, separado de banco/chaves/certificados. Google é opcional por variáveis privadas; EnableNativeBroker permanece false. Não houve provisionamento de produção, registro de domínio, assinatura ou envio externo. Inno Setup já estava instalado, sem instalação pela automação. Consulte GOOGLE-SETUP/INSTALLER/VALIDATION para configurações e limites atuais.


Nenhum serviço remoto foi implantado. Docker/Compose estão disponíveis na inspeção de 2026-11-16; API/PostgreSQL/Mailpit locais já existentes responderam. GitHub CLI e Inno Setup não foram usados nesta entrega. Não instalar infraestrutura elevada nem criar serviços pagos sem autorização específica.

## Desktop

Abra o workspace no VS Code e execute as tarefas restore → build → test → desktop. O SDK local está no D:; novos terminais recebem DOTNET_ROOT, PATH e NUGET_PACKAGES pelo workspace. Em outro computador, ajuste os caminhos ou use `dotnet` no PATH.

```powershell
dotnet publish src/PHsystem.Desktop -c Release -r win-x64 --self-contained true -o artifacts/desktop
./tools/PackageDesktop.ps1
```

O executável depende da pasta completa, incluindo DLLs, runtime e assets. O script gera ZIP e SHA256SUMS localmente. Não instala, não publica e não assina. Para compilar o instalador após instalar Inno Setup por fonte oficial autorizada: `ISCC installer/PHsystem.iss`. Instalação por usuário, sem administrador. Desinstalação preserva o histórico local; apague-o pela UI se desejado. Máquina limpa, atualização, assinatura e certificado de distribuição permanecem pendentes.

## API sem serviços configurados

```powershell
dotnet run --project src/PHsystem.Server
dotnet run --project src/PHsystem.Admin
```

Por padrão só fazem bind em loopback: API 5080 e Admin 5081. Health endpoints são legíveis; mutações exigem HTTPS. API sem ConnectionStrings__Community retorna 503. Admin sem Backend__BaseUrl HTTPS retorna indisponível. Não ignorar validação TLS nem adicionar bypass de certificado para desenvolvimento.

## Backend de desenvolvimento com PostgreSQL

1. Instale Docker/Podman e certificado HTTPS de desenvolvimento somente com a autorização necessária ao seu ambiente. A imagem atual foi compilada e verificada em contêiner isolado; somente a API local foi atualizada, preservando banco e volumes.
2. Copie `.env.example` para `.env` local ignorado; preencha uma senha nova para o banco e os diretórios de certificado/lista de hashes. Não compartilhe esses valores. ASP.NET Core executado diretamente não lê `.env` automaticamente: configure as variáveis do processo por canal seguro. Docker Compose lê `.env` para interpolação.
3. Forneça `dev.pfx` e sua senha em configuração segura. A conta não privilegiada do container precisa conseguir ler o arquivo. Configure confiança/cadeia e hostname também entre Admin e API; não desative TLS para fazer o proxy funcionar.
4. Forneça lista offline revisada `compromised-sha256.txt`, um digest SHA-256 hexadecimal por linha. Não inclui senhas em texto. Sem essa dependência o validador recusa cadastro/reset; listas vazias são recusadas e uma lista insuficiente não constitui triagem útil. Falta processo de atualização e teste de escala.
5. Revise compose e os scripts SQL gerados antes de aplicar. Todos os binds publicados pelo compose são loopback. Mailpit é apenas captura de email de teste, sem envio externo. Não usar dados reais.

```powershell
dotnet tool restore
dotnet ef migrations script --project src/PHsystem.Server --idempotent -o artifacts/community-migrations.sql
docker compose up -d postgres mailpit
docker compose run --rm migrate
docker compose up --build api admin
```

A migração é uma ação explícita, não ocorre no startup comum. Para execução direta com ConnectionStrings__Community configurada: `dotnet run --project src/PHsystem.Server -- --migrate`. A API não inventa um banco fallback. Persistir e proteger Data Protection keys é necessário para manter/invalidar cookies corretamente entre reinícios; o compose possui volume dedicado.

O desktop usa services.json da distribuição, sem campo de URL. Em Debug, StartDevelopment.ps1 inicia os serviços e grava descoberta de porta dinâmica fora do repositório; Release ignora essa descoberta e variáveis de desenvolvimento. Faça registro/login por ação explícita. Confirmação/reset recebem tokens pelo email configurado; as telas atuais aceitam ID/token. Cookies ficam em memória, com persistência DPAPI CurrentUser apenas por opt-in. Veja [API-CONNECTION](API-CONNECTION.md). Enrollment/recuperação MFA pela UI desktop ainda precisam de fluxo completo; endpoints setup/enable existem e são testados via HTTP.

## Proprietário e administração

Após migrar e configurar lista de senhas/ConnectionStrings em canal local seguro:

```powershell
dotnet run --project tools/ProvisionAdmin -- --provision-owner
```

A CLI é interativa, não ecoa senha/TOTP, exige senha forte 16–128 e confirma MFA antes do papel Admin. Recusa reaproveitar/elevar uma conta Zphzin existente. Ela não foi executada contra banco real; nenhum proprietário foi criado. Não registrar o segredo de enrollment no terminal compartilhado/logs. A tool não pode garantir remoção de strings da memória gerenciada. Recuperação de emergência ainda requer runbook de dupla revisão, prova de autoridade, revogação de sessões e auditoria; não há fallback que reassume a conta por nome.

O painel Admin é separado. O proxy encaminha somente rotas allowlist para Backend__BaseUrl HTTPS, sem cookie jar compartilhado entre usuários; a API verifica papel, MFA e autenticação recente. O painel atual lê auditoria. Gestão completa de usuários/papéis, moderação visual e reautenticação guiada estão pendentes.

## Produção e GitHub

Produção requer domínio, TLS válido, email real, PostgreSQL, storage isolado, proteção de chaves, backups testados, monitoramento, gestão de segredos, revisão de licenças/privacidade e orçamento aprovado. Custos dependem do provedor; nenhum plano gratuito é presumido. Não usar o PC do usuário como servidor público.

GitHub remoto, proprietário, visibilidade, branch protection, Projects, Issues e Releases não foram configurados. Actions Windows/Linux estão preparadas, sem alegação de execução remota. Pages é manual (`workflow_dispatch`) e exige autorização de publicação. O site estático não contém API, banco, segredo, analytics ou dados privados. CI não substitui revisão de produção.

## Conexão automática — atualização 2026-11-16

As portas de host de API/PostgreSQL/Mailpit no Compose agora são dinâmicas. Para o fluxo recomendado, utilize tools/StartDevelopment.ps1 com compose.env fora do repositório; o script usa docker compose port e valida /health por TLS antes de iniciar o desktop. Não é um serviço instalado no PC do usuário final. O empacotamento -Production exige domínio HTTPS e readiness válido; não foi realizado deploy. Instruções completas em API-CONNECTION.md.
