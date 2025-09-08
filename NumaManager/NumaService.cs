using System.Diagnostics;
using System.Management;

namespace NumaManager;

/// <summary>
/// NUMA topology ve sistem bilgilerini yöneten servis
/// </summary>
public class NumaService
{
    /// <summary>
    /// Sistem işlemci bilgileri
    /// </summary>
    public class ProcessorInfo
    {
        public int TotalCores { get; set; }
        public int LogicalProcessors { get; set; }
        public int PhysicalProcessors { get; set; }
        public int NumaNodes { get; set; }
        public List<NumaNode> Nodes { get; set; } = new();
    }

    /// <summary>
    /// NUMA node bilgileri
    /// </summary>
    public class NumaNode
    {
        public int NodeId { get; set; }
        public List<int> ProcessorIds { get; set; } = new();
        public long AvailableMemoryMB { get; set; }
        public string AffinityMask { get; set; } = "";
    }

    /// <summary>
    /// Çalışan process bilgileri
    /// </summary>
    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "";
        public string WindowTitle { get; set; } = "";
        public IntPtr CurrentAffinity { get; set; }
        public string CurrentAffinityMask { get; set; } = "";
        public double CpuUsage { get; set; }
        public long WorkingSetMB { get; set; }
    }

    /// <summary>
    /// Sistem işlemci bilgilerini alır
    /// </summary>
    public static ProcessorInfo GetProcessorInfo()
    {
        var info = new ProcessorInfo();
        
        try
        {
            // WMI ile işlemci bilgileri
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                info.PhysicalProcessors = Convert.ToInt32(obj["NumberOfProcessors"]);
                info.LogicalProcessors = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
            }

            // Çekirdek sayısı
            using var cpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (ManagementObject obj in cpuSearcher.Get())
            {
                info.TotalCores += Convert.ToInt32(obj["NumberOfCores"]);
            }

            // NUMA node sayısı (basit hesaplama)
            info.NumaNodes = info.PhysicalProcessors;
            
            // NUMA node'ları oluştur
            int coresPerNode = info.LogicalProcessors / Math.Max(info.NumaNodes, 1);
            for (int nodeId = 0; nodeId < info.NumaNodes; nodeId++)
            {
                var node = new NumaNode { NodeId = nodeId };
                
                // Bu node'daki processor ID'leri
                for (int i = 0; i < coresPerNode; i++)
                {
                    node.ProcessorIds.Add(nodeId * coresPerNode + i);
                }
                
                // Affinity mask hesapla
                ulong mask = 0;
                foreach (int procId in node.ProcessorIds)
                {
                    if (procId < 64) // 64-bit limit
                        mask |= (1UL << procId);
                }
                node.AffinityMask = $"0x{mask:X}";
                
                info.Nodes.Add(node);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Processor info alınırken hata: {ex.Message}");
        }

        return info;
    }

    /// <summary>
    /// Çalışan process'leri listeler
    /// </summary>
    public static List<ProcessInfo> GetRunningProcesses()
    {
        var processes = new List<ProcessInfo>();
        
        try
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (process.HasExited) continue;
                    
                    var procInfo = new ProcessInfo
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        WindowTitle = process.MainWindowTitle,
                        WorkingSetMB = process.WorkingSet64 / (1024 * 1024)
                    };

                    // Mevcut affinity bilgisi
                    try
                    {
                        procInfo.CurrentAffinity = process.ProcessorAffinity;
                        procInfo.CurrentAffinityMask = $"0x{process.ProcessorAffinity.ToInt64():X}";
                    }
                    catch
                    {
                        procInfo.CurrentAffinityMask = "N/A";
                    }

                    processes.Add(procInfo);
                }
                catch
                {
                    // Process erişim hatası - atla
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Process listesi alınırken hata: {ex.Message}");
        }

        return processes.OrderBy(p => p.ProcessName).ToList();
    }

    /// <summary>
    /// Process'in CPU affinity'sini ayarlar
    /// </summary>
    public static bool SetProcessAffinity(int processId, IntPtr affinityMask)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            process.ProcessorAffinity = affinityMask;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Affinity ayarlanırken hata (PID: {processId}): {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// NUMA node'a göre affinity mask hesaplar
    /// </summary>
    public static IntPtr CalculateAffinityMask(List<int> processorIds)
    {
        ulong mask = 0;
        foreach (int procId in processorIds)
        {
            if (procId < 64) // 64-bit limit
                mask |= (1UL << procId);
        }
        return new IntPtr((long)mask);
    }

    /// <summary>
    /// Sistem performans sayaçlarını alır
    /// </summary>
    public static Dictionary<string, double> GetPerformanceCounters()
    {
        var counters = new Dictionary<string, double>();
        
        try
        {
            using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            using var memCounter = new PerformanceCounter("Memory", "Available MBytes");
            
            // İlk okuma (baseline)
            cpuCounter.NextValue();
            Thread.Sleep(100);
            
            counters["CPU_Usage"] = Math.Round(cpuCounter.NextValue(), 1);
            counters["Available_Memory_MB"] = Math.Round(memCounter.NextValue(), 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Performance counter hatası: {ex.Message}");
        }

        return counters;
    }

    /// <summary>
    /// CPU listesi için affinity mask string hesaplar
    /// </summary>
    public static string CalculateAffinityMaskString(List<int> processorIds)
    {
        ulong mask = 0;
        foreach (int procId in processorIds)
        {
            if (procId < 64) // 64-bit limit
                mask |= (1UL << procId);
        }
        return $"0x{mask:X}";
    }
}
