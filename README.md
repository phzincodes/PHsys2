# PHsystem Next

**Prévia local0.3.0:** instalador real em [download local HTTPS](https://localhost:5178/download). Tema/widgets persistentes,18 coletores, avaliação por faixa com cobertura, investigação de travamentos e relatório reduzido. Google tem infraestrutura testada por mocks, mas depende de credenciais e validação real. Não é produto completo nem release pública assinada. Consulte [estado atual](docs/STATUS.md) e [validação](docs/VALIDATION.md).

Aplicativo Windows em desenvolvimento, escrito do zero em .NET 10 / WPF, com backend ASP.NET Core. Não contém motores ou scripts do PHsystem antigo.

O diagnóstico local já coleta informações reais, registra snapshots em SQLite e alimenta inventário de drivers, recomendações e histórico. Limpeza atua somente sobre temporários elegíveis após prévia e confirmação. A comunidade possui API real e feed WPF integrado. O diagnóstico local abre sem conta e funciona offline. A autenticação é exigida somente para os recursos online. O responsável pela distribuição configura a API hospedada, PostgreSQL, e-mail e triagem de senhas; o usuário final não digita URLs nem inicia serviços. Sem esses serviços, a tela informa indisponibilidade e não simula autenticação. O motor de diagnóstico continua local; hardware não é sincronizado automaticamente.

**Não é uma release de produção.** Nem todos os requisitos de M0–M8 estão concluídos. Veja o [estado verificável](docs/STATUS.md), as [pendências por marco](docs/BACKLOG.md) e o [plano de testes](docs/TEST-PLAN.md). Ausência de evidência não produz score perfeito; HealthScore permanece indisponível.

## Executar no VS Code

Abra `phsystem-next.code-workspace`. Nesta máquina, as tarefas usam SDK 10.0.404 em `D:\PHsystemTools\dotnet` e cache em `D:\PHsystemTools\nuget`. Em outra máquina, instale o SDK .NET 10 por canal oficial e ajuste os caminhos das tarefas/workspace. O app publicado dispensa SDK e IDE.

Terminal → Executar Tarefa → restore, build, test e desktop. Após o provisionamento único de desenvolvimento, F5 → PHsystem Desenvolvimento (serviços automáticos) inicia os serviços e descobre a porta sem digitar URLs. Sem Docker/configuração, o preflight explica a pendência; coloque breakpoint em `MainWindow.xaml.cs` ou `ScanCoordinator.cs`.

```powershell
dotnet restore PHsystem.slnx
dotnet build PHsystem.slnx
dotnet test PHsystem.slnx --logger trx
dotnet run --project src/PHsystem.Desktop
```

Clique em ANALISAR no início, mesmo sem entrar em uma conta. Acompanhe os coletores reais, cancele se necessário e consulte o snapshot nas abas de resultados. Navegar entre abas não dispara nova varredura. Exportar diagnóstico grava informações privadas no destino escolhido; o resumo para compartilhar é separado e reduzido.

## Gerar o pacote Windows local

```powershell
dotnet publish src/PHsystem.Desktop -c Release -r win-x64 --self-contained true -o artifacts/desktop
./tools/PackageDesktop.ps1
```

Abra `artifacts/desktop/PHsystem.Desktop.exe` com toda a pasta. Não é single-file. O script também produz ZIP e SHA-256; o instalador possui fonte Inno Setup, mas depende do compilador e de teste em máquina limpa. Assinatura de código e atualização automática segura ainda pendentes.

## Backend e administração

Consulte [DEPLOYMENT](docs/DEPLOYMENT.md). Sem ConnectionStrings__Community, a API retorna 503 para comunidade; não troca PostgreSQL por banco fictício. A conexão é descoberta automaticamente a partir de services.json empacotado. Não existe campo de URL na interface. Sem infraestrutura, aparece “Não foi possível conectar aos serviços do PHsystem.” com Tentar novamente. Consulte [conexão e provisionamento](docs/API-CONNECTION.md). A opção Lembrar sessão protege somente o cookie com DPAPI e revalida a sessão ao abrir; nesse caso há conexão automática de autenticação, sem envio do diagnóstico. Não envie inventários reais durante desenvolvimento.

Os testes HTTP usam Identity real com SQLite isolado e email de teste. Em 2026-11-16, migrações, Identity e concorrência de reações também passaram em PostgreSQL real exclusivo; isso não comprova entrega de email externo. O teste PostgreSQL aparece como ignorado quando PHSYSTEM_TEST_POSTGRES não está configurado. CI Linux contém esse serviço, mas ainda não foi executada remotamente.

## Reformulação atual

14 áreas, ícones centralizados, início focado, comparação de histórico, movimento reduzido e sons locais opcionais. Feed real com paginação, votos, salvos, categorias seguidas, respostas encadeadas, edição própria, perfis e notificações. Uploads, avatares, push da timeline e outros itens do [roadmap](docs/ROADMAP.md) continuam pendentes. Consulte a [auditoria](docs/AUDITORIA-UX-ATUAL.md) e as [evidências de teste](docs/TESTES.md).

## Documentação

- [Arquitetura](docs/ARCHITECTURE.md), [contrato de coleta](docs/SCAN-CONTRACT.md), [drivers](docs/DRIVERS.md)
- [Segurança](docs/SECURITY.md), [privacidade](docs/PRIVACY.md), [checklist LGPD](docs/LGPD-CHECKLIST.md)
- [Moderação](docs/COMMUNITY-MODERATION.md), [implantação local](docs/DEPLOYMENT.md), [testes](docs/TEST-PLAN.md)
- [Imagens e slots](assets/README-IMAGENS.md), [contribuição](CONTRIBUTING.md), [changelog](CHANGELOG.md)

O site estático está em `site/`. Não há analytics, repo remoto, release pública ou deploy ativo. GitHub/Pages só serão conectados e publicados mediante autorização específica.

## Entrega V2 — fluxos essenciais

Limpeza informa o escopo, os motivos dos itens ignorados e diferencia bytes removidos da variação observada no volume. Curtir/retirar curtida recebe confirmação do servidor sem recarregar o feed inteiro. Configurações identifica a versão e o build efetivo. Consulte [auditoria](docs/AUDITORIA-CORRECOES.md) e [validação atual](docs/VALIDATION.md). O site ainda é estático; Google, quarentena e instalador validado continuam pendentes.

## Red Eclipse V3

Prévia Windows: `artifacts/red-eclipse-rc/PHsystem.Desktop.exe`, atalho **PHsystem Next - Red Eclipse**. Web integrada local: **https://localhost:5178/** (Compose provisionado). Agora o frontend foi reconstruído e usa a mesma API; veja [WEB](docs/WEB.md), [EMAIL](docs/EMAIL.md) e [validação atual](docs/VALIDATION.md). Seis temas persistentes, dashboard com evidências e testes de rede sob demanda. Não é uma release pronta: navegador bloqueado nesta sessão, Google/score/instalador/entrega externa continuam pendentes.
