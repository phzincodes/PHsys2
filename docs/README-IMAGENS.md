# Catálogo visual e sonoro

O catálogo canônico está em [assets/README-IMAGENS.md](../assets/README-IMAGENS.md), com nomes, telas, tamanhos, formatos, obrigatoriedade e fallback. A allowlist técnica fica em [assets-manifest.json](../assets/assets-manifest.json).

- As imagens opcionais não foram fabricadas nem baixadas nesta revisão. A interface usa geometrias vetoriais discretas quando faltam imagens.
- Os dez sons WAV são originais, sintetizados localmente pelo script versionado `tools/create-interface-sounds.py`, sem amostras externas. Começam desligados; falha causa silêncio.
- O catálogo/cache de assets permanece centralizado em `PHsystem.LocalData/CatalogAssetProvider.cs`. A integração automática de todos os slots visuais nas novas telas ainda precisa ser ampliada; adicionar um arquivo ao catálogo não comprova que toda tela já o renderiza.
- Avatares/anexos da comunidade não usam diretórios locais como storage público. O upload permanece indisponível até existir pipeline seguro separado.
- Capturas em `artifacts` são evidências privadas de desenvolvimento e não são distribuídas como imagens do produto.
