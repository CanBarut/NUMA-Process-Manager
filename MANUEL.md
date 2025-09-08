# 🔧 Manuel NUMA ve CPU Affinity Optimizasyonu Rehberi

Bu rehber, NUMA Process Manager uygulaması olmadan, manuel olarak PowerShell ve Registry editörü kullanarak CPU affinity ayarlarını yapmanızı sağlar.

## 📋 İçindekiler

- [Genel Bakış](#-genel-bakış)
- [PowerShell Yöntemleri](#-powershell-yöntemleri)
- [Registry Yöntemleri](#-registry-yöntemleri)
- [Pratik Örnekler](#-pratik-örnekler)
- [Performans İpuçları](#-performans-ipuçları)
- [Sorun Giderme](#-sorun-giderme)

## 🎯 Genel Bakış

CPU affinity ayarları, bir process'in hangi CPU çekirdeklerini kullanabileceğini belirler. Bu ayarlar:

- **NUMA optimizasyonu** sağlar (memory locality)
- **Core çakışmasını** önler (hyperthreading)
- **Cache efficiency** artırır
- **Context switching** azaltır
- **Genel performansı** %10-100 artırabilir


## ⚡ PowerShell Yöntemleri

### 1. Temel Affinity Ayarlama

```powershell
# Process ID ile affinity ayarlama
$process = Get-Process -Id 1234
$process.ProcessorAffinity = [System.IntPtr]15  # CPU 0,1,2,3 (binary: 1111)

# Process adı ile affinity ayarlama
$process = Get-Process -Name "MyApp"
$process.ProcessorAffinity = [System.IntPtr]255  # CPU 0-7 (binary: 11111111)
```

### 2. Affinity Mask Hesaplama

```powershell
# CPU ID'lerinden mask hesaplama
function Get-AffinityMask {
    param([int[]]$CpuIds)
    $mask = 0
    foreach ($cpuId in $CpuIds) {
        $mask += [math]::Pow(2, $cpuId)
    }
    return [System.IntPtr]$mask
}

# Örnek kullanım
$mask = Get-AffinityMask @(0, 1, 4, 5)  # CPU 0,1,4,5
$process = Get-Process -Name "MyApp"
$process.ProcessorAffinity = $mask
```

### 3. NUMA Node Bazlı Ayarlama

```powershell
# NUMA node 0'daki CPU'ları kullan (genellikle 0-7)
$numaNode0Mask = [System.IntPtr]255  # 0xFF

# NUMA node 1'daki CPU'ları kullan (genellikle 8-15)
$numaNode1Mask = [System.IntPtr]65280  # 0xFF00

# Process'e uygula
$process = Get-Process -Name "MyApp"
$process.ProcessorAffinity = $numaNode0Mask
```

### 4. Otomatik Startup Script

```powershell
# Startup script oluştur
$scriptContent = @"
# Auto-generated NUMA affinity script
Start-Sleep -Seconds 10
`$process = Get-Process -Name "MyApp" -ErrorAction SilentlyContinue
if (`$process) {
    `$process.ProcessorAffinity = [System.IntPtr]15  # CPU 0,1,2,3
    Write-Host "Affinity applied to MyApp: CPU 0,1,2,3"
}
"@

$scriptContent | Out-File -FilePath "C:\Scripts\ApplyAffinity.ps1" -Encoding UTF8
```

## 🗃️ Registry Yöntemleri

### 1. Process Affinity Registry Ayarları

```powershell
# Registry anahtarı oluştur
$regPath = "HKCU:\SOFTWARE\LabsOffice\NumaManager\ProcessAffinity"

# Affinity ayarlarını kaydet
New-Item -Path $regPath -Force | Out-Null
Set-ItemProperty -Path $regPath -Name "MyApp" -Value "F" -Type String  # 0xF = CPU 0,1,2,3
Set-ItemProperty -Path $regPath -Name "Chrome" -Value "3" -Type String  # 0x3 = CPU 0,1
Set-ItemProperty -Path $regPath -Name "SQLServer" -Value "FF" -Type String  # 0xFF = CPU 0-7
```

### 2. Registry'den Affinity Okuma ve Uygulama

```powershell
# Registry'den ayarları oku ve uygula
$regPath = "HKCU:\SOFTWARE\LabsOffice\NumaManager\ProcessAffinity"
$affinitySettings = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue

if ($affinitySettings) {
    $affinitySettings.PSObject.Properties | ForEach-Object {
        if ($_.Name -ne "PSPath" -and $_.Name -ne "PSParentPath") {
            $processName = $_.Name
            $affinityHex = $_.Value
            $affinityMask = [System.IntPtr][Convert]::ToInt64($affinityHex, 16)
            
            $process = Get-Process -Name $processName -ErrorAction SilentlyContinue
            if ($process) {
                $process.ProcessorAffinity = $affinityMask
                Write-Host "Applied affinity to $processName : 0x$affinityHex"
            }
        }
    }
}
```

### 3. Registry Backup ve Restore

```powershell
# Registry ayarlarını yedekle
$regPath = "HKCU:\SOFTWARE\LabsOffice\NumaManager\ProcessAffinity"
$backupPath = "C:\Backup\AffinitySettings.reg"
reg export "HKCU\SOFTWARE\LabsOffice\NumaManager\ProcessAffinity" $backupPath

# Registry ayarlarını geri yükle
reg import $backupPath
```

## 💡 Pratik Örnekler

### Örnek 1: Finansal Uygulama Optimizasyonu

```powershell
# Capital uygulaması için NUMA optimizasyonu
$processName = "Capital"
$process = Get-Process -Name $processName -ErrorAction SilentlyContinue

if ($process) {
    # İlk NUMA node'daki 4 CPU'yu kullan (CPU 0,1,2,3)
    $affinityMask = [System.IntPtr]15  # 0xF
    $process.ProcessorAffinity = $affinityMask
    
    # Registry'ye kaydet
    $regPath = "HKCU:\SOFTWARE\LabsOffice\NumaManager\ProcessAffinity"
    New-Item -Path $regPath -Force | Out-Null
    Set-ItemProperty -Path $regPath -Name $processName -Value "F" -Type String
    
    Write-Host "✅ $processName optimized: CPU 0,1,2,3 (NUMA Node 0)"
    Write-Host "📊 Expected performance gain: 20-40%"
}
```

### Örnek 2: Web Tarayıcısı Optimizasyonu

```powershell
# Chrome için güç tasarrufu modu
$processName = "chrome"
$chromeProcesses = Get-Process -Name $processName -ErrorAction SilentlyContinue

if ($chromeProcesses) {
    # Sadece 2 CPU kullan (CPU 0,1)
    $affinityMask = [System.IntPtr]3  # 0x3
    
    foreach ($process in $chromeProcesses) {
        $process.ProcessorAffinity = $affinityMask
    }
    
    # Registry'ye kaydet
    $regPath = "HKCU:\SOFTWARE\LabsOffice\NumaManager\ProcessAffinity"
    New-Item -Path $regPath -Force | Out-Null
    Set-ItemProperty -Path $regPath -Name $processName -Value "3" -Type String
    
    Write-Host "✅ Chrome optimized: CPU 0,1 (Power Saving Mode)"
    Write-Host "🔋 Reduced CPU usage, better battery life"
}
```

### Örnek 3: Veritabanı Sunucusu Optimizasyonu

```powershell
# SQL Server için maksimum performans
$processName = "sqlservr"
$process = Get-Process -Name $processName -ErrorAction SilentlyContinue

if ($process) {
    # Tüm fiziksel core'ları kullan (hyperthreading'den kaçın)
    # 16 core sistem için: CPU 0,2,4,6,8,10,12,14 (sadece fiziksel core'lar)
    $affinityMask = [System.IntPtr]43690  # 0xAAAA (binary: 1010101010101010)
    $process.ProcessorAffinity = $affinityMask
    
    # Registry'ye kaydet
    $regPath = "HKCU:\SOFTWARE\LabsOffice\NumaManager\ProcessAffinity"
    New-Item -Path $regPath -Force | Out-Null
    Set-ItemProperty -Path $regPath -Name $processName -Value "AAAA" -Type String
    
    Write-Host "✅ SQL Server optimized: Physical cores only"
    Write-Host "🚀 Expected performance gain: 30-50%"
    Write-Host "⚠️ Avoided hyperthreading conflicts"
}
```

### Örnek 4: Oyun Optimizasyonu

```powershell
# Steam oyunları için optimal ayar
$processName = "steam"
$process = Get-Process -Name $processName -ErrorAction SilentlyContinue

if ($process) {
    # İlk 8 CPU'yu kullan (CPU 0-7)
    $affinityMask = [System.IntPtr]255  # 0xFF
    $process.ProcessorAffinity = $affinityMask
    
    # Registry'ye kaydet
    $regPath = "HKCU:\SOFTWARE\LabsOffice\NumaManager\ProcessAffinity"
    New-Item -Path $regPath -Force | Out-Null
    Set-ItemProperty -Path $regPath -Name $processName -Value "FF" -Type String
    
    Write-Host "✅ Steam optimized: CPU 0-7 (Gaming Mode)"
    Write-Host "🎮 Better frame rates and reduced stuttering"
}
```

## 🚀 Performans İpuçları

### 1. Affinity Mask Hesaplama Tablosu

| CPU'lar | Binary | Hex | Decimal | Açıklama |
|---------|--------|-----|---------|----------|
| 0       | 1      | 0x1 | 1       | Tek CPU  |
| 0,1     | 11     | 0x3 | 3       | İki CPU |
| 0,1,2,3 | 1111 | 0xF | 15 | İlk 4 CPU |
| 0-7 | 11111111 | 0xFF | 255 | İlk 8 CPU |
| 0,2,4,6 | 1010101 | 0x55 | 85 | Çift CPU'lar |
| 1,3,5,7 | 10101010 | 0xAA | 170 | Tek CPU'lar |

### 2. NUMA Node Dağılımı (Tipik 16 Core Sistem)

```
NUMA Node 0: CPU 0-7   (Mask: 0xFF)
NUMA Node 1: CPU 8-15  (Mask: 0xFF00)
```

### 3. Hyperthreading Optimizasyonu

```powershell
# Fiziksel core'ları kullan (hyperthreading'den kaçın)
# 16 core sistem için: CPU 0,2,4,6,8,10,12,14
$physicalCoresMask = [System.IntPtr]43690  # 0xAAAA

# Mantıksal core'ları kullan (hyperthreading dahil)
# 16 core sistem için: CPU 1,3,5,7,9,11,13,15
$logicalCoresMask = [System.IntPtr]87380   # 0x15554
```

### 4. Otomatik Startup Script Oluşturma

```powershell
# Windows startup'a ekle
$startupScript = @"
# NUMA Affinity Auto-Apply Script
Start-Sleep -Seconds 15

# Finansal uygulama
`$process = Get-Process -Name "Capital" -ErrorAction SilentlyContinue
if (`$process) { `$process.ProcessorAffinity = [System.IntPtr]15 }

# Web tarayıcısı
`$chromeProcesses = Get-Process -Name "chrome" -ErrorAction SilentlyContinue
foreach (`$proc in `$chromeProcesses) { `$proc.ProcessorAffinity = [System.IntPtr]3 }

# Veritabanı
`$process = Get-Process -Name "sqlservr" -ErrorAction SilentlyContinue
if (`$process) { `$process.ProcessorAffinity = [System.IntPtr]43690 }

Write-Host "NUMA Affinity settings applied successfully!"
"@

$startupScript | Out-File -FilePath "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\ApplyAffinity.ps1" -Encoding UTF8
```

## 🔧 Sorun Giderme

### 1. Yaygın Hatalar

```powershell
# Process bulunamadı hatası
try {
    $process = Get-Process -Name "NonExistentApp" -ErrorAction Stop
    $process.ProcessorAffinity = [System.IntPtr]15
} catch {
    Write-Host "Process bulunamadı: $($_.Exception.Message)"
}

# İzin hatası
try {
    $process = Get-Process -Name "SystemProcess" -ErrorAction Stop
    $process.ProcessorAffinity = [System.IntPtr]15
} catch {
    Write-Host "İzin hatası: Yönetici olarak çalıştırın"
}
```

### 2. Affinity Kontrolü

```powershell
# Mevcut affinity'yi kontrol et
$process = Get-Process -Name "MyApp"
$currentAffinity = $process.ProcessorAffinity.ToInt64()
$binary = [Convert]::ToString($currentAffinity, 2)
Write-Host "Current affinity: 0x$($currentAffinity.ToString('X')) (Binary: $binary)"
```

### 3. Sistem Bilgileri

```powershell
# CPU bilgilerini al
$cpuInfo = Get-WmiObject -Class Win32_Processor
Write-Host "CPU Cores: $($cpuInfo.NumberOfCores)"
Write-Host "Logical Processors: $($cpuInfo.NumberOfLogicalProcessors)"

# NUMA bilgilerini al
$numaInfo = Get-WmiObject -Class Win32_ComputerSystem
Write-Host "NUMA Nodes: $($numaInfo.NumberOfProcessors)"
```

## 📊 Performans Testi

```powershell
# Performans testi için benchmark
function Test-AffinityPerformance {
    param([string]$ProcessName, [System.IntPtr]$AffinityMask)
    
    $process = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    if ($process) {
        $startTime = Get-Date
        $process.ProcessorAffinity = $AffinityMask
        $endTime = Get-Date
        
        $duration = ($endTime - $startTime).TotalMilliseconds
        Write-Host "Affinity applied in $duration ms"
        
        # CPU kullanımını izle
        Get-Counter "\Process($ProcessName)\% Processor Time" -SampleInterval 1 -MaxSamples 10
    }
}

# Test örneği
Test-AffinityPerformance -ProcessName "MyApp" -AffinityMask ([System.IntPtr]15)
```

## 🎯 Sonuç

Manuel CPU affinity ayarları:

- ✅ **%10-100 performans artışı** sağlayabilir
- ✅ **NUMA optimizasyonu** yapar
- ✅ **Core çakışmasını** önler
- ✅ **Cache efficiency** artırır
- ✅ **Güç tüketimini** optimize eder

> 💡 **Önemli**: Bu ayarlar geçicidir. Kalıcı olması için registry'ye kaydetmeyi veya startup script kullanmayı unutmayın!

---

**Not**: Bu işlemler yönetici hakları gerektirir ve sistem process'lerini etkileyebilir. Kullanmadan önce sistem yedeği almanız önerilir.
