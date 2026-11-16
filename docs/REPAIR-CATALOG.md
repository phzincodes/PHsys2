# Catálogo de reparos — limite atual

## Atualização 0.3.0 — prevalece sobre o histórico abaixo

Achados adicionais contextualizam reinicialização inesperada, Defender desativado quando ativo e firewall ativo desabilitado. Ferramentas oficiais continuam assistidas, não reparos automáticos. Nenhuma abertura de ferramenta é marcada como resolvida. A comparação histórica pode mostrar faixas/coberturas recalculadas, sem atribuir mudança a intervenção. DISM/SFC/helper/driver privilegiado, rollback e histórico completo de reparos continuam não implementados.


Ainda não existe uma central completa de intervenções verificadas. Os achados do diagnóstico e regras do PH CORE fornecem evidências/orientações; isso não torna um botão de reparo privilegiado operacional. `PrivilegedHelper` continua recusando operações não implementadas.

Categoria | Política | Estado atual
---|---|---
A — automática autorizada | operação local específica, prévia, consentimento e verificação | somente exclusão restrita de temporários tem fluxo implementado; não é reparo universal
B — assistida | ferramenta oficial com destino/argumentos fixos | catálogo integrado ainda pendente
C — manual | orientação contextual com fonte, risco e verificação | regras do Optimizer/PH CORE existentes; ampliar cobertura e revisão
D — indisponível | explicar impossibilidade e alternativa | instalação de drivers e reparos privilegiados não devem simular sucesso

DISM/SFC, rede, inicialização e correlação de estabilidade precisam de entregas próprias com testes isolados, IPC/ACL/UAC quando necessário, timeout e evidência antes/depois. Nenhum comando remoto ou de comunidade pode entrar no catálogo executável. Nunca desativar segurança, limpar Registro genericamente ou apagar eventos para declarar resolução.

## V3 — primeiras ações assistidas

Diagnóstico → Ferramentas oficiais oferece Windows Security (windowsdefender:), Windows Update (ms-settings:windowsupdate) e Inicialização (ms-settings:startupapps). Lista fechada no código, sem argumentos/comandos de usuários. São categoria B: o usuário opera a ferramenta oficial; abrir não é corrigir. Teste rejeita cmd.exe e URL arbitrária. DISM/SFC, ações elevadas e histórico antes/depois ainda não estão implementados.
