# Interface desktop — revisão de 2026-11-15

## Atualização 0.3.0 — prevalece sobre o histórico abaixo

Editor agora inclui superfícies, bordas, textos, destaque secundário, fonte/raio, largura da sidebar e densidade. Biblioteca de até20 temas nomeados, duplicação por nome novo, import/export, remoção e restauração. Quatro widgets ordenáveis/ocultáveis persistem, mantendo saúde/alertas. Contraste e limites continuam validados. Falta sincronização opt-in e controle completo da velocidade de animações. Ação analisar foi movida ao topo após captura real; peer de navegação foi corrigido após falha UIA externa.


## Estrutura de uso

Dez áreas no menu lateral: Visão geral, Diagnóstico, Limpeza, Armazenamento, Drivers, PH CORE, Modo Jogo, Histórico, Comunidade e Privacidade. O menu agrupa computador, ferramentas e área pessoal; pode ser recolhido. Abaixo de 1120 unidades WPF, recolhe automaticamente. O usuário pode expandi-lo novamente.

Abas são contextuais à tarefa: componentes/pontos de atenção no diagnóstico; volumes/análise de pasta no armazenamento; conta, perguntas, mensagens, perfil e resumo na comunidade. Botões de exportação e análise permanecem no cabeçalho. Atalhos: Ctrl+1 visão geral, Ctrl+2 diagnóstico, Ctrl+3 limpeza, F5 análise. Nenhuma navegação executa coleta.

## Hierarquia

- Fundo neutro `#0D1117`, navegação `#10151D`, superfícies `#161D27`, bordas `#2A3442`.
- Texto principal `#E8EDF4`, secundário `#A1ADBD`, destaque de ação/seleção `#63C7B6`.
- Segoe UI do sistema, corpo de 13 unidades, título de página 26, seções 16, legendas 12.
- Espaçamentos de 6, 8, 12, 16, 20, 24 e 30 unidades. Bordas de controles entre 4 e 7 unidades.
- Botão principal para iniciar a tarefa; secundário para utilidades; destrutivo com cor e verbo explícitos. Confirmações de exclusão continuam separadas.
- Dados ausentes usam mensagem de início ou traço; não inventar números, gráficos de desempenho, scores ou estados saudáveis.
- Detalhes técnicos, referências, limites e exclusões ficam em expansores; informações necessárias para consentimento ficam junto à ação.

## Componentes

Theme.xaml centraliza botões, campos, senhas, seleção, abas, tabelas, menus, checkbox, expansores, barras e estados de foco/desabilitado. As telas ficam em `Views/`; MainWindow mantém o shell. ShellViewModel deriva indicadores do snapshot existente, filtra componentes e controla navegação sem coletar dados.

Os ícones da navegação são geometrias de linha definidas no código do projeto, sem emojis, fontes baixadas ou pacote externo de imagens. A marca textual PH é fallback neutro; o catálogo continua aceitando o logo PNG fornecido. Não foram geradas fotos, artes ou banners automaticamente.

Tamanhos são apresentados em B/KiB/MiB/GiB; estados de revisão são traduzidos. Pedidos de atualização de evidências no PH CORE são agrupados, evitando uma sequência de cartões com a mesma ação. O histórico mostra cobertura factual e uma referência curta, sem transformar cobertura em score de saúde.

## Verificação e limites

Compilação e testes WPF verificam navegação, sincronização do menu/área de trabalho, recolhimento, filtragem, resumo derivado de evidências e ausência de nova coleta ao navegar. Capturas reais das dez áreas e de janela 960×720 foram mantidas em artifacts, pois podem conter hardware privado. Não publicar automaticamente.

A revisão visual não substitui teste completo de leitores de tela, alto contraste ou DPI 100–200%/4K. Essas matrizes, configurações persistentes e sons continuam pendentes. A mudança de front não conclui os módulos de backend, drivers ou distribuição.

## Entrega V2: estados e hierarquia

Limpeza usa título/estado único, resumo de escopo, motivos de exclusão e revisão selecionável; área vazia não simula tabela de resultados. Botões destrutivos só aparecem quando há seleção elegível e revisão. Permanência/irreversibilidade ficam junto da confirmação.

PostCard usa uma ação principal Curtir/Curtido com estado teal e foco do tema; contagem positiva é diferente de saldo legado. Durante envio os comandos são desabilitados, sem skeleton do feed inteiro. Solução aceita tem rótulo independente. Conta desconectada é acesso à comunidade, nunca bloqueio dos recursos locais. Configurações mostra proveniência técnica sem expor caminhos/segredos.

Captura real nova em artifacts/v2-final-local.png. Testes lógicos de XAML/WPF não substituem a matriz física de DPI, contraste e leitores de tela ainda pendente. Não há redesenho completo de site/dashboards declarado nesta entrega.

## V3 — Red Eclipse (atual)

Substitui a paleta anterior: fundo #07080B, profundidade #030406, superfícies #111318/#191C23, borda #30343D, acento #FF3347, texto #F3F4F6 e secundário #A6ADBA. Warning/Danger/Success continuam semanticamente distintos. Recursos dinâmicos aplicados globalmente; sem imagens IA.

ThemeManager centraliza seis presets, contraste mínimo 4,5:1 para textos nas superfícies, cor de texto de ação pelo contraste, raio e fonte. Preferências existentes são migradas por campos JSON opcionais. Import limitado a 8 KiB, cores RGB, faixas numéricas e rejeição de campos desconhecidos; nenhum XAML/URL executável. Export duplica o tema via arquivo. Não há biblioteca de múltiplos temas nem editor completo/sincronização/widgets reordenáveis.

ResponsivePanel mede as colunas por largura disponível e altura real. ComboBox preserva templates/selector/format no selecionado e dropdown. Site usa a mesma direção visual com CSS responsivo, foco visível e prefers-reduced-motion. Captura real desktop e testes estão em VALIDATION; browser físico não validado.
