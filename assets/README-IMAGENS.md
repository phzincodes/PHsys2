# Catálogo de imagens e sons

Nenhuma arte, foto, banner ou rosto gerado automaticamente. Arquivos de arte continuam ausentes; adicionar somente materiais próprios/licenciados e registrar proveniência. O único consumo visual atual é branding/app-icon.png, com texto PHsystem Next como fallback. Os demais são slots previstos, não telas concluídas.

CatalogAssetProvider usa PHSYSTEM_ASSETS_DIRECTORY quando configurado e assets junto ao executável como fallback instalado. Caminhos relativos cadastrados, até 4 MiB por arquivo, cache até 16 MiB e atualização por tamanho/data de modificação. SVG restrito sem scripts/referências externas; PNG/WebP/WAV têm verificação estrutural básica. Decoder WPF protege o logo PNG com fallback. Decodificação completa dos demais formatos/renderização SVG/WebP ainda pendente. Ver docs/SECURITY.md.

Arquivo | Componente/tela | Função | Dimensão / proporção | Formato | Obrigatório/opcional | Ausência
---|---|---|---|---|---|---
branding/logo-horizontal.svg | branding | identidade horizontal | 640×160 / 4:1 | svg | opcional | texto ou geometria neutra
branding/logo-symbol.svg | branding | símbolo da marca | 256×256 / 1:1 | svg | opcional | texto ou geometria neutra
branding/app-icon.png | branding | logo PNG da janela atual | 256×256 / 1:1 | png | opcional | texto ou geometria neutra
backgrounds/login-cover.webp | backgrounds | fundo/capa previsto | 1920×1080 / 16:9 | webp | opcional | texto ou geometria neutra
backgrounds/dashboard-decoration.webp | backgrounds | fundo/capa previsto | 1920×1080 / 16:9 | webp | opcional | texto ou geometria neutra
backgrounds/community-header.webp | backgrounds | capa da comunidade prevista | 1920×480 / 4:1 | webp | opcional | texto ou geometria neutra
illustrations/scan-empty.webp | illustrations | slot visual previsto | 512×512 / 1:1 | webp | opcional | texto ou geometria neutra
illustrations/scan-complete.webp | illustrations | slot visual previsto | 512×512 / 1:1 | webp | opcional | texto ou geometria neutra
illustrations/cleanup-empty.webp | illustrations | slot visual previsto | 512×512 / 1:1 | webp | opcional | texto ou geometria neutra
illustrations/driver-warning.webp | illustrations | slot visual previsto | 512×512 / 1:1 | webp | opcional | texto ou geometria neutra
illustrations/optimizer-guide.webp | illustrations | slot visual previsto | 512×512 / 1:1 | webp | opcional | texto ou geometria neutra
illustrations/offline.webp | illustrations | slot visual previsto | 512×512 / 1:1 | webp | opcional | texto ou geometria neutra
icons/cpu.svg | icons | ícone ou selo vetorial previsto | 24×24 viewBox / 1:1 | svg | opcional | texto ou geometria neutra
icons/gpu.svg | icons | ícone ou selo vetorial previsto | 24×24 viewBox / 1:1 | svg | opcional | texto ou geometria neutra
icons/ram.svg | icons | ícone ou selo vetorial previsto | 24×24 viewBox / 1:1 | svg | opcional | texto ou geometria neutra
icons/disk.svg | icons | ícone ou selo vetorial previsto | 24×24 viewBox / 1:1 | svg | opcional | texto ou geometria neutra
icons/network.svg | icons | ícone ou selo vetorial previsto | 24×24 viewBox / 1:1 | svg | opcional | texto ou geometria neutra
icons/shield.svg | icons | ícone ou selo vetorial previsto | 24×24 viewBox / 1:1 | svg | opcional | texto ou geometria neutra
icons/driver.svg | icons | ícone ou selo vetorial previsto | 24×24 viewBox / 1:1 | svg | opcional | texto ou geometria neutra
icons/repair.svg | icons | ícone ou selo vetorial previsto | 24×24 viewBox / 1:1 | svg | opcional | texto ou geometria neutra
community/categories/hardware.webp | community | capa de categoria prevista | 320×180 / 16:9 | webp | opcional | texto ou geometria neutra
community/categories/windows.webp | community | capa de categoria prevista | 320×180 / 16:9 | webp | opcional | texto ou geometria neutra
community/categories/games.webp | community | capa de categoria prevista | 320×180 / 16:9 | webp | opcional | texto ou geometria neutra
community/categories/drivers.webp | community | capa de categoria prevista | 320×180 / 16:9 | webp | opcional | texto ou geometria neutra
community/categories/network.webp | community | capa de categoria prevista | 320×180 / 16:9 | webp | opcional | texto ou geometria neutra
avatars/default-avatar.png | avatars | avatar neutro previsto | 256×256 / 1:1 | png | opcional | texto ou geometria neutra
badges/verified-expert.svg | badges | ícone ou selo vetorial previsto | 24×24 viewBox / 1:1 | svg | opcional | texto ou geometria neutra
badges/moderator.svg | badges | ícone ou selo vetorial previsto | 24×24 viewBox / 1:1 | svg | opcional | texto ou geometria neutra
badges/administrator.svg | badges | ícone ou selo vetorial previsto | 24×24 viewBox / 1:1 | svg | opcional | texto ou geometria neutra
sounds/click.wav | sounds | som local original previsto; player/volume pendentes | 48 kHz mono PCM / duração curta | wav | opcional | silêncio
sounds/success.wav | sounds | som local original previsto; player/volume pendentes | 48 kHz mono PCM / duração curta | wav | opcional | silêncio
sounds/warning.wav | sounds | som local original previsto; player/volume pendentes | 48 kHz mono PCM / duração curta | wav | opcional | silêncio

Capturas reais ficam em artifacts ignorado e não devem ser publicadas antes de revisar dados privados. Uploads do servidor possuem política separada e estão indisponíveis; não reutilizar este leitor de arquivos para anexos públicos. Sons ausentes significam silêncio; não há download automático.

## Sons originais locais
Os WAV mono PCM 16-bit/44,1 kHz foram sintetizados pelo script `tools/create-interface-sounds.py`, sem amostras externas. Durações entre 45 e 190 ms; revisão auditiva humana ainda pendente. São opcionais; ausência/falha causa silêncio. Iniciam desligados.

arquivo | componente/tela | função | dimensão/duração | formato | obrigatório/opcional | ausente
---|---|---|---|---|---|---
sounds/click.wav | botões | interação | 45 ms | WAV PCM | opcional | silêncio
sounds/navigation.wav | navegação | seleção | 65 ms | WAV PCM | opcional | silêncio
sounds/panel.wav | painéis | abertura (cue disponível) | 100 ms | WAV PCM | opcional | silêncio
sounds/scan-start.wav | análise | início | 140 ms | WAV PCM | opcional | silêncio
sounds/success.wav | análise | conclusão | 190 ms | WAV PCM | opcional | silêncio
sounds/confirm.wav | autenticação | sessão confirmada | 100 ms | WAV PCM | opcional | silêncio
sounds/error.wav | análise | erro | 140 ms | WAV PCM | opcional | silêncio
sounds/notification.wav | notificações | cue disponível; integração pendente | 150 ms | WAV PCM | opcional | silêncio
sounds/reply.wav | comunidade | cue disponível; integração pendente | 170 ms | WAV PCM | opcional | silêncio
sounds/warning.wav | avisos | cue disponível | 130 ms | WAV PCM | opcional | silêncio
