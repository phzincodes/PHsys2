# E-mail — estado real

## Atualização 0.3.0 — prevalece sobre o histórico abaixo

Google OAuth de login não envia email por Gmail. SMTP configurável permanece o provedor de mensagens; Mailpit só Development. Google verificado dispensa confirmação adicional; contas convencionais usam os links existentes. Entrega externa, OAuth Gmail API, bounce e revisão dos templates de todos os avisos de segurança continuam pendentes.


IAccountEmail permanece substituível; SmtpAccountEmail aceita SMTP transacional configurável. Mailpit é exclusivo do Compose Development. Destinatários podem ser Gmail ou outro provedor; o login Google não fornece permissão de envio Gmail.

Configuração privada: Email:Host, Port, From, Username/Password quando exigidos, EnableSsl e PublicBaseUrl. Em produção Configured exige origem pública HTTPS sem localhost e SMTP TLS. Segredos nunca entram no desktop/repositório. OAuth Gmail API ainda não está implementado e credenciais/entrega externa permanecem pendentes.

AccountEmailContent gera links confirmar/redefinir a partir da origem configurada. ID/token ficam no fragmento, não na URL enviada ao servidor, evitando registro em access logs. O navegador envia o token pelo POST com CSRF. Identity mantém prazo de 30 minutos; confirmação já usada é recusada e reset usa security stamp. A apresentação ID/token legada permanece no corpo para compatibilidade desktop; eliminação desse fluxo é pendente.

Mensagem possui versões texto e HTML Red Eclipse com conteúdo codificado. Nenhum status afirma entrega: sucesso de SMTP significa aceitação pelo servidor, não chegada na caixa do destinatário. Monitoramento de bounce, templates separados de avisos de segurança/alteração de senha, cooldown específico de reenvio e provedor OAuth continuam pendentes. Rate limiter global e respostas genéricas existentes reduzem abuso/enumeração, sem equivaler a uma plataforma transacional completa.

Em desenvolvimento PublicBaseUrl usa https://localhost:5178/. Produção precisa domínio público próprio. Não foram enviados e-mails externos nesta entrega.
