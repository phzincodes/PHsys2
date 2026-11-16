using System.Collections.Immutable;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using PHsystem.Core;

namespace PHsystem.Windows;

public static class WindowsCollectors
{
    public static IEnumerable<IDiagnosticCollector> Create() =>
    [
        new LocalCollector("Sistema", CollectSystem),
        new LocalCollector("Processador", CollectCpu),
        new LocalCollector("Memória", CollectMemory),
        new LocalCollector("Armazenamento", CollectStorage),
        new LocalCollector("Rede", CollectNetwork),
        new LocalCollector("Drivers", CollectDrivers)
        ,new LocalCollector("GPU", token => WmiEvidence.Read("root\\cimv2", "SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController", ["Name", "DriverVersion", "AdapterRAM"], token))
        ,new LocalCollector("Controladores", token => WmiEvidence.Read("root\\cimv2", "SELECT Name, Status FROM Win32_SCSIController", ["Name", "Status"], token))
        ,new LocalCollector("Dispositivos com erro", token => WmiEvidence.Read("root\\cimv2", "SELECT Name, ConfigManagerErrorCode FROM Win32_PnPEntity WHERE ConfigManagerErrorCode <> 0", ["Name", "ConfigManagerErrorCode"], token))
        ,new LocalCollector("Inicialização", token => WmiEvidence.Read("root\\cimv2", "SELECT Name, Location FROM Win32_StartupCommand", ["Name", "Location"], token))
        ,new LocalCollector("Serviços", token => WmiEvidence.Read("root\\cimv2", "SELECT Name, State, StartMode FROM Win32_Service WHERE StartMode = 'Auto' AND State <> 'Running'", ["Name", "State", "StartMode"], token))
        ,new LocalCollector("Atualizações instaladas", token => WmiEvidence.Read("root\\cimv2", "SELECT HotFixID, InstalledOn FROM Win32_QuickFixEngineering", ["HotFixID", "InstalledOn"], token))
        ,new LocalCollector("Segurança", token => WmiEvidence.Read("root\\SecurityCenter2", "SELECT displayName, productState FROM AntiVirusProduct", ["displayName", "productState"], token))
        ,new LocalCollector("Saúde das unidades", token => WmiEvidence.Read("root\\cimv2", "SELECT Model, Status FROM Win32_DiskDrive", ["Model", "Status"], token))
        ,new LocalCollector("Eventos críticos", WmiEvidence.ReadEvents)
        ,new LocalCollector("Proteção Windows", SecurityEvidence.Defender, "Microsoft Defender / MSFT_MpComputerStatus")
        ,new LocalCollector("Firewall", SecurityEvidence.Firewall, "Windows Firewall / INetFwPolicy2")
        ,new LocalCollector("Estabilidade", StabilityEvidence.Read, "Windows Event Log / System + Application")
    ];

    private static IReadOnlyDictionary<string, string> CollectSystem(CancellationToken token) => new Dictionary<string, string>
    {
        ["Sistema"] = RuntimeInformation.OSDescription,
        ["Arquitetura"] = RuntimeInformation.OSArchitecture.ToString(),
        ["Runtime"] = RuntimeInformation.FrameworkDescription,
        ["Tempo ativo (horas)"] = (Environment.TickCount64 / 3600000d).ToString("F1", CultureInfo.InvariantCulture)
    };

    private static IReadOnlyDictionary<string, string> CollectCpu(CancellationToken token)
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        return new Dictionary<string, string>
        {
            ["Modelo"] = (key?.GetValue("ProcessorNameString") as string)?.Trim() ?? "Indisponível",
            ["Processadores lógicos"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static IReadOnlyDictionary<string, string> CollectMemory(CancellationToken token)
    {
        var status = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        if (!GlobalMemoryStatusEx(ref status)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        return new Dictionary<string, string>
        {
            ["TotalBytes"] = status.TotalPhysical.ToString(CultureInfo.InvariantCulture),
            ["AvailableBytes"] = status.AvailablePhysical.ToString(CultureInfo.InvariantCulture),
            ["UsoPercentual"] = status.MemoryLoad.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static IReadOnlyDictionary<string, string> CollectStorage(CancellationToken token)
    {
        var facts = new Dictionary<string, string>();
        var index = 0;
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
        {
            token.ThrowIfCancellationRequested();
            var prefix = $"Volume {++index}";
            facts[$"{prefix} TotalBytes"] = drive.TotalSize.ToString(CultureInfo.InvariantCulture);
            facts[$"{prefix} FreeBytes"] = drive.AvailableFreeSpace.ToString(CultureInfo.InvariantCulture);
            facts[$"{prefix} Formato"] = drive.DriveFormat;
        }
        if (index == 0) throw new IOException("Nenhum volume disponível.");
        return facts;
    }

    private static IReadOnlyDictionary<string, string> CollectNetwork(CancellationToken token)
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback).ToArray();
        return new Dictionary<string, string>
        {
            ["Interfaces detectadas"] = interfaces.Length.ToString(CultureInfo.InvariantCulture),
            ["Interfaces ativas"] = interfaces.Count(n => n.OperationalStatus == OperationalStatus.Up).ToString(CultureInfo.InvariantCulture),
            ["Observação"] = "Interface ativa não comprova acesso à internet. Endereços IP e MAC não são coletados."
        };
    }

    private static IReadOnlyDictionary<string, string> CollectDrivers(CancellationToken token)
    {
        // Registry inventory only; no driver downloads, commands, or device changes.
        string[] classes = ["{4d36e968-e325-11ce-bfc1-08002be10318}", "{4d36e972-e325-11ce-bfc1-08002be10318}"];
        var facts = new Dictionary<string, string>();
        var index = 0;
        foreach (var classId in classes)
        {
            using var parent = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\" + classId);
            if (parent is null) continue;
            foreach (var name in parent.GetSubKeyNames().Where(n => n.Length == 4 && n.All(char.IsAsciiDigit)))
            {
                token.ThrowIfCancellationRequested();
                using var key = parent.OpenSubKey(name);
                if (key?.GetValue("DriverDesc") is not string description) continue;
                facts[$"{++index:D3} · {description}"] = $"{key.GetValue("ProviderName") ?? "Fornecedor indisponível"} · {key.GetValue("DriverVersion") ?? "Versão indisponível"}";
            }
        }
        facts["Escopo"] = "Adaptadores de vídeo e rede registrados; não verifica atualização, assinatura ou dispositivos presentes.";
        return facts;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    private sealed class LocalCollector(string category, Func<CancellationToken, IReadOnlyDictionary<string, string>> collect, string source = "Windows local / .NET") : IDiagnosticCollector
    {
        public string Category => category;
        public Task<CategoryResult> CollectAsync(CancellationToken cancellationToken) => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var facts = collect(cancellationToken).ToImmutableDictionary();
            // Scope is deliberately partial: these collectors do not assess health, temperature or driver freshness.
            return new CategoryResult(Category, CollectionState.Partial, DateTimeOffset.UtcNow.AddMinutes(5),
                source, .9, [], facts);
        }, cancellationToken);
    }
}
