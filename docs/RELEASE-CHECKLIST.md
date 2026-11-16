# Checklist de release

Nenhuma release de produção está autorizada ou declarada pronta.

- [x] Restore/build/test/publish locais registrados em VALIDATION.
- [x] Diagnóstico sem conta e isolamento dos dados locais testados.
- [x] Curtir/retirar com Identity/TLS e concorrência PostgreSQL exclusivo testados.
- [x] Imagem Linux não root compilada e diretório de chaves novo gravável verificado.
- [ ] Reconciliar implementação local e branches remotas divergentes com revisão.
- [ ] Validar bootstrap integral em ambiente limpo preservando volumes existentes.
- [ ] Hospedar API, configurar domínio/TLS, PostgreSQL, SMTP e backups com autorização.
- [ ] Confirmar entrega real de e-mail, revogação, restore de backup e monitoramento.
- [ ] Implementar/validar web autenticada e Google antes de anunciar esses recursos.
- [ ] Completar testes físicos DPI/acessibilidade e instalação Windows limpa.
- [x] Gerar instalador de prévia e validar download/checksum local.
- [ ] Validar instalação/atualização/desinstalação e preservação de dados em VM.
- [ ] Auditar segredos/licenças/PII, assinar artefatos e publicar hash/tamanho/notas reais.
- [ ] Obter autorização de publicação/deploy e executar CI/release remota.

Checks locais não equivalem a aprovação remota, revisão jurídica LGPD ou certificação de segurança.
