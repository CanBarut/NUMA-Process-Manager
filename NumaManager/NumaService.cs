using System.Diagnostics;
using System.Management;

namespace NumaManager;

/// <summary>
/// NUMA topology ve sistem bilgilerini yöneten servis
/// </summary>
public class NumaService
{
    // Registry yolları (HKCU + HKLM) - global ve kullanıcı bazlı
    private const string RegPath = @"SOFTWARE\\LabsOffice\\NumaManager\\ProcessAffinity";

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
        public int SessionId { get; set; }
        public string UserName { get; set; } = "";
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
                        SessionId = SafeGetSessionId(process),
                        UserName = SafeGetProcessOwner(process.Id),
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
    /// Registry'den process adına göre affinity mask okur (HKLM öncelikli)
    /// </summary>
    public static bool TryGetAffinityFromRegistry(string processName, out IntPtr affinityMask, out string source)
    {
        affinityMask = IntPtr.Zero;
        source = string.Empty;

        string Normalize(string name)
        {
            try
            {
                var lower = (name ?? string.Empty).Trim().ToLowerInvariant();
                return lower.EndsWith(".exe") ? lower[..^4] : lower;
            }
            catch { return name ?? string.Empty; }
        }

        var keyNames = new[] { Normalize(processName), Normalize(processName) + ".exe" };

        bool TryRead(Microsoft.Win32.RegistryKey root, out IntPtr mask)
        {
            mask = IntPtr.Zero;
            try
            {
                using var key = root.OpenSubKey(RegPath);
                if (key == null) return false;
                foreach (var k in keyNames)
                {
                    var val = key.GetValue(k)?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        if (long.TryParse(val, System.Globalization.NumberStyles.HexNumber, null, out long parsed))
                        {
                            mask = new IntPtr(parsed);
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        if (TryRead(Microsoft.Win32.Registry.LocalMachine, out var mHklm))
        {
            affinityMask = mHklm; source = "HKLM"; return true;
        }
        if (TryRead(Microsoft.Win32.Registry.CurrentUser, out var mHkcu))
        {
            affinityMask = mHkcu; source = "HKCU"; return true;
        }
        return false;
    }

    /// <summary>
    /// Çalışan tüm process'ler için registry'deki eşleşmelere göre affinity uygular
    /// </summary>
    public static void ApplyRegistryAffinitiesToRunningProcesses()
    {
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (p.HasExited) continue;
                if (TryGetAffinityFromRegistry(p.ProcessName, out var mask, out _))
                {
                    try { p.ProcessorAffinity = mask; } catch { }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// INI dosya yolu (uygulama dizininde)
    /// </summary>
    public static string IniFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "affinity_rules.ini");

    private static string NormalizeProcessName(string name)
    {
        var n = (name ?? string.Empty).Trim().ToLowerInvariant();
        return n.EndsWith(".exe") ? n[..^4] : n;
    }

    /// <summary>
    /// INI: Uygulama adı → Hex mask sözlüğü okur
    /// </summary>
    public static Dictionary<string, string> ReadIniRules()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (!File.Exists(IniFilePath)) return dict;
            foreach (var raw in File.ReadAllLines(IniFilePath))
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || !line.Contains('=')) continue;
                var idx = line.IndexOf('=');
                var key = NormalizeProcessName(line.Substring(0, idx).Trim());
                var val = line[(idx + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
                {
                    dict[key] = val;
                }
            }
        }
        catch { }
        return dict;
    }

    /// <summary>
    /// INI sözlüğünü yazar
    /// </summary>
    public static void WriteIniRules(Dictionary<string, string> rules)
    {
        try
        {
            var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in rules)
            {
                normalized[NormalizeProcessName(kv.Key)] = kv.Value;
            }

            var lines = new List<string> { "# UygulamaAdi=HexMask", "# ornek: capital=0000000F" };
            lines.AddRange(normalized.OrderBy(k => k.Key).Select(kv => $"{kv.Key}={kv.Value}"));
            File.WriteAllLines(IniFilePath, lines);
        }
        catch { }
    }

    /// <summary>
    /// INI+Registry kurallarını çalışan süreçlere uygular
    /// </summary>
    public static void ApplyAllRulesToRunningProcesses()
    {
        var ini = ReadIniRules();
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (p.HasExited) continue;

                // Öncelik: INI (uygulama adı), ardından Registry override
                var key = NormalizeProcessName(p.ProcessName);
                if (ini.TryGetValue(key, out var hex) && long.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var parsedIni))
                {
                    try { p.ProcessorAffinity = new IntPtr(parsedIni); } catch { }
                    continue;
                }

                if (TryGetAffinityFromRegistry(p.ProcessName, out var mask, out _))
                {
                    try { p.ProcessorAffinity = mask; } catch { }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Process başladığında tetiklenecek: eşleşme varsa mask uygular, INI’ye log yazar
    /// </summary>
    public static void TriggerForProcessStart(Process process)
    {
        try
        {
            if (process.HasExited) return;

            // Öncelik: INI
            var ini = ReadIniRules();
            var key = NormalizeProcessName(process.ProcessName);
            if (ini.TryGetValue(key, out var hex) && long.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var parsedIni))
            {
                try { process.ProcessorAffinity = new IntPtr(parsedIni); } catch { }
                return;
            }

            // Registry
            if (TryGetAffinityFromRegistry(process.ProcessName, out var mask, out _))
            {
                try { process.ProcessorAffinity = mask; } catch { }
                return;
            }
        }
        catch { }
    }

    /// <summary>
    /// Güvenli şekilde SessionId okur (erişim hatalarında 0 döner)
    /// </summary>
    private static int SafeGetSessionId(Process process)
    {
        try { return process.SessionId; } catch { return 0; }
    }

    /// <summary>
    /// Process'in çalıştığı kullanıcı adını DOMAIN\User formatında döner (erişim hatasında boş döner)
    /// </summary>
    private static string SafeGetProcessOwner(int pid)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE ProcessId = {pid}");
            foreach (ManagementObject obj in searcher.Get())
            {
                var outParams = obj.InvokeMethod("GetOwner", null, null) as ManagementBaseObject;
                var user = outParams?["User"]?.ToString();
                var domain = outParams?["Domain"]?.ToString();
                if (!string.IsNullOrWhiteSpace(user))
                {
                    return string.IsNullOrWhiteSpace(domain) ? user! : $"{domain}\\{user}";
                }
            }
        }
        catch { }
        return string.Empty;
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
        /// Seçili işlem (PID/Session) için optimal NUMA ataması yapar
        /// </summary>
        public static List<int> GetOptimalErpAssignment(ProcessorInfo systemInfo, ProcessInfo targetProcess)
        {
            // Aynı uygulamanın çalışan tüm örnekleri
            var erpProcesses = GetErpProcesses()
                .Where(p => p.ProcessName.Equals(targetProcess.ProcessName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Önce aynı session içindeki mevcut atamayı koru
            var sameSession = erpProcesses.Where(p => p.SessionId == targetProcess.SessionId).ToList();
            var sessionNodeId = DetermineDominantNodeIdForProcesses(sameSession, systemInfo);
            if (sessionNodeId.HasValue)
            {
                return systemInfo.Nodes.First(n => n.NodeId == sessionNodeId.Value).ProcessorIds.Take(4).ToList();
            }

            // Node başına kaç farklı session var? (eşitlikte daha az yük)
            var sessionsPerNode = systemInfo.Nodes.ToDictionary(n => n.NodeId, _ => new HashSet<int>());
            foreach (var p in erpProcesses)
            {
                var nodeId = DetermineDominantNodeIdForProcess(p, systemInfo);
                if (nodeId.HasValue)
                {
                    sessionsPerNode[nodeId.Value].Add(p.SessionId);
                }
            }

            // Eğer hiç atama yoksa, sessionId'ye göre round-robin
            var haveAny = sessionsPerNode.Any(kv => kv.Value.Count > 0);
            if (!haveAny)
            {
                var rrIndex = Math.Abs(targetProcess.SessionId) % Math.Max(systemInfo.Nodes.Count, 1);
                return systemInfo.Nodes[rrIndex].ProcessorIds.Take(4).ToList();
            }

            var targetNodeId = sessionsPerNode.OrderBy(kv => kv.Value.Count).First().Key;
            return systemInfo.Nodes.First(n => n.NodeId == targetNodeId).ProcessorIds.Take(4).ToList();
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
        /// Belirli bir process için dominant NUMA node ID'sini döner (mask tüm CPU'lar ise null)
        /// </summary>
        private static int? DetermineDominantNodeIdForProcess(ProcessInfo process, ProcessorInfo systemInfo)
        {
            var processCpus = GetProcessCpuIds(process.CurrentAffinity);
            if (processCpus.Count == 0 || processCpus.Count >= systemInfo.LogicalProcessors)
                return null; // Atanmamış/tüm CPU'lar

            var counts = new Dictionary<int, int>();
            foreach (var node in systemInfo.Nodes)
            {
                counts[node.NodeId] = processCpus.Count(cpu => node.ProcessorIds.Contains(cpu));
            }
            var best = counts.OrderByDescending(kv => kv.Value).First();
            return best.Value > 0 ? best.Key : null;
        }

        /// <summary>
        /// Bir grup process için dominant node'u döner (çoğunluğa göre)
        /// </summary>
        private static int? DetermineDominantNodeIdForProcesses(List<ProcessInfo> processes, ProcessorInfo systemInfo)
        {
            var tallies = new Dictionary<int, int>();
            foreach (var node in systemInfo.Nodes) tallies[node.NodeId] = 0;

            foreach (var p in processes)
            {
                var nodeId = DetermineDominantNodeIdForProcess(p, systemInfo);
                if (nodeId.HasValue) tallies[nodeId.Value]++;
            }

            var best = tallies.OrderByDescending(kv => kv.Value).First();
            return best.Value > 0 ? best.Key : (int?)null;
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
