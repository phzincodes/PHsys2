# Contrato de diagnóstico

`DiagnosticSnapshot` é um registro imutável com ScanId, CapturedAt UTC, CollectorVersion e categorias imutáveis. Cada categoria contém estado, origem, confiança, erros, expiração e fatos tipados como pares de texto. Confiança descreve a leitura, não uma probabilidade estatística de saúde. `HealthScore` é nulo até existir modelo de avaliação validado; cobertura conta somente categorias Complete.

`ScanCoordinator` admite uma varredura por instância, até 32 coletores e três coletas simultâneas. Timeout padrão: 20 segundos por coletor. Três falhas suspendem a categoria por dois minutos. Dados inválidos viram erro de categoria; mensagens brutas de exceções não são persistidas. Cancelamento não publica snapshot incompleto. Persistência acontece antes de atualizar o store compartilhado.

Reuso considera categorias completas não expiradas. O botão principal força atualização. As leituras atuais são parciais por escopo: todas as telas explicam esse limite; categoria parcial não vira saúde completa. Limites por categoria: 256 fatos, chaves de até 512 caracteres, valores até 2048 e 32 erros. Os adaptadores WMI limitam a 50 registros, sem mensagens brutas de eventos.

Categorias atuais: Sistema, Processador, Memória, Armazenamento, Rede, Drivers, GPU, Controladores, Dispositivos com erro, Inicialização, Serviços, Atualizações instaladas, Segurança, Saúde das unidades e Eventos críticos. Segurança lê produtos registrados, não comprova proteção efetiva. Saúde das unidades usa status genérico WMI, sem promessa SMART/NVMe. Serviços automáticos parados podem estar assim por projeto. Atualizações instaladas não revelam pendências.

WPF, drivers, recomendações e resumo local usam o mesmo SnapshotStore. Nenhuma navegação executa coleta. A prévia de limpeza é uma análise opt-in separada e limitada, pois o snapshot não contém listas de arquivos privados.

Pendências: telemetria leve em tempo real, expiração distinta por fonte, refresh seletivo após intervenções, relação antes/depois persistida, SMART/NVMe, Windows Update Agent, recorrência e causalidade de eventos. Timeouts cancelam a espera e solicitam cancelamento; não encerram à força chamadas nativas já em execução. Coletores nativos também usam limites internos, mas precisam de medições adicionais em hardware variado.
