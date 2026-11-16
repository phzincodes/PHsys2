# Drivers e intervenções

O inventário atual é somente leitura, de adaptadores de vídeo/rede registrados no Windows. É parcial: não demonstra presença, atualização, compatibilidade ou assinatura. A tela não oferece instalação fictícia.

`DriverPackagePolicy` é uma regra de domínio testada com simuladores: exige HTTPS e host oficial exato, hardware/SO/arquitetura compatíveis, evidência de assinatura/editor, download completo e SHA-256 correspondente. Esses dados são entradas de teste; a política isolada não verifica Authenticode nem comprova uma fonte. Nenhum instalador pode tratar CanProceed isoladamente como autorização.

Ainda não implementado: busca oficial WUA/fabricantes, download protegido, verificação real WinTrust/Authenticode, comparação completa Hardware IDs, backup/exportação, UAC, IPC allowlist, PnPUtil/SetupAPI e verificação pós-instalação. O helper atual continua recusando qualquer ação. Cancelamento de UAC e instalações reais não foram testados.

Referências oficiais para a implementação seguinte: [Windows Update Agent](https://learn.microsoft.com/en-us/windows/win32/api/_wua/), [PnPUtil](https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/pnputil-command-syntax), [Win32_PnPEntity e códigos de dispositivo](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pnpentity).

Não baixar de fóruns/mirrors, instalar BIOS/firmware, desativar segurança ou presumir rollback universal. Toda instalação futura exige prévia, fonte, compatibilidade, consentimento, privilégio somente na etapa necessária e resultado observável. Testes destrutivos ficam restritos a máquinas controladas autorizadas.
