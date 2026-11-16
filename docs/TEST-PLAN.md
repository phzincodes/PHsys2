# Plano e evidências de teste

Comandos mínimos: `dotnet restore PHsystem.slnx`, `dotnet build PHsystem.slnx`, `dotnet test PHsystem.slnx --logger trx`, `dotnet publish src/PHsystem.Desktop -c Release -r win-x64 --self-contained true -o artifacts/desktop`. TRX em TestResults é artefato local ignorado.

Suíte | Escopo real | Limites
---|---|---
UnitTests | snapshot/score nulo, cache e formatos de assets, caminhos, coordenação, timeout, cancelamento, paralelismo, circuit breaker, regras, driver policy e prioridade simulada | não testa instala/assinatura real de driver
IntegrationTests | SQLite real, migrações locais, retenção, leitura entre instâncias e unicidade; PostgreSQL opcional | PostgreSQL é ignorado quando não configurado
SecurityTests | HTTP com Identity real e SQLite/email de teste; CSRF, roles, MFA/antirreuso, reset, rate limit, BOLA, mensagens opt-in, privacidade, exclusão; limpeza em fixtures e junction | não é auditoria ASVS; sem email real, infraestrutura distribuída ou VM destrutiva
WindowsTests | leituras nativas básicas nesta máquina e ausência de chaves identificadoras intencionais | hardware único, sem Windows 10 ou falhas de todos os drivers
UiTests | carga de XAML em STA, navegação sem coleta, comandos iniciais, prévia de compartilhamento privada | não é automação completa por UI Automation; sem matriz DPI/leitor de tela
E2ETests | coleta Windows → SQLite → consumidores de snapshot | não cobre backend remoto nem instalador

## PostgreSQL real

Use somente banco exclusivo com nome `phsystem_test_*`, acessível por PHSYSTEM_TEST_POSTGRES. O teste aplica migrações e faz transação de dados de fixture, depois rollback. Não use credenciais de produção. Execute `dotnet test tests/PHsystem.IntegrationTests --filter Database=PostgreSQL --logger trx`. A configuração de CI Linux fornece serviço PostgreSQL isolado; ainda não foi executada remotamente.

## Falhas encontradas e corrigidas nesta continuação

- Conflito de versões DI/EF: dependências alinhadas a 10.0.15.
- Erros de inferência de `new` em teste params, namespaces WPF e palavra reservada `field` do C# 14: corrigidos e compilação repetida.
- Busca de TabControl antes de realização visual: teste passou a percorrer árvore lógica real do XAML, mantendo a verificação de navegação.
- CSRF antes de autenticação: teste HTTP expôs 400 em sessão válida; pipeline corrigido e fluxo autenticado testado.
- Contagem de taxa agrupada indevidamente entre operações de conta: política passou a usar IP/rota normalizada; teste de 429 explícito.
- Consultas de datas do backend: timestamps UTC DateTime para comportamento consistente entre PostgreSQL e SQLite de teste.

## Ainda obrigatório antes de liberar

Matriz Windows 10/11 e hardware ausente, WUA/SetupAPI reais, picos de memória/CPU, longas coletas canceladas, links/hardlinks/ACL sob corrida adversarial, histórico antes/depois, PostgreSQL real local/CI, SMTP/TLS, tokens expirados por relógio controlado, recuperação MFA, concorrência de cadastro/reputação/mensagens, upload hostil, testes de contratos e edição de conteúdo, DPI 100–200%, 4K, alto contraste, teclado/leitor de tela, instalador em VM limpa, desinstalação e rollback de atualização. Não marcar itens como aprovados por existir um projeto.
