# Modelo de ameaças e controles

## Atualização 0.3.0 — prevalece sobre o histórico abaixo

Google usa endpoints oficiais fixos, middleware OAuth/state/correlação e PKCE; sub/verified-email são derivados do provedor, nunca do formulário. Email igual não vincula contas. Equipe/MFA são recusados neste fluxo até haver desafio apropriado. Broker de cinco minutos é limitado128, exige hash do poll token e verifier, consumo atômico; desligado por padrão. Tokens Google não são salvos e logs Hosting.Diagnostics não registram request URLs do callback. Os testes de provedor são mocks; não equivalem a auditoria ou validação de produção. Instalador é prévia sem assinatura, não executado pelo Codex.


Estado: desenvolvimento. Sem auditoria independente ou certificação ASVS. Referência: [OWASP ASVS 5.0.0](https://asvs.owasp.org/). O checklist completo de requisitos ainda precisa ser mapeado e executado.

## Fronteiras

- Desktop sem elevação, dados locais; nenhuma API de comunidade tem autoridade sobre o computador.
- Snapshot privado separado de PublicHardwareField. O servidor não possui endpoint para receber o snapshot completo.
- Conteúdo de fórum é não confiável, exibido como texto. Não existe botão ou endpoint que execute posts no computador.
- Helper indisponível por padrão; futura interface deve aceitar operações tipadas com ACL e validação, nunca uma linha de comando.
- Arquivos, assets e anexos possuem políticas distintas. Uploads retornam 503 até existir storage isolado, reprocessamento e quarentena.

## Controles implementados

Identity armazena hashes PBKDF2 com salt e 600.000 iterações configuradas. Senhas exigem 8–128 caracteres com maiúscula, minúscula, número e símbolo; CLI administrativa exige 16+. SHA-256 aparece somente no índice offline de senhas comprometidas e na política de pacote, nunca como substituto do hash de senha. A lista revisada de senhas comprometidas é pré-requisito configurável; sem arquivo, o validador recusa cadastro/reset. Sua qualidade, atualização e desempenho ainda precisam de processo operacional. Consulte a [referência de armazenamento OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html).

Cookies HttpOnly/Secure/SameSite Strict, validação de security stamp em cada requisição, expiração de sessão de quatro horas, logout e revogação global. CSRF é verificado após autenticação em operações mutáveis, inclusive login. API exige HTTPS para mutações, limita corpo a 64 KiB, taxa global e taxa de identidade por IP/rota. Respostas de cadastro/recuperação são genéricas; testes não substituem análise de canais laterais/timing.

MFA TOTP obrigatório para equipe; uso de código é registrado por digest para rejeitar reuso por 15 minutos. Políticas Admin/Staff/Review verificam papéis e claim MFA no servidor. Admin exige autenticação com até dez minutos. A API pública não permite atribuição de papéis. Auditoria aceita apenas inclusão no DbContext; isso não impede alteração por DBA ou SQL externo. Produção exige usuário de banco restrito, trilha externa, alertas e backups protegidos.

Limpeza valida arquivos antigos da raiz confiável de temporários, recusa links e abre handles com revalidação e exclusão por handle. Diretórios ancestrais ficam fixados contra renomeação. Não há exclusão recursiva. Testes usam pastas isoladas, arquivos alterados/em uso e junction; não foi feita limpeza real do ambiente do usuário.

## Antes de produção

Revisar ASVS, dependências, licenças e segredos; testar PostgreSQL real, TLS, email, concorrência e rate limits distribuídos; proteger chaves Data Protection; restringir proxy e registrar acessos sem PII; implementar retenção de mensagens/anexos/auditoria, recuperação MFA, elegibilidade de menores, backups e resposta a incidentes. O cliente desktop não persiste cookies em disco. Exceções e logs de infraestrutura precisam de revisão adicional contra PII.

## V3 — controles adicionados e limites

Temas são dados JSON limitados, nunca XAML executável/URLs de recursos. Contraste e faixas validados, campos desconhecidos recusados. Web trata texto de usuário via textContent, mantém CSP self, cookies HttpOnly/Secure e CSRF servidor. Links de confirmação usam fragmento para não gravar token no request URL. Produção SMTP exige TLS/origem pública, não Mailpit/localHost. Não houve OAuth Google implementado, envio externo, auditoria ASVS completa ou teste browser. Ferramentas assistidas usam allowlist fixa; nenhum comando de posts é executado.
