# Roadmap verificável da reformulação

Esta revisão é incremental e não é uma release de produção. A auditoria anterior às alterações está em [AUDITORIA-UX-ATUAL](AUDITORIA-UX-ATUAL.md). O histórico de validação está em [STATUS](STATUS.md).

Marco | Entrega atual | Próximo trabalho / dependência
---|---|---
0 — Auditoria | inventário real, baseline, branch e commits locais | manter a matriz a cada alteração
1 — HUD | IconControl, trilho fixo, cores, rolagem e navegação tipada | revisar todos os templates, alto contraste e leitor de tela
2 — Autenticação | boas-vindas, validação real, cadastro/termos, e-mail/reset, sessão DPAPI opt-in, expiração/renovação | ambiente HTTPS/PostgreSQL/SMTP, links de e-mail automáticos, UX de cadastro MFA/recuperação
3 — Início | ação única, estados e progresso real, cancelamento, snapshot persistido, resultados | testar interação autenticada de ponta a ponta no ambiente implantado
4 — Resultados | abas por finalidade e comparação imutável | sensores/placa-mãe, telemetria leve, gráficos e timeline visual
5 — Movimento/som | microinterações, ReduceMotion, preferências, sons locais e silêncio | revisão auditiva e integração dos cues de notificação/painel
6 — Comunidade | feed paginado, busca/filtros, votos, salvos, categorias seguidas, posts próprios, threads, perfis, notificações | avatares/anexos seguros, editar respostas, seguir posts individuais, menções e push/reconexão SignalR
7 — Segurança | Identity/MFA/CSRF/roles preservados, BOLA, privacidade por campo, limpeza de sessão e exportação/exclusão ampliadas | painel ampliado de moderação, reputação auditável, auditoria ASVS e revisão jurídica/LGPD
8 — Validação | restore/build/test/publish locais, testes XAML/segurança, matriz lógica | DPI físico, acessibilidade, máquinas limpas, instalador compilado/assinatura, CI remota

## Condições externas

- A entrada atual exige autenticação, conforme o pedido mais recente. Sem API HTTPS, PostgreSQL, triagem de senhas e e-mail, não há cadastro/login utilizável. O motor continua local e os diagnósticos não são enviados para autenticar.
- Docker/Podman, e-mail real, certificado, storage e assinatura não foram provisionados nesta sessão. Consulte [DEPLOYMENT](DEPLOYMENT.md).
- Upload continua respondendo indisponível até validação de conteúdo, reprocessamento, remoção de metadados e quarentena estarem configurados. Não aceitar executáveis ou scripts como alternativa.
- Não existe autorização nesta revisão para push, publicação pública ou deploy; nenhuma dessas ações foi realizada.

## Critério para avançar

Cada recurso deve ter estado real, autorização no servidor quando pertinente e testes de regressão. Não transformar votos em selo técnico, ausência de coleta em saúde perfeita, ou conexão ausente em sucesso. Dados de teste e capturas locais não devem virar conteúdo público do produto.
