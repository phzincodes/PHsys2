# Comunidade e revisão humana

API v1 implementa categorias, criação/listagem/pesquisa de perguntas, respostas, marcação de solução pelo autor, denúncia, bloqueio e mensagens com opt-in. Todas as leituras de mensagens filtram o usuário autenticado. Conteúdo é texto; não se executa HTML, scripts ou comandos da comunidade.

Respostas começam Unverified. O detector conservador sinaliza links, scripts, comandos e pedidos potencialmente perigosos; itens suspeitos ficam ocultos/pendentes. Esse filtro não detecta todos os riscos e não valida instruções. Uma resposta não sinalizada continua não verificada.

TrustedReviewer, com MFA, pode aprovar/revogar revisão técnica de resposta de outro autor. Registro inclui evidência, URL HTTPS, escopo, revisor, validade de 90 dias e auditoria. HTTPS sozinho não comprova que a fonte é oficial; o revisor humano deve verificar domínio, compatibilidade, riscos e alternativas. A API não atribui o selo Testada em ambiente controlado porque esse fluxo ainda não existe.

Moderator/Admin com MFA podem ler denúncias e ocultar posts. Admin tem auditoria separada e requer autenticação recente. Papéis não são alteráveis por cadastro/perfil público. A ferramenta local de proprietário não promove uma conta pré-existente por nome.

Pendências: histórico de edições, comentários, tags completas, votos/reputação, contestação, fila visual completa, revisão por versão/hardware, categorias administrativas, anexos reprocessados, denúncias de mensagens, retenção e controles de flood sob concorrência. Upload retorna 503; não aceita scripts como reparo verificado. Mensagens são armazenadas no servidor e não têm E2EE.
