# Privacidade — rascunho de engenharia

Não é uma política jurídica final. Controlador, contato do encarregado, bases legais, público elegível e prazos operacionais devem ser definidos antes de operação pública. Não coletar dados pessoais reais em um ambiente remoto de desenvolvimento.

## Inventário atual

Dados | Finalidade técnica | Local / padrão | Retenção atual
---|---|---|---
Fatos técnicos sem hostname, IP, MAC ou serial intencional | diagnóstico e orientação | SQLite no perfil local | até 100 snapshots; exclusão na UI
Nomes de arquivos temporários | revisar limpeza | memória local durante a prévia | até nova prévia/encerramento
Conta, email, hash de senha e MFA | autenticação | PostgreSQL configurado, sem ativação automática | política do servidor ainda pendente
Campos públicos escolhidos | perfil técnico público | tabela pública separada; vazio por padrão | substituição ou retirada explícita
Posts/respostas | comunidade | servidor, após ação do usuário | política ainda pendente; exclusão oculta/remove conteúdo do titular
Mensagens | comunicação opt-in | servidor, sem E2EE | política ainda pendente; exclusão da conta remove mensagens vinculadas
Auditoria | rastreabilidade de ações | servidor, IDs pseudônimos e ação | prazo operacional pendente

O banco local não é criptografado: proteção depende do perfil Windows e de backups do usuário. Exportar diagnóstico cria um arquivo privado no destino escolhido; apagar o histórico não remove exportações. O resumo local de comunidade contém somente nomes/estados das categorias, sem fatos ou erros brutos.

O cliente não transmite inventário automaticamente. Para publicar hardware, a UI prepara valores do snapshot com todos os campos desmarcados, permite revisar e exige Publicar. Retirar compartilhamento envia conjunto vazio e preserva o diagnóstico local. O servidor aceita somente chaves públicas permitidas. Texto digitado voluntariamente em posts/mensagens continua podendo conter dados pessoais; a detecção de risco não é garantia de remoção de PII.

Exportação de conta e anonimização existem na API. A anonimização retém conta tombstone para integridade referencial e IDs em auditoria, remove credenciais/perfil público e conteúdo principal do titular. Backups, evidências de revisão, logs e eventual obrigação de retenção exigem política/revisão antes do lançamento; não há promessa de apagamento irrestrito.

Analytics, publicidade, venda de dados e sincronização de hardware automática não são implementados. A equipe não recebe acesso ao inventário local. Mensagens não têm criptografia ponta a ponta.
