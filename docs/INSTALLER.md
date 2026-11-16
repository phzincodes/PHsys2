# Instalador Windows — prévia 0.3.0

O Inno Setup existente foi encontrado em C:/Program Files (x86)/Inno Setup 6/ISCC.exe e sua assinatura foi validada antes do uso. Nenhuma ferramenta foi instalada.

```powershell
./tools/BuildInstaller.ps1 -Dotnet D:/PHsystemTools/dotnet/dotnet.exe
```

O script gera publish self-contained win-x64, ZIP, instalador e receipt/checksum em artifacts/releases. O instalador usa privilégios lowest, diretório por usuário, seleção de pasta habilitada e atalho desktop opcional. DLLs/runtime/assets acompanham o app; o usuário não precisa de SDK/Docker/PostgreSQL para diagnóstico local. Histórico fica fora da pasta instalada e não é removido pelo desinstalador.

Fonte installer/PHsystem.iss, versão alinhada0.3.0. App comum não tem API de produção configurada nesta prévia: diagnóstico funciona offline; comunidade instalada só será operacional com distribuição configurada para uma API hospedada real. BuildInstaller gera apenas a prévia, não uma distribuição de produção pronta. Não incluir descoberta Debug/credenciais em release.

Web local https://localhost:5178/download mostra versão, tamanho, hash e assinatura do receipt; /download/windows transfere o arquivo real de nome fixo montado readonly. /api/v1/releases/current é metadado público independente do banco. Ausência/receipt inválido dá404; nenhum link de sucesso fictício. A transferência HTTPS foi comparada ao SHA-256 original.

O artefato está SEM assinatura digital e NÃO foi executado. Não foram testados instalação limpa, escolha de disco real, upgrade/desinstalação ou preservação em VM. Updater não está implementado; nenhuma alegação de atualização automática segura. Assinatura requer certificado/autorização, e release pública exige auditoria/infra/testes/permissão. Não usar artifacts de CI expirantes como distribuição permanente.

Workflow Windows tenta compilar quando Inno está disponível e emite aviso explícito quando ausente; não instala a ferramenta nem publica release. CI remota não foi executada nesta sessão.
