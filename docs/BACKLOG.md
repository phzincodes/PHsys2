# Backlog — atualização 0.3.0

Entregues nesta continuação:18 coletores, faixa ponderada/relatório/Crash Investigator, temas/widgets persistentes, peer acessível, Google sob configuração e instalador/download local. O histórico abaixo contém itens parcialmente superados; STATUS/VALIDATION descrevem o aceite atual.

Restante de código: integridade real/helper IPC restrito, central/histórico de reparos e rollback; fontes/instalação de drivers/SMART/WUA; updater assinado; perfil público/ranking opt-in/reputação/anexos/tipos da comunidade; gestão administrativa completa; vinculação de métodos/MFA Google e sessões individuais. Não tratar tudo isso como simples falta de credenciais.

Restante externo: credenciais Google e teste real, SMTP transacional e entrega, certificado de código, domínio/hospedagem/backups, VMs limpas, acessibilidade física/browser/segurança e revisão jurídica. Nenhuma dessas validações foi inventada.

---

# Lacunas do prompt mestre

Este arquivo mantém requisitos não concluídos visíveis. Não equivale a Issues remotas; criar Issues/labels/milestones/Projects somente após conexão GitHub autorizada. Ordem de consolidação: concluir M0/M1 e endurecer M2 antes de liberar intervenções e comunidade pública.

Marco | Próximas entregas verificáveis | Dependência / aceite
---|---|---
M0 | consolidar componentes visuais próprios, alto contraste completo, configurações persistentes, sons originais locais e volume, movimentos reduzidos, renderização SVG/WebP e slots adicionais | matriz DPI/teclado/leitor de tela; assets fornecidos/licenciados, sem geração automática de arte
M1 | telemetria leve, expiração por categoria, indicadores SMART/NVMe e WUA, qualidade/confiança por fonte, análise de recorrência, atualização seletiva e prova antes/depois, exportação redigida detalhada | hardware/VMs controlados; score só com regras auditáveis validadas
M2 | regras versionadas e relatórios por arquivo/categoria, quarentena/restauração elegível, lixeira com consentimento específico, caches revisados e árvore de diretórios; ampliar análise opt-in de maiores arquivos/duplicados já implementada | provar proteção contra hardlinks/ACL/races adicionais; nenhuma exclusão de arquivos pessoais durante testes
M3 | inventário completo com Hardware IDs/data/assinatura, adapters de fonte oficial, verificador real de assinatura/hash, backup, helper tipado IPC/ACL/UAC, instalação/rollback suportados | pacotes oficiais e VMs autorizadas; não instalar drivers no PC do usuário sem aprovação
M3 | PH CORE com base de conhecimento ampliada, medição de gargalos e Modo Jogo antes/depois | perfil atual altera apenas prioridade AboveNormal e restaura no encerramento normal; sem garantia após crash/kill do app
M4 | PostgreSQL real validado, entrega/reenvio email, UI de enrollment e recuperação MFA, gestão de sessões por dispositivo, edição de perfil e triagem de senhas em escala | Docker/Podman/PostgreSQL, TLS, email e lista de hashes configurados; sem credenciais reais no Git
M5 | comentários, tags, votos/reputação, histórico de edição, contestação, revisão contextual completa, notificações persistentes, denúncias de mensagens, anexos sanitizados | storage/quarentena/decoder/antivírus, autorização e testes concorrentes; uploads continuam 503
M6 | fluxo completo de usuários/papéis/suspensão/revisões/estatísticas/KB no painel, reautenticação guiada e auditoria externa resistente a DBA | revisar bootstrap/recuperação em ambiente controlado; proprietário não provisionado nesta sessão
M7 | GitHub privado, conta/organização real, Projects, Issues, labels, branch protection, revisão, Release e Pages | autorização específica e auditoria de segredos/licenças; nenhum remoto criado
M8 | compilar/testar instalador, assinatura, updater que verifica origem/versão/assinatura, VMs limpas, ASVS/LGPD, backups/restauração, performance e acessibilidade | Inno Setup, certificados, infra e revisão; sem custos/serviços externos não autorizados

Governança: definir responsável/controlador, canal de segurança/titulares, política para menores, retenção e jurisdição/transferência, licença do projeto e arte. Nenhuma declaração de conformidade jurídica completa é permitida sem revisão.
