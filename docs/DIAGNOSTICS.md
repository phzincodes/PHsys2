# Diagnóstico e manutenção local

## Atualização 0.3.0 — prevalece sobre o histórico abaixo

18 coletores. Proteção Windows lê MSFT_MpComputerStatus sem ComputerID ou recursos de ameaça; Firewall lê INetFwPolicy2 e perfis ativos, sem alterar políticas. Estabilidade lê até500 registros System/Application por janela7dias, sem mensagens ou dumps, e preserva indicação de limite. Crash Investigator mostra até40 itens e contexto temporal sem apontar causa. HEALTH-SCORE documenta os limites ponderados; integridade profunda segue pendente. Relatório HTML reduzido não inclui identificadores/logs brutos.


`ScanCoordinator` coordena coletores com cancelamento, timeout e paralelismo limitado; `SnapshotStore` reutiliza o snapshot nas telas. `LocalData` registra SQLite e histórico. Navegação não é autorização para iniciar outra coleta. O teste offline de DesktopTests comprova ausência de chamada online e de varredura duplicada.

Cobertura e achados vêm de evidências. Ausência de sensor ou falha de coleta não significa computador saudável; score não validado permanece indisponível. Telemetria leve, WUA/SMART completos e atualização parcial por categoria ainda são lacunas.

## Escopo da limpeza

`TempCleanupService` examina o primeiro nível da pasta temporária permitida, com limite de entradas e idade superior a sete dias. Não percorre subpastas nem amplia o escopo para fabricar resultados. Downloads, documentos, perfis e caches não classificados ficam fora.

`CleanupAnalysis` distingue arquivos considerados/elegíveis/ignorados, diretórios, entradas sem identificação e motivos de exclusão. Limites/erros produzem resultado parcial. Zero elegíveis é um resultado válido, diferente de ainda não analisado.

Prévia é de uso único e expira em dez minutos. A seleção deve ser subconjunto do plano. No Windows, handles fixam o diretório e a identidade do arquivo é verificada entre prévia e exclusão; reparse points, hardlinks, alteração e arquivo em uso são recusados. Consentimento continua necessário.

O resultado informa cada tentativa e distingue tamanho nominal removido da variação observada no espaço livre. Atividade concorrente do Windows impede atribuir toda variação à limpeza. Não há quarentena nesta implementação local: exclusão é permanente e explicitamente confirmada. Quarentena, lixeira e caches adicionais permanecem pendentes de políticas e testes.

## V3 — utilidade adicional

NetworkLabViewModel, aba Diagnóstico → Rede/testes: hostname/IP validado, DNS e cinco pings explícitos, prazo 20s, cancelamento, perdas e média/min–máx. Não mede throughput nem conclui defeito permanente. ICMP bloqueado é explicado. Não armazena/envia diagnóstico à comunidade; DNS/ICMP atingem o destino escolhido. Testes atuais validam entrada, não todas as redes reais.

Ferramentas oficiais: Windows Security, Update e Startup por URI fixa allowlist. A ação só abre a ferramenta; não declara scan/reparo/resolução. Malware/SMART/score profundo e correlação de estabilidade continuam pendentes. Snapshot compartilhado e suas limitações são mantidos.
