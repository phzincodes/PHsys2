# Checklist de privacidade e governança

Rascunho técnico sujeito a revisão jurídica. Implementar controles não comprova conformidade integral. Referências consultadas em 2026-11-15: [perguntas frequentes da ANPD](https://www.gov.br/anpd/pt-br/acesso-a-informacao/perguntas-frequentes) e [procedimento de comunicação de incidentes](https://www.gov.br/anpd/pt-br/canais_atendimento/agente-de-tratamento/comunicado-de-incidente-de-seguranca-cis).

- [x] Inventário técnico inicial em PRIVACY.md; coleta local e compartilhamento separado.
- [x] Campos públicos vazios por padrão, publicação granular e retirada via API; teste entre usuários.
- [x] Exportação de conta, exclusão/anonimização inicial e revogação de sessão na API.
- [ ] Identificar controlador, responsáveis, operadores e canal de titulares.
- [ ] Definir finalidades e bases legais por tratamento, transparência e termos revisados.
- [ ] Definir elegibilidade/idade e requisitos para menores antes de receber cadastro público.
- [ ] Definir retenção de contas, mensagens, auditoria, backups e evidências de revisão.
- [ ] Completar registro versionado de consentimentos por finalidade, além da auditoria técnica de compartilhamento.
- [ ] Testar atendimento de acesso, correção, portabilidade, oposição e exclusão com prazos e verificação de identidade.
- [ ] Avaliar RIPD, fornecedores e transferências internacionais conforme a infraestrutura escolhida.
- [ ] Exercitar restauração de backups, revogação de acessos e descarte.
- [ ] Aprovar plano de resposta/comunicação a incidentes e manter contatos verificados.

Plano inicial de incidente: conter o acesso, preservar evidências com acesso restrito, identificar dados/titulares/operadores afetados, avaliar impacto com responsável jurídico e de privacidade, decidir comunicações pelo procedimento vigente, corrigir a causa e registrar medidas. Prazos e obrigatoriedade não são codificados aqui; devem ser avaliados contra o regulamento aplicável ao incidente. Não abrir Issues públicas com dados pessoais.
