# 🖥️ NUMA Process Manager

**Labs Office** geliştirici ekibi tarafından geliştirilmiştir, Windows sistemlerde NUMA (Non-Uniform Memory Access) optimizasyonu için gelişmiş bir process yönetim aracı.

## 📋 İçindekiler

- [Genel Bakış](#-genel-bakış)
- [Özellikler](#-özellikler)
- [Sistem Gereksinimleri](#-sistem-gereksinimleri)
- [Kurulum](#-kurulum)
- [Kullanım](#-kullanım)
- [Teknik Detaylar](#-teknik-detaylar)
- [API Referansı](#-api-referansı)
- [Örnekler](#-örnekler)
- [Sorun Giderme](#-sorun-giderme)
- [Katkıda Bulunma](#-katkıda-bulunma)
- [Lisans](#-lisans)

## 🎯 Genel Bakış

NUMA Process Manager, çok çekirdekli Windows sistemlerde çalışan process'lerin CPU affinity ayarlarını yöneterek NUMA performansını optimize eden profesyonel bir araçtır. Özellikle erp uygulamalar, web servisleri ve yoğun hesaplama gerektiren uygulamalar için tasarlanmıştır.

### 🎨 Ana Özellikler

- **🔍 Akıllı Sistem Analizi**: CPU, RAM, NUMA node'lar ve process'lerin gerçek zamanlı analizi
- **🎯 NUMA-Aware Optimizasyon**: Memory locality için aynı NUMA node'daki CPU'ları kullanma
- **⚡ Performans Tahmini**: Affinity değişikliklerinin performans etkisini önceden hesaplama
- **🔧 Gelişmiş CPU Seçimi**: Manuel, otomatik ve profesyonel mod seçenekleri
- **💾 Kalıcı Ayarlar**: Registry kaydetme ve startup script oluşturma
- **📊 Detaylı Analiz**: Core birleşme, hyperthreading çakışması ve NUMA dağılım analizi

## ✨ Özellikler

### 🖥️ Sistem Bilgileri
- **CPU Detayları**: Çekirdek sayısı, thread sayısı, socket sayısı
- **NUMA Topology**: Node sayısı ve CPU dağılımı
- **Gerçek Zamanlı Metrikler**: CPU kullanımı, RAM kullanımı
- **Process Listesi**: Çalışan tüm process'ler ve mevcut affinity durumları

### 🎯 Akıllı Affinity Yönetimi
- **Process Türü Analizi**: Finansal, web, tarayıcı, veritabanı uygulamaları için özel öneriler
- **NUMA Optimizasyonu**: Memory locality için aynı node'daki CPU'ları seçme
- **Core Birleşme Uyarısı**: Hyperthreading çakışması tespiti ve uyarısı
- **Performans Tahmini**: Beklenen iyileşme ve risk seviyesi analizi

### 🔧 Gelişmiş CPU Seçimi
- **Görsel CPU Grid**: NUMA node'lara göre renk kodlaması
- **Manuel Giriş**: Aralık desteği (örn: 0,1,4,5,8-11,16,20-23)
- **Hızlı Seçim**: Fibonacci, asal sayılar, NUMA node'lar
- **Affinity Mask**: Doğrudan hex/decimal girişi (0xFF, 0x15, 255)

### 💾 Kalıcı Ayarlar
- **Registry Kaydetme**: HKEY_CURRENT_USER altında kalıcı saklama
- **Startup Script**: PowerShell ile otomatik uygulama
- **Backup/Restore**: JSON formatında ayar yedekleme
- **Toplu Yönetim**: Silme, dışa/içe aktarma işlemleri

## 🖥️ Sistem Gereksinimleri

- **İşletim Sistemi**: Windows 10/11 (x64)
- **.NET Framework**: .NET 8.0 Runtime
- **RAM**: Minimum 4GB (8GB önerilen)
- **CPU**: Çok çekirdekli işlemci (NUMA desteği önerilen)
- **İzinler**: Yönetici hakları (process affinity değiştirmek için)

## 🚀 Kurulum

### 1. Gereksinimler
```bash
# .NET 8.0 Runtime'ı indirin ve kurun
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### 2. Uygulamayı Çalıştırma
```bash
# Yönetici olarak çalıştırın
NumaManager.exe
```

### 3. İlk Kurulum
1. Uygulamayı yönetici olarak çalıştırın
2. Sistem bilgileri otomatik olarak yüklenecektir
3. Process listesi 5 saniyede bir otomatik yenilenecektir

## 📖 Kullanım

### 🎯 Temel Kullanım

#### 1. Process Seçimi
- Ana formdaki process listesinden bir process seçin
- Process'in mevcut affinity durumu sağ panelde görüntülenir

#### 2. CPU Seçimi
- **NUMA Node Seçimi**: Dropdown'dan bir NUMA node seçin
- **CPU Seçimi**: Checkbox'larla istediğiniz CPU'ları seçin
- **Hızlı Seçim**: "İlk N CPU", "Son N CPU", "Orta N CPU" butonlarını kullanın

#### 3. Affinity Uygulama
- **"Affinity Uygula"** butonuna tıklayın
- Gelişmiş dialog açılacaktır
- CPU seçimini yapın ve **"Affinity Uygula"** butonuna tıklayın

### 🔧 Gelişmiş Kullanım

#### AffinityDialog Özellikleri
- **Akıllı Öneriler**: Process türüne göre otomatik öneriler
- **NUMA Analizi**: Memory locality analizi
- **Performans Tahmini**: Beklenen iyileşme hesaplama
- **Core Birleşme Uyarısı**: Hyperthreading çakışması tespiti

#### Profesyonel Mod
- **Manuel CPU Girişi**: "0,1,4,5,8-11,16,20-23" formatında
- **Affinity Mask**: Doğrudan "0xFF" veya "255" formatında
- **Hızlı Seçim**: Fibonacci, asal sayılar, NUMA node'lar
- **Gelişmiş Analiz**: Core çakışması ve NUMA dağılım analizi

#### Registry Yönetimi
- **Kalıcı Ayarlar**: Process'ler için kalıcı affinity ayarları
- **Backup/Restore**: JSON formatında ayar yedekleme
- **Toplu İşlemler**: Silme, dışa/içe aktarma

### 📊 Örnek Kullanım Senaryoları

#### Senaryo 1: Finansal Uygulama Optimizasyonu
```
1. "capital" process'ini seçin
2. Sistem otomatik olarak 4-8 CPU önerir
3. İlk NUMA node'daki CPU'ları seçin
4. "Kalıcı olarak kaydet" seçeneğini işaretleyin
5. Affinity uygulayın
```

#### Senaryo 2: Web Servisi Optimizasyonu
```
1. "labsmobile" process'ini seçin
2. Sistem 4-8 CPU önerir
3. Profesyonel mod'u açın
4. Manuel olarak "0,1,2,3,4,5" girin
5. Performans analizini inceleyin
```

#### Senaryo 3: Güç Tasarrufu
```
1. Profesyonel mod'u açın
2. "Güç Tasarrufu" butonuna tıklayın
3. Sadece ilk NUMA node'daki 4 CPU seçilir
4. Affinity uygulayın
```

## 🔧 Teknik Detaylar

### 🏗️ Mimari

```
NumaManager/
├── Form1.cs                 # Ana form ve UI yönetimi
├── NumaService.cs           # Core servis ve sistem analizi
├── AffinityDialog.cs        # Gelişmiş affinity dialog'u
├── ProfessionalCpuDialog.cs # Profesyonel CPU seçim dialog'u
├── RegistryManagerDialog.cs # Registry yönetim dialog'u
└── Program.cs               # Uygulama giriş noktası
```

### 🔍 NUMA Hesaplama Algoritması

```csharp
// NUMA node'ları oluştur
int coresPerNode = info.LogicalProcessors / Math.Max(info.NumaNodes, 1);
for (int nodeId = 0; nodeId < info.NumaNodes; nodeId++)
{
    var node = new NumaNode { NodeId = nodeId };
    for (int i = 0; i < coresPerNode; i++)
    {
        node.ProcessorIds.Add(nodeId * coresPerNode + i);
    }
}
```

### ⚡ Affinity Mask Hesaplama

```csharp
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
```

### 🔧 Core Birleşme Kontrolü

```csharp
private (bool HasCollisions, List<int> CollidingCores, List<int> CollidingCpus) CheckCoreCollisions(List<int> cpuIds)
{
    var coreGroups = cpuIds.GroupBy(cpu => cpu / 2).ToList();
    var hyperthreadingCores = coreGroups.Where(g => g.Count() > 1).ToList();
    // Hyperthreading çakışması kontrolü
}
```

## 📚 API Referansı

### NumaService Sınıfı

#### GetProcessorInfo()
Sistem işlemci bilgilerini alır.
```csharp
var processorInfo = NumaService.GetProcessorInfo();
// Returns: ProcessorInfo object with CPU, NUMA node details
```

#### GetRunningProcesses()
Çalışan process'leri listeler.
```csharp
var processes = NumaService.GetRunningProcesses();
// Returns: List<ProcessInfo> with process details
```

#### SetProcessAffinity(int processId, IntPtr affinityMask)
Process'in CPU affinity'sini ayarlar.
```csharp
bool success = NumaService.SetProcessAffinity(1234, affinityMask);
// Returns: true if successful, false otherwise
```

#### CalculateAffinityMask(List<int> processorIds)
CPU ID'lerinden affinity mask hesaplar.
```csharp
IntPtr mask = NumaService.CalculateAffinityMask(new List<int> {0, 1, 2, 3});
// Returns: IntPtr representing the affinity mask
```

### ProcessInfo Sınıfı

```csharp
public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; }
    public string WindowTitle { get; set; }
    public IntPtr CurrentAffinity { get; set; }
    public string CurrentAffinityMask { get; set; }
    public double CpuUsage { get; set; }
    public long WorkingSetMB { get; set; }
}
```

### ProcessorInfo Sınıfı

```csharp
public class ProcessorInfo
{
    public int TotalCores { get; set; }
    public int LogicalProcessors { get; set; }
    public int PhysicalProcessors { get; set; }
    public int NumaNodes { get; set; }
    public List<NumaNode> Nodes { get; set; }
}
```

## 💡 Örnekler

### Örnek 1: Basit Affinity Uygulama

```csharp
// Process ID'si 1234 olan process'e CPU 0,1,2,3'ü ata
var cpuIds = new List<int> {0, 1, 2, 3};
var affinityMask = NumaService.CalculateAffinityMask(cpuIds);
bool success = NumaService.SetProcessAffinity(1234, affinityMask);

if (success)
{
    Console.WriteLine("Affinity başarıyla uygulandı!");
}
```

### Örnek 2: NUMA Node Analizi

```csharp
var processorInfo = NumaService.GetProcessorInfo();
foreach (var node in processorInfo.Nodes)
{
    Console.WriteLine($"NUMA Node {node.NodeId}:");
    Console.WriteLine($"  CPU'lar: {string.Join(", ", node.ProcessorIds)}");
    Console.WriteLine($"  Affinity Mask: {node.AffinityMask}");
}
```

### Örnek 3: Process Analizi

```csharp
var processes = NumaService.GetRunningProcesses();
var targetProcess = processes.FirstOrDefault(p => p.ProcessName == "MyApp");

if (targetProcess != null)
{
    Console.WriteLine($"Process: {targetProcess.ProcessName}");
    Console.WriteLine($"PID: {targetProcess.ProcessId}");
    Console.WriteLine($"Mevcut Affinity: {targetProcess.CurrentAffinityMask}");
    Console.WriteLine($"RAM Kullanımı: {targetProcess.WorkingSetMB} MB");
}
```

## 🐛 Sorun Giderme

### Yaygın Sorunlar

#### 1. "Process'e erişim izni olmayabilir" Hatası
**Çözüm**: Uygulamayı yönetici olarak çalıştırın.

#### 2. Affinity uygulanamıyor
**Çözüm**: 
- Process'in çalışır durumda olduğundan emin olun
- Yönetici haklarıyla çalıştırın
- Process'in sistem process'i olmadığından emin olun

#### 3. NUMA node'lar görünmüyor
**Çözüm**:
- Sisteminizde NUMA desteği olduğundan emin olun
- Çok çekirdekli işlemci kullandığınızdan emin olun
- WMI servisinin çalışır durumda olduğundan emin olun

#### 4. Performance counter hatası
**Çözüm**:
- Windows Performance Toolkit'in kurulu olduğundan emin olun
- Yönetici haklarıyla çalıştırın
- Sistem yeniden başlatmayı deneyin

### Log Dosyaları

Uygulama hata mesajlarını konsola yazdırır. Detaylı log için:

```csharp
// Hata mesajları Console.WriteLine ile yazdırılır
// Visual Studio Output penceresinde görüntüleyebilirsiniz
```

### Debug Modu

Debug modunda çalıştırmak için:

```bash
# Visual Studio'da F5 ile çalıştırın
# Veya komut satırından:
dotnet run --configuration Debug
```

## 🤝 Katkıda Bulunma

### Geliştirme Ortamı Kurulumu

1. **Repository'yi klonlayın**:
```bash
git clone https://github.com/NUMA-Process-Manager/numa-manager.git
cd numa-manager
```

2. **Gereksinimleri kurun**:
```bash
# .NET 8.0 SDK
# Visual Studio 2022 veya VS Code
```

3. **Projeyi derleyin**:
```bash
dotnet build
```

### Katkı Kuralları

1. **Kod Stili**: C# coding conventions'ı takip edin
2. **Test**: Yeni özellikler için test yazın
3. **Dokümantasyon**: Yeni API'ler için dokümantasyon ekleyin
4. **Commit Mesajları**: Açıklayıcı commit mesajları yazın

### Pull Request Süreci

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add amazing feature'`)
4. Branch'inizi push edin (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje **MIT Lisansı** altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakın.

## 📞 İletişim

- **Geliştirici**: Labs Office
- **Email**: info@labsoffice.com
- **Website**: https://labsoffice.com



**Not**: Bu uygulama yönetici hakları gerektirir ve sistem process'lerini etkileyebilir. Kullanmadan önce sistem yedeği almanız önerilir.
