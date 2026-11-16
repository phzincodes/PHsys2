# Instalador por usuário

Fonte: PHsystem.iss, Inno Setup. Antes de compilar, gere `artifacts/desktop` com dotnet publish conforme docs/DEPLOYMENT.md; depois use `ISCC installer/PHsystem.iss` num ambiente com compilador oficial instalado/autorizado.

Configuração: x64, PrivilegesRequired=lowest, instalação no LocalAppData/Programs, atalho e desinstalador. Não limpa histórico do perfil ao desinstalar. Preserva assets e dependências, sem alegar single-file.

Compilador Inno Setup ausente nesta máquina; instalador ainda não compilado nem validado em VM limpa. Assinatura, atualização segura, ícone fornecido e política de release permanecem pendentes. O ZIP local pode ser produzido por tools/PackageDesktop.ps1, sem instalação ou publicação.
