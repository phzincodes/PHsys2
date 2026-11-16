# Avaliação de saúde — health-2026.11.1

Indicador de verificações, não probabilidade científica de ausência de defeitos ou malware. Pesos iniciais: estabilidade 25, segurança 25, armazenamento 15, integridade 15, dispositivos 10, enlace 10. API do motor aceita outros pesos positivos até 50 por dimensão, com as mesmas seis chaves e soma 100; editor visual de pesos ainda não existe.

Uma dimensão precisa de fonte, fatos válidos, confiança da leitura >=0,7 e evidência não expirada. Erros, indisponibilidade, valores inválidos e dados antigos não viram aprovação. Confiança é uma heurística da fonte, não uma porcentagem estatística de segurança. Cobertura ponderada é distinta da contagem de categorias completas.

Dimensão | Regra inicial | Escopo/limite
---|---|---
Estabilidade | 100 menos até 50 por Kernel-Power41 (15 por registro), até35 por WHEA (10), até30 por ApplicationError1000 (5); piso0 | sete dias, System/Application completos dentro do limite; WHEA pode ser corrigido, registros não comprovam causa
Segurança | Defender realtime45, assinaturas até3dias25 (até7dias10), perfis ativos de firewall30 | só quando Defender é o antivírus ativo e ambos os provedores são legíveis; produto alternativo deixa desconhecido. Perfis inativos não penalizam
Armazenamento | menor razão livre: >=20%100, >=10%70, >=3%30, abaixo0 | capacidade somente, não SMART/defeito físico
Dispositivos | sem erro PnP100; menos25 por dispositivo com erro até0 | consulta limitada não pode produzir aprovação; não testa CPU/RAM fisicamente
Enlace | alguma interface ativa100, nenhuma0 | não comprova internet, DNS ou throughput
Integridade | não verificada | DISM/SFC ainda não integrados; inventário do Windows não substitui verificação

Pontos conhecidos = peso × percentual/100. Limite inferior soma somente os pontos conhecidos; limite superior soma os conhecidos + todos os pesos não verificados. Não se normalizam os conhecidos para fabricar nota100. A UI mostra faixa quando incompleta. Nota única e faixa Excelente/Bom/Atenção/Crítico só são permitidas com cobertura100%; hoje a integridade pendente impede essa nota completa.

Histórico recalcula com a versão atual da regra no horário da coleta e informa que não é a nota originalmente armazenada. Mudança de cobertura não comprova reparo. A avaliação permanece privada. Não há ranking, selo remoto ou publicação automática.

Fontes nativas: [Microsoft Defender WMI](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/defender/msft-mpcomputerstatus), INetFwPolicy2, Event Log, PnP/WMI e DriveInfo. Nenhum comando PowerShell é usado como motor. Validação em HEALTH/Crash tests e docs/VALIDATION.md.
