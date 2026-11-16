# Validação da reformulação desktop — 2026-11-15

Escopo: front WPF. As dez áreas existentes foram reorganizadas em um shell com navegação lateral, abas de contexto e controles comuns; dados, exclusões, autenticação e intervenções continuam sujeitos aos contratos e limites anteriores. Não houve instalação de driver, exclusão de dados reais ou publicação externa durante a revisão.

## Verificações executadas

- Restore de PHsystem.slnx aprovado.
- Build completo aprovado com zero avisos e zero erros.
- Suíte completa de 19:57 local: **81 aprovados, zero falhas, um ignorado**. UnitTests 51; SecurityTests 22; IntegrationTests 3 + 1 PostgreSQL ignorado; UiTests 3; WindowsTests 1; E2ETests 1.
- Testes WPF novamente aprovados às 20:04 após os ajustes finais de navegação e tabela de detalhes: 3 aprovados. Cobrem sincronização menu/página, dez destinos, recolhimento, filtro de categorias, resumo derivado do mesmo snapshot, rota para compartilhamento e nenhuma coleta por navegação.
- Publish self-contained win-x64 e tools/PackageDesktop.ps1 aprovados. ZIP/checksum atualizados em artifacts. SHA-256 do ZIP desta revisão: `0cd2b557cd606c4be1476b5d0e1328cd4894588370aad440f51d6e8bba44dcfb`.
- As dez áreas foram percorridas via UI Automation em uma janela real. Capturas de visão geral, diagnóstico, limpeza, armazenamento, drivers, PH CORE, modo jogo, histórico, comunidade e privacidade foram inspecionadas. Janela compacta 960×720 também foi capturada. As imagens permanecem privadas em artifacts.

## Problemas encontrados e corrigidos

- ProgressBar.Value tentava atualizar uma propriedade calculada somente leitura e encerrava a janela. Binding passou a OneWay; o teste WPF agora processa o dispatcher para ativar bindings reais.
- O menu agrupado não tinha sua fonte inicializada no teste/antes de resolver o contexto. Fonte e seleção inicial passaram a ser configuradas explicitamente pelo shell.
- Dois painéis da visão geral ocupavam a mesma coluna; grid corrigido e captura repetida.
- Margem externa em TabItem era descontada pelo TabPanel e cortava rótulos. Espaçamento foi movido para padding interno.
- Confirmações e textos vazios estavam centralizados/cortados; alinhamento e quebra de texto corrigidos.
- Detalhes de componentes passaram a usar tabela Informação/Valor com linhas de altura automática; a captura final confirmou o alinhamento. O menu compacto recebeu padding próprio para preservar os ícones.
- Solicitações repetidas de atualização no PH CORE foram agrupadas sem alterar as regras de diagnóstico.

## Limites

Percorrer telas reais e testar 960×720 não substitui matriz de DPI, 4K, alto contraste, leitor de tela ou E2E completo de cada backend. Não foram usados serviços remotos ou credenciais reais para testar formulários. Inputs de senha são limpos ao sair da área de comunidade. Nenhum screenshot com hardware privado foi publicado ou incluído no site.

O executável publicado final foi aberto, permaneceu respondendo e teve a tela de diagnóstico capturada novamente. A janela foi deixada em Visão geral para revisão do usuário.

O teste PostgreSQL continua ignorado porque PHSYSTEM_TEST_POSTGRES não está configurado; isso não representa validação do banco servidor. Requisitos pendentes do produto permanecem em BACKLOG.md.
