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

    /// <summary>
    /// ERP yazılımları için akıllı NUMA atama sistemi
    /// </summary>
    public class ErpSmartAssignment
    {
        /// <summary>
        /// ERP process'leri için optimal NUMA ataması yapar
        /// </summary>
        public static List<int> GetOptimalErpAssignment(ProcessorInfo systemInfo, string processName)
        {
            var erpProcesses = GetErpProcesses();
            var currentErpProcesses = erpProcesses.Where(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)).ToList();
            
            // ERP process türüne göre strateji belirle
            var strategy = DetermineErpStrategy(processName);
            
            switch (strategy)
            {
                case ErpStrategy.DatabaseServer:
                    return GetDatabaseServerAssignment(systemInfo, currentErpProcesses);
                
                case ErpStrategy.ApplicationServer:
                    return GetApplicationServerAssignment(systemInfo, currentErpProcesses);
                
                case ErpStrategy.WebServer:
                    return GetWebServerAssignment(systemInfo, currentErpProcesses);
                
                case ErpStrategy.ReportServer:
                    return GetReportServerAssignment(systemInfo, currentErpProcesses);
                
                default:
                    return GetDefaultErpAssignment(systemInfo, currentErpProcesses);
            }
        }

        /// <summary>
        /// ERP process türünü belirler
        /// </summary>
        private static ErpStrategy DetermineErpStrategy(string processName)
        {
            var name = processName.ToLower();
            
            // Veritabanı sunucuları
            if (name.Contains("sql") || name.Contains("oracle") || name.Contains("postgres") || 
                name.Contains("mysql") || name.Contains("db2") || name.Contains("sybase"))
                return ErpStrategy.DatabaseServer;
            
            // Web sunucuları
            if (name.Contains("iis") || name.Contains("apache") || name.Contains("nginx") || 
                name.Contains("tomcat") || name.Contains("weblogic") || name.Contains("websphere"))
                return ErpStrategy.WebServer;
            
            // Rapor sunucuları
            if (name.Contains("report") || name.Contains("crystal") || name.Contains("ssrs") || 
                name.Contains("cognos") || name.Contains("businessobjects"))
                return ErpStrategy.ReportServer;
            
            // ERP uygulama sunucuları
            if (name.Contains("sap") || name.Contains("oracle") || name.Contains("dynamics") || 
                name.Contains("netsuite") || name.Contains("capital") || name.Contains("capital.exe") || 
                name.Contains("labsmobile"))
                return ErpStrategy.ApplicationServer;
            
            return ErpStrategy.Default;
        }

        /// <summary>
        /// Veritabanı sunucuları için atama
        /// </summary>
        private static List<int> GetDatabaseServerAssignment(ProcessorInfo systemInfo, List<ProcessInfo> currentProcesses)
        {
            // Veritabanı sunucuları için: Son NUMA node'ları tercih et (genelde daha az kullanılır)
            var lastNode = systemInfo.Nodes.LastOrDefault();
            if (lastNode != null)
            {
                // Son node'daki son 4 CPU'yu al
                return lastNode.ProcessorIds.TakeLast(4).ToList();
            }
            
            return GetDefaultErpAssignment(systemInfo, currentProcesses);
        }

        /// <summary>
        /// Uygulama sunucuları için atama
        /// </summary>
        private static List<int> GetApplicationServerAssignment(ProcessorInfo systemInfo, List<ProcessInfo> currentProcesses)
        {
            // Uygulama sunucuları için: En az yüklü node'u bul
            var leastLoadedNode = GetLeastLoadedNode(systemInfo, currentProcesses);
            if (leastLoadedNode != null)
            {
                // En az yüklü node'daki ilk 4 CPU'yu al
                return leastLoadedNode.ProcessorIds.Take(4).ToList();
            }
            
            return GetDefaultErpAssignment(systemInfo, currentProcesses);
        }

        /// <summary>
        /// Web sunucuları için atama
        /// </summary>
        private static List<int> GetWebServerAssignment(ProcessorInfo systemInfo, List<ProcessInfo> currentProcesses)
        {
            // Web sunucuları için: Orta node'ları tercih et
            var middleNodeIndex = systemInfo.Nodes.Count / 2;
            if (middleNodeIndex < systemInfo.Nodes.Count)
            {
                var middleNode = systemInfo.Nodes[middleNodeIndex];
                // Orta node'daki orta CPU'ları al
                var startIndex = middleNode.ProcessorIds.Count / 2 - 2;
                var endIndex = startIndex + 4;
                return middleNode.ProcessorIds.Skip(startIndex).Take(4).ToList();
            }
            
            return GetDefaultErpAssignment(systemInfo, currentProcesses);
        }

        /// <summary>
        /// Rapor sunucuları için atama
        /// </summary>
        private static List<int> GetReportServerAssignment(ProcessorInfo systemInfo, List<ProcessInfo> currentProcesses)
        {
            // Rapor sunucuları için: İlk node'u tercih et (genelde daha stabil)
            var firstNode = systemInfo.Nodes.FirstOrDefault();
            if (firstNode != null)
            {
                // İlk node'daki son 4 CPU'yu al
                return firstNode.ProcessorIds.TakeLast(4).ToList();
            }
            
            return GetDefaultErpAssignment(systemInfo, currentProcesses);
        }

        /// <summary>
        /// Varsayılan ERP ataması
        /// </summary>
        private static List<int> GetDefaultErpAssignment(ProcessorInfo systemInfo, List<ProcessInfo> currentProcesses)
        {
            // Varsayılan: En az yüklü node'dan 4 CPU
            var leastLoadedNode = GetLeastLoadedNode(systemInfo, currentProcesses);
            if (leastLoadedNode != null)
            {
                return leastLoadedNode.ProcessorIds.Take(4).ToList();
            }
            
            // Fallback: İlk node'dan 4 CPU
            return systemInfo.Nodes.FirstOrDefault()?.ProcessorIds.Take(4).ToList() ?? new List<int>();
        }

        /// <summary>
        /// En az yüklü NUMA node'unu bulur
        /// </summary>
        private static NumaNode? GetLeastLoadedNode(ProcessorInfo systemInfo, List<ProcessInfo> currentProcesses)
        {
            var nodeLoads = new Dictionary<int, int>();
            
            // Her node için yük hesapla
            foreach (var node in systemInfo.Nodes)
            {
                var load = 0;
                foreach (var process in currentProcesses)
                {
                    var processCpus = GetProcessCpuIds(process.CurrentAffinity);
                    var nodeCpus = node.ProcessorIds;
                    
                    // Bu process'in kaç CPU'su bu node'da
                    load += processCpus.Count(cpu => nodeCpus.Contains(cpu));
                }
                nodeLoads[node.NodeId] = load;
            }
            
            // En az yüklü node'u bul
            var leastLoadedNodeId = nodeLoads.OrderBy(kvp => kvp.Value).FirstOrDefault().Key;
            return systemInfo.Nodes.FirstOrDefault(n => n.NodeId == leastLoadedNodeId);
        }

        /// <summary>
        /// Process'in kullandığı CPU ID'lerini döner
        /// </summary>
        private static List<int> GetProcessCpuIds(IntPtr affinityMask)
        {
            var cpuIds = new List<int>();
            var mask = affinityMask.ToInt64();
            
            for (int i = 0; i < 64; i++)
            {
                if ((mask & (1L << i)) != 0)
                {
                    cpuIds.Add(i);
                }
            }
            
            return cpuIds;
        }

        /// <summary>
        /// ERP process'lerini alır
        /// </summary>
        private static List<ProcessInfo> GetErpProcesses()
        {
            return GetRunningProcesses().Where(p => IsErpProcess(p.ProcessName)).ToList();
        }

        /// <summary>
        /// Process'in ERP process'i olup olmadığını kontrol eder
        /// </summary>
        private static bool IsErpProcess(string processName)
        {
            var name = processName.ToLower();
            var erpKeywords = new[]
            {
                "sap", "oracle", "dynamics", "netsuite", "capital", "capital.exe", 
                "labsmobile", "sql", "iis", "apache", "tomcat", "weblogic", "websphere", 
                "report", "crystal", "ssrs", "cognos"
            };
            
            return erpKeywords.Any(keyword => name.Contains(keyword));
        }
    }

    /// <summary>
    /// ERP strateji türleri
    /// </summary>
    public enum ErpStrategy
    {
        Default,
        DatabaseServer,
        ApplicationServer,
        WebServer,
        ReportServer
    }
}
