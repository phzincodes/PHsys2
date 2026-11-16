# Conexão automática, desenvolvimento e produção

## Usuário final

O aplicativo não pede endereço da API, não instala PostgreSQL e não inicia backend local. A distribuição contém `services.json` com a raiz HTTPS pública escolhida pelo responsável pelo serviço. A configuração não contém credenciais.

Ao abrir, o desktop verifica `/health` e o protocolo `PHsystem.Next` versão 1. São no máximo três tentativas de GET, três segundos por tentativa e prazo total de onze segundos. Erro de TLS interrompe as tentativas. Nenhum cadastro, login ou reset é reenviado automaticamente. Sem conexão aparece **“Não foi possível conectar aos serviços do PHsystem.”** e **Tentar novamente**. O carregamento termina e nenhum sucesso é simulado.

O pacote local desta sessão continua sem domínio de produção: não há servidor hospedado, PostgreSQL/SMTP de produção ou credenciais disponíveis. Remover o campo de URL não cria essa infraestrutura. Não distribuir esta prévia como um serviço online pronto.

## Configuração central

Responsáveis: `Desktop/ApiEndpointConfiguration.cs`, `ApiConnectionService.cs`, `CommunityClient.cs`, `AuthenticationViewModel.cs` e `services.json`.

Ordem em **Debug**, quando o manifesto não exige Production:

1. `PHSYSTEM_API_URL`, somente para automação do desenvolvedor, não para usuários finais.
2. `%LOCALAPPDATA%/PHsystemNext/development/api-discovery.json`, produzido pelo inicializador, HTTPS loopback, schema 1 e validade limitada.
3. Endpoint do manifesto instalado.

Em **Release**, somente o manifesto instalado é utilizado. Variáveis e descoberta de desenvolvimento são ignoradas. Não existe varredura de rede, tentativa de portas conhecidas, domínio inventado ou envio de senha a URLs de posts. Endereços com HTTP, credenciais, query, fragmento ou subcaminho são recusados; produção também recusa loopback e domínios `.invalid`/`.localhost`.

Conexões para a mesma origem preservam cookies. Um cookie DPAPI só pode ser restaurado para a origem atualmente configurada; o vault não escolhe o servidor. Dados de diagnóstico não participam do protocolo de autenticação. Persistência de sessão é opt-in; expiração e security stamp são validados pelo servidor. Revogação global existe; gestão detalhada por dispositivo ainda é pendente.

## Provisionamento único do desenvolvedor

Esses passos pertencem ao ambiente de desenvolvimento, nunca ao instalador do usuário final:

1. Disponibilize Docker/Compose por instalação autorizada e mantenha o daemon ativo. Eles não estão instalados nesta máquina.
2. Configure e confie no certificado ASP.NET local do usuário. `dotnet dev-certs https --check --trust` apenas verifica; se necessário, o responsável deve autorizar `dotnet dev-certs https --trust`. Nesta máquina o certificado existe e é válido, mas ainda não está confiável. Esta sessão não alterou a confiança do Windows.
3. Exporte esse certificado para `dev.pfx` em diretório privado externo ao repositório. Use uma senha nova e secreta. No Windows, `Export-PfxCertificate` permite receber a senha como SecureString, sem colocá-la em linha de comando. Não versionar o PFX.
4. Disponibilize uma lista revisada de hashes SHA-256 de senhas comprometidas em diretório privado externo. O lookup não substitui o hash PBKDF2 do Identity. Não usar uma lista fictícia como proteção de produção.
5. Copie os **nomes** de `.env.example` para `%LOCALAPPDATA%/PHsystemNext/development/compose.env`, fora do repositório. Preencha `PHSYSTEM_DEV_DB_PASSWORD`, `PHSYSTEM_DEV_CERT_PASSWORD`, `PHSYSTEM_DEV_CERT_DIRECTORY` e `PHSYSTEM_PASSWORD_LIST_DIRECTORY`. Nunca imprimir `docker compose config`, enviar esse arquivo ou gravar valores reais nos documentos.

Depois desse provisionamento:

```powershell
./tools/StartDevelopment.ps1
```

Ou F5 → **PHsystem Desenvolvimento (serviços automáticos)** no VS Code. O script compila os containers, inicia PostgreSQL/Mailpit, aplica migrations, inicia a API, consulta a porta publicada pelo Docker, valida TLS/readiness e só então inicia o desktop Debug. O usuário não descobre portas nem digita URLs. A caixa de e-mail local é indicada automaticamente no terminal; Mailpit não comprova entrega para a internet. `-EnvironmentFile` permite outro arquivo externo; `-NoDesktop` serve ao depurador.

Portas internas de protocolo são fixas dentro dos containers; portas do host são atribuídas dinamicamente e limitadas a loopback. O inicializador não instala serviço Windows, não inicia produção no PC do usuário e não cria contas.

Atualização 2026-11-16: Docker 29.9.3 e Compose estão disponíveis; API/PostgreSQL/Mailpit existentes responderam localmente e /health retornou ready com TLS normal. A imagem atual foi compilada em Linux e verificada sem root. Somente a API local foi atualizada, preservando banco/volumes; não foi validado o provisionamento integral numa máquina limpa.

## API hospedada: roteiro do operador

1. Escolha domínio e infraestrutura próprios, com custos e publicação previamente autorizados. Prepare PostgreSQL, storage futuro, backups, monitoramento e canal de incidentes.
2. Forneça segredos por cofre/variáveis do servidor: conexão PostgreSQL, SMTP real, lista revisada de hashes e certificado. Não colocar senha no app desktop. Configure `AllowedHosts` para o domínio correto e persista/proteja as chaves Data Protection entre reinícios e instâncias.
3. Aplique migrations com `dotnet PHsystem.Server.dll --migrate`, usando o mesmo ambiente seguro do servidor. Faça backup antes de migrar bases existentes. As migrations já existem; esta alteração de conexão não mudou o modelo EF.
4. Configure HTTPS com certificado válido para o domínio. Pode terminar TLS no Kestrel ou manter HTTPS entre proxy e Kestrel. O código não passa a confiar em `X-Forwarded-Proto` arbitrário; um proxy HTTP exige configuração explícita e limitada de proxies confiáveis antes de usá-lo. Não desativar TLS nem publicar a porta HTTP de desenvolvimento como API de autenticação.
5. Valide `/health/live` (processo) e `/health` (banco, schema/migrations e flags de login/cadastro/recuperação). Banco falho ou migration pendente gera 503. SMTP configurado **não prova entrega**: execute teste real do provedor, confirmação e recuperação antes de liberar usuários. Health público não expõe conexão, tabelas, contagem de usuários, senhas ou stack traces.
6. Defina o domínio público na automação de distribuição, por exemplo numa variável não secreta `PHSYSTEM_RELEASE_API_URL`. Gere o pacote com:

```powershell
./tools/PackageDesktop.ps1 -Production -ApiBaseUrl $env:PHSYSTEM_RELEASE_API_URL
```

O empacotador recusa domínio ausente/inválido, TLS inválido, protocolo incompatível e cadastro/login indisponíveis. Grava o endpoint em `artifacts/desktop/services.json` antes de gerar ZIP/checksum. Um `dotnet publish` comum gera apenas prévia sem endpoint se nenhum domínio tiver sido configurado; não é evidência de produção pronta.

7. Assine instalador/artefatos quando o certificado de distribuição existir. Teste instalação limpa sem SDK, cadastro, entrega de e-mail, login, expiração, revogação e comunidade. Só publique após autorização e validação. Nenhum desses passos externos foi realizado nesta sessão.

## Testes desta correção

Testes novos cobrem configuração ausente, origem insegura, Release ignorando descoberta local, serviço disponível/indisponível, reconexão, protocolo incompatível, TLS, cancelamento, ausência de retries em POST e cookies vinculados à origem. O teste desktop/backend usa **Kestrel real, porta dinâmica e TLS**, com CA isolada confiada apenas pelo handler de teste: não instala certificados e não há callback que aceite certificado inválido. Outro cliente usa a validação normal do Windows e rejeita esse certificado não confiável.

O fluxo integrado usa Identity/PBKDF2 e SQLite isolado, com mailbox exclusivamente de teste: registro, confirmação, duplicidades, senha inválida, login correto/incorreto, DPAPI, restauração, revogação, recuperação e logout. Isso comprova contrato desktop/API e comportamento HTTP/TLS, não banco PostgreSQL real nem entrega SMTP. PostgreSQL sem `PHSYSTEM_TEST_POSTGRES` aparece ignorado. Instalação limpa em VM, API pública e e-mail real permanecem pendentes.

Referências: [health checks ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0), [certificados de desenvolvimento](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs).

## Diagnóstico de infraestrutura V2

Execute `./tools/TestDevelopment.ps1` antes de iniciar os serviços. `-Builder nome` permite inspecionar um builder específico sem selecioná-lo nem apagar cache. `StartDevelopment.ps1` usa o builder selecionado no ambiente, sem nome obrigatório; nesta máquina o operador selecionou `phsystem-clean`. Falha de armazenamento não autoriza prune/reset de volumes.

O Dockerfile prepara o diretório Data Protection com o UID da imagem para volumes novos. Isso não corrige volumes existentes: diagnostique ownership/permissões de forma direcionada, preserve backups e nunca aplique chmod amplo como atalho. As correções de descoberta de porta feitas pelo usuário em StartDevelopment.ps1 foram preservadas.

O diagnóstico local independe desta conexão. Somente comunidade/conta usam a API; o release continua precisando do domínio HTTPS configurado durante o empacotamento. A API em execução pode ser de build anterior: compare Configurações e /health após uma atualização explicitamente planejada.
