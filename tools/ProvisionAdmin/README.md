# ProvisionAdmin — somente autoridade local

`dotnet run --project tools/ProvisionAdmin -- --provision-owner`

Requer PostgreSQL já migrado, ConnectionStrings__Community e lista de hashes de senhas comprometidas configurados por canal seguro. A ferramenta não lê senha de argumento/arquivo, não ecoa caracteres, exige 16–128 caracteres e confirmação TOTP antes de atribuir Admin. O segredo de enrollment é exibido apenas na sessão interativa para cadastro no autenticador; não capturar essa sessão em logs/gravações.

Nome proprietário reservado Zphzin, unicidade normalizada pelo Identity. Se a conta já existir, retorna sem alterar credencial/papel. Falha de MFA reverte a transação. Não existe senha embutida, seed ou mecanismo de assumir conta existente por nome. Recuperação de emergência ainda precisa de runbook revisado; não recriar o proprietário manualmente sem prova de autoridade, auditoria e revogação.

Compilado, mas não executado contra banco real nesta sessão. Nenhum administrador real criado. Não substitui revisão de produção. Strings sensíveis existem temporariamente na memória gerenciada e não podem ser apagadas com garantia pela tool.
