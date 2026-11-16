# Testes e evidências da reformulação

## Ambiente e comandos

Windows local, SDK .NET 10.0.404, cache `D:\PHsystemTools\nuget`. O workspace configura as tarefas do VS Code. O `global.json` alterado pelo usuário foi preservado fora dos commits desta revisão.

```powershell
dotnet restore PHsystem.slnx
dotnet build PHsystem.slnx --no-restore
dotnet test PHsystem.slnx --no-build --logger trx
dotnet publish src/PHsystem.Desktop -c Release -r win-x64 --self-contained true -o artifacts/desktop
```

Os comandos correspondentes foram executados com o executável `D:\PHsystemTools\dotnet\dotnet.exe`. TRX ficam em `tests/*/TestResults`, ignorados pelo Git. O total final e o pacote estão registrados em [STATUS](STATUS.md).

## Cobertura adicionada

- `IconTests`: limites de geometrias assimétricas, negativas e grandes nas escalas 100/125/150/200%.
- `LayoutMatrixTests`: XAML real, quatro resoluções × quatro tamanhos lógicos equivalentes a DPI, menu expandido/recolhido, mesmo centro horizontal dos ícones e scrollbar fina. Nenhuma coleta é acionada. Isso não simula todos os eventos de mudança de DPI de um monitor físico.
- `AuthenticationTests`: serviço ausente não autentica, confirmação/aceite antes de rede, senhas 8/128/129 sem truncamento.
- `SessionVaultTests`: DPAPI CurrentUser real, ausência de cookie em texto puro, adulteração, expiração e exclusão.
- `SessionPrivacyTests`: logout limpa feed/mensagens/notificações/rascunhos e preserva snapshot local.
- `PreferenceTests`: persistência, limites de volume, NaN, modo silencioso e todos os cues desligados.
- `ResultsTests`: comparação sem alterar snapshots, dado removido distinto de melhora e destinos de navegação válidos.
- `ScanCoordinatorTests`: progresso factual para coleta, cache e falha; testes anteriores de concorrência/cancelamento/timeout preservados.
- `CommunityApiTests`: Identity/PBKDF2, HTTP real no TestServer, SQLite controlado, paginação de 25 posts, votos idempotentes, salvos/seguidos privados, edição/revisões somente pelo autor, notificações por destinatário, threads válidas e inválidas, aceite sem revisão técnica automática, perfil público e exclusão de novos dados sociais.
- `HardwareSharingTests`: rejeição de padrões de caminhos, e-mail, IP/MAC, UUID e credenciais; modelos técnicos continuam elegíveis somente por opt-in.

As fixtures são exclusivas de testes. E-mail de teste não comprova entrega real. SQLite de teste não comprova migração PostgreSQL. O teste PostgreSQL usa `PHSYSTEM_TEST_POSTGRES` e é ignorado quando ausente.

## Evidência visual local

- `artifacts/ux-auth-welcome-updated.png`: captura da janela executada, entrada real, sem sessão simulada.
- `artifacts/ux-validation/test-shell-empty-home.png`: render do shell real em teste STA, vazio e sem dados pessoais. O teste controla a visibilidade do shell para inspecionar o layout; não há bypass equivalente no aplicativo.
- Defina `PHSYSTEM_UI_CAPTURE_DIRECTORY` para gerar novamente a captura de teste. A CI Windows está configurada para armazenar TRX e essa captura como artefatos, mas não foi executada remotamente nesta sessão.

## Falhas encontradas e corrigidas

Template GroupItem com ContentSource inválido; ProgressBar com binding TwoWay de propriedade somente leitura; conversor ausente na carga isolada; corrupção de acentos por encoding Windows; concorrência entre testes STA no cache BAML global; largura mínima nativa de 17 DIP impedindo scrollbar de 8 DIP. Os testes UI são serializados porque o cache de recursos WPF é compartilhado pelo processo.

## Ainda não comprovado

Login desktop completo contra serviços implantados; PostgreSQL local real; SMTP real; uploads; push/reconexão da timeline; avaliação auditiva; acessibilidade por leitor de tela; alto contraste completo; mudanças reais de monitor/DPI; instalador em máquina limpa; assinatura e CI remota. Não atribuir aprovação a esses itens.

## Conexão automática — 2026-11-16

Novos ApiConnectionTests, EndpointConfigurationTests e DesktopBackendTests: configuração central e isolamento Release/Debug, serviço ausente, retries GET, ausência de retry em POST, protocolo incompatível, prazo sem cancelamento do usuário, origem do vault, recuperação após login incorreto e TLS real em Kestrel com portas dinâmicas. API de produção e Windows trust não são alterados pelos testes.

Resultado: 135 testes aprovados e 1 PostgreSQL ignorado na validação atual (38 UI/API, 40 segurança, 52 unidade, 3 integração, 1 Windows, 1 E2E). Testes HTTP/TLS usam Identity real, SQLite e mailbox isolados; não equivalem a confirmação de e-mail pela internet ou instalação limpa. Captura da versão publicada: artifacts/api-auto-unavailable.png. Ver API-CONNECTION.md para limites e implantação.
