# PHsystem Next

Código novo; não importar motores antigos. .NET 10, WPF, monólito modular. UI sem elevação. Nunca executar comandos da comunidade. Dados locais privados. Nenhuma publicação ou instalação sem autorização explícita.

Validar: dotnet restore PHsystem.slnx; dotnet build PHsystem.slnx; dotnet test PHsystem.slnx; dotnet publish src/PHsystem.Desktop -c Release -r win-x64 --self-contained true -o artifacts/desktop.

Atualizar docs/STATUS.md com evidências reais; testes vazios não contam como cobertura. Não declarar módulos operacionais por existirem projetos.

O prompt mestre do usuário exige marcos M0–M8 incrementais; lacunas verificáveis estão em docs/BACKLOG.md. Não marcar o produto completo enquanto houver requisitos pendentes. Continuar escolhas técnicas rotineiras sem nova confirmação. Aprovação específica continua necessária para ações destrutivas em dados reais, custos, credenciais, privilégios administrativos, publicação pública e serviços externos.

Ambiente local: SDK em D:\PHsystemTools\dotnet; NUGET_PACKAGES em D:\PHsystemTools\nuget; telemetria CLI desativada. Preserve alterações do usuário, inclusive global.json, sem sobrescrever sua configuração. Nenhuma identidade Git pessoal deve ser inventada; commits desta automação podem usar identidade explicitamente identificada como Codex Automation.

Testes de backend usam Identity real com SQLite/email de teste. PostgreSQL real requer PHSYSTEM_TEST_POSTGRES com banco exclusivo phsystem_test_*; ausência deve aparecer como ignorado, nunca aprovado. Docker/Podman, gh e Inno Setup estavam ausentes na última inspeção. Não inferir sucesso de CI remota, entrega de email, instalação de driver ou instalador.

Fotos/artes não devem ser geradas automaticamente. Capturas da janela podem conter hardware privado: manter em artifacts ignorado, sem copiar para site/GitHub sem revisão e autorização. Não executar tools/scaffold.py para recriar a solução sobre a implementação existente.


Atualização verificada 2026-11-16: Docker/Compose e Inno Setup estão presentes; ISCC em C:/Program Files (x86)/Inno Setup 6/ISCC.exe (compilador com assinatura válida). BuildInstaller.ps1 gera prévia sem executá-la. Não inferir instalação limpa/assinatura do app/CI remota. API/web local compartilham https://localhost:5178; Google não configurado, broker nativo desligado por padrão. Snapshot agora possui18 coletores; todos mantêm escopo parcial. Saúde é faixa ponderada enquanto integridade não implementada; não declarar nota completa. AccessiblePageHost é necessário para expor páginas ao UIA. Preservar as alterações particulares de global.json e StartDevelopment.ps1 e seu backup.
