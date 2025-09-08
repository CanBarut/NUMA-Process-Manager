using System.Diagnostics;

namespace NumaManager;

/// <summary>
/// NUMA Process Manager - Ana Form
/// </summary>
public partial class Form1 : Form
{
    private NumaService.ProcessorInfo _processorInfo;
    private List<NumaService.ProcessInfo> _processes;
    private System.Windows.Forms.Timer _refreshTimer;

    public Form1()
    {
        InitializeComponent();
        InitializeUI();
        LoadSystemInfo();
        SetupTimer();
    }

    /// <summary>
    /// UI bileşenlerini başlatır
    /// </summary>
    private void InitializeUI()
    {
        this.Text = "NUMA Process Manager - LabsOffice";
        this.Size = new Size(1000, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        
        // Ana panel layout
        CreateControls();
        
        // Tooltip'leri ekle
        SetupTooltips();
    }

    /// <summary>
    /// Kontrolleri oluşturur
    /// </summary>
    private void CreateControls()
    {
        // Ana tablo layout
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(10)
        };
        
        // Column styles
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        
        // Row styles  
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F)); // System info
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Process list
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));  // Controls

        // Sistem bilgileri paneli
        var systemPanel = CreateSystemInfoPanel();
        mainLayout.Controls.Add(systemPanel, 0, 0);
        mainLayout.SetColumnSpan(systemPanel, 2);

        // Process listesi
        var processPanel = CreateProcessPanel();
        mainLayout.Controls.Add(processPanel, 0, 1);

        // NUMA kontrol paneli
        var numaPanel = CreateNumaControlPanel();
        mainLayout.Controls.Add(numaPanel, 1, 1);

        // Alt kontrol paneli
        var controlPanel = CreateControlPanel();
        mainLayout.Controls.Add(controlPanel, 0, 2);
        mainLayout.SetColumnSpan(controlPanel, 2);

        this.Controls.Add(mainLayout);
    }

    /// <summary>
    /// Sistem bilgi paneli oluşturur
    /// </summary>
    private GroupBox CreateSystemInfoPanel()
    {
        var panel = new GroupBox
        {
            Text = "Sistem Bilgileri",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10)
        };

        // Bilgi labelları
        layout.Controls.Add(CreateInfoLabel("lblCores", "Çekirdek: -"));
        layout.Controls.Add(CreateInfoLabel("lblThreads", "Thread: -"));
        layout.Controls.Add(CreateInfoLabel("lblSockets", "Socket: -"));
        layout.Controls.Add(CreateInfoLabel("lblNumaNodes", "NUMA Node: -"));
        layout.Controls.Add(CreateInfoLabel("lblCpuUsage", "CPU: -%"));
        layout.Controls.Add(CreateInfoLabel("lblMemory", "RAM: - MB"));

        panel.Controls.Add(layout);
        return panel;
    }

    /// <summary>
    /// Process listesi paneli oluşturur
    /// </summary>
    private GroupBox CreateProcessPanel()
    {
        var panel = new GroupBox
        {
            Text = "Çalışan Process'ler",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var listView = new ListView
        {
            Name = "lvProcesses",
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false,
            Font = new Font("Consolas", 8.5F)
        };

        // Kolonlar
        listView.Columns.Add("PID", 60);
        listView.Columns.Add("Process Adı", 150);
        listView.Columns.Add("Pencere Başlığı", 200);
        listView.Columns.Add("Kullanıcı", 160);
        listView.Columns.Add("Oturum", 60);
        listView.Columns.Add("RAM (MB)", 80);
        listView.Columns.Add("Mevcut Affinity", 100);

        panel.Controls.Add(listView);
        return panel;
    }

    /// <summary>
    /// NUMA kontrol paneli oluşturur
    /// </summary>
    private GroupBox CreateNumaControlPanel()
    {
        var panel = new GroupBox
        {
            Text = "NUMA Node Kontrolü",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        // NUMA node seçimi
        var lblNode = new Label { Text = "NUMA Node Seçin:", Dock = DockStyle.Fill };
        var cmbNodes = new ComboBox 
        { 
            Name = "cmbNumaNodes", 
            Dock = DockStyle.Fill, 
            DropDownStyle = ComboBoxStyle.DropDownList 
        };

        // CPU seçimi
        var lblCpu = new Label { Text = "CPU Çekirdekleri:", Dock = DockStyle.Fill };
        var chkListCpus = new CheckedListBox 
        { 
            Name = "chkListCpus", 
            Dock = DockStyle.Fill,
            CheckOnClick = true
        };

        layout.Controls.Add(lblNode, 0, 0);
        layout.Controls.Add(cmbNodes, 0, 1);
        layout.Controls.Add(lblCpu, 0, 2);
        layout.Controls.Add(chkListCpus, 0, 3);

        panel.Controls.Add(layout);
        return panel;
    }

    /// <summary>
    /// Alt kontrol paneli oluşturur
    /// </summary>
    private GroupBox CreateControlPanel()
    {
        var panel = new GroupBox
        {
            Text = "İşlemler",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };

        // Butonlar
        var btnRefresh = new Button 
        { 
            Name = "btnRefresh", 
            Text = "Yenile", 
            Size = new Size(80, 30),
            UseVisualStyleBackColor = true
        };
        btnRefresh.Click += BtnRefresh_Click;

        var btnApply = new Button 
        { 
            Name = "btnApply", 
            Text = "Affinity Uygula", 
            Size = new Size(120, 30),
            UseVisualStyleBackColor = true,
            BackColor = Color.LightGreen
        };
        btnApply.Click += BtnApply_Click;

        var btnReset = new Button 
        { 
            Name = "btnReset", 
            Text = "Sıfırla", 
            Size = new Size(80, 30),
            UseVisualStyleBackColor = true
        };
        btnReset.Click += BtnReset_Click;

        var chkAutoRefresh = new CheckBox 
        { 
            Name = "chkAutoRefresh", 
            Text = "Otomatik Yenile (10s)", 
            Size = new Size(150, 30),
            Checked = false
        };
        chkAutoRefresh.CheckedChanged += ChkAutoRefresh_CheckedChanged;

        layout.Controls.AddRange(new Control[] { btnRefresh, btnApply, btnReset, chkAutoRefresh });
        panel.Controls.Add(layout);
        return panel;
    }

    /// <summary>
    /// Bilgi etiketi oluşturur
    /// </summary>
    private Label CreateInfoLabel(string name, string text)
    {
        return new Label
        {
            Name = name,
            Text = text,
            Size = new Size(120, 25),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.DarkBlue,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(5)
        };
    }

    /// <summary>
    /// Sistem bilgilerini yükler
    /// </summary>
    private void LoadSystemInfo()
    {
        _processorInfo = NumaService.GetProcessorInfo();
        
        // Sistem bilgilerini güncelle
        UpdateSystemLabels();
        
        // NUMA node combo'yu doldur
        var cmbNodes = this.Controls.Find("cmbNumaNodes", true).FirstOrDefault() as ComboBox;
        if (cmbNodes != null)
        {
            cmbNodes.Items.Clear();
            foreach (var node in _processorInfo.Nodes)
            {
                cmbNodes.Items.Add($"Node {node.NodeId} (CPU: {string.Join(",", node.ProcessorIds)})");
            }
            if (cmbNodes.Items.Count > 0) cmbNodes.SelectedIndex = 0;
            cmbNodes.SelectedIndexChanged += CmbNodes_SelectedIndexChanged;
        }
    }

    /// <summary>
    /// Timer'ı başlatır
    /// </summary>
    private void SetupTimer()
    {
        _refreshTimer = new System.Windows.Forms.Timer();
        _refreshTimer.Interval = 10000; // 10 saniye
        _refreshTimer.Tick += RefreshTimer_Tick;
        _refreshTimer.Start();

        // Uygulama başlarken tüm kuralları mevcut süreçlere uygula
        try { NumaService.ApplyAllRulesToRunningProcesses(); } catch {}
    }

    /// <summary>
    /// Sistem etiketlerini günceller
    /// </summary>
    private void UpdateSystemLabels()
    {
        var perfCounters = NumaService.GetPerformanceCounters();
        
        UpdateLabel("lblCores", $"Çekirdek: {_processorInfo.TotalCores}");
        UpdateLabel("lblThreads", $"Thread: {_processorInfo.LogicalProcessors}");
        UpdateLabel("lblSockets", $"Socket: {_processorInfo.PhysicalProcessors}");
        UpdateLabel("lblNumaNodes", $"NUMA Node: {_processorInfo.NumaNodes}");
        UpdateLabel("lblCpuUsage", $"CPU: {perfCounters.GetValueOrDefault("CPU_Usage", 0):F1}%");
        UpdateLabel("lblMemory", $"RAM: {perfCounters.GetValueOrDefault("Available_Memory_MB", 0):F0} MB");
    }

    /// <summary>
    /// Label günceller
    /// </summary>
    private void UpdateLabel(string name, string text)
    {
        var label = this.Controls.Find(name, true).FirstOrDefault() as Label;
        if (label != null) label.Text = text;
    }

    /// <summary>
    /// Process listesini günceller
    /// </summary>
    private void RefreshProcessList()
    {
        _processes = NumaService.GetRunningProcesses();
        
        var listView = this.Controls.Find("lvProcesses", true).FirstOrDefault() as ListView;
        if (listView != null)
        {
            listView.Items.Clear();
            
            foreach (var process in _processes.Take(100)) // İlk 100 process
            {
                var item = new ListViewItem(process.ProcessId.ToString());
                item.SubItems.Add(process.ProcessName);
                item.SubItems.Add(process.WindowTitle);
                item.SubItems.Add(string.IsNullOrWhiteSpace(process.UserName) ? "" : process.UserName);
                item.SubItems.Add(process.SessionId.ToString());
                item.SubItems.Add(process.WorkingSetMB.ToString());
                item.SubItems.Add(process.CurrentAffinityMask);
                item.Tag = process;
                
                listView.Items.Add(item);
            }
        }
    }

    // Event Handlers
    private void BtnRefresh_Click(object sender, EventArgs e)
    {
        LoadSystemInfo();
        RefreshProcessList();
    }

    private void BtnApply_Click(object sender, EventArgs e)
    {
        ApplyAffinityToSelectedProcess();
    }

    private void BtnReset_Click(object sender, EventArgs e)
    {
        ResetSelectedProcessAffinity();
    }

    private void ChkAutoRefresh_CheckedChanged(object sender, EventArgs e)
    {
        var chk = sender as CheckBox;
        if (chk != null)
        {
            _refreshTimer.Enabled = chk.Checked;
        }
    }

    private void RefreshTimer_Tick(object sender, EventArgs e)
    {
        UpdateSystemLabels();
        // Periyodik olarak kuralları çalışan süreçlere uygula (arka plan izleme)
        try { NumaService.ApplyAllRulesToRunningProcesses(); } catch {}
        RefreshProcessList();
    }

    private void CmbNodes_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateCpuList();
    }

    /// <summary>
    /// CPU listesini günceller
    /// </summary>
    private void UpdateCpuList()
    {
        var cmbNodes = this.Controls.Find("cmbNumaNodes", true).FirstOrDefault() as ComboBox;
        var chkList = this.Controls.Find("chkListCpus", true).FirstOrDefault() as CheckedListBox;
        
        if (cmbNodes != null && chkList != null && cmbNodes.SelectedIndex >= 0)
        {
            var selectedNode = _processorInfo.Nodes[cmbNodes.SelectedIndex];
            
            chkList.Items.Clear();
            foreach (var cpuId in selectedNode.ProcessorIds)
            {
                chkList.Items.Add($"CPU {cpuId}", true);
            }
        }
    }

    /// <summary>
    /// Seçili process'e affinity uygular - Gelişmiş Dialog ile
    /// </summary>
    private void ApplyAffinityToSelectedProcess()
    {
        var listView = this.Controls.Find("lvProcesses", true).FirstOrDefault() as ListView;
        var chkList = this.Controls.Find("chkListCpus", true).FirstOrDefault() as CheckedListBox;
        
        if (listView?.SelectedItems.Count > 0)
        {
            var selectedProcess = listView.SelectedItems[0].Tag as NumaService.ProcessInfo;
            if (selectedProcess != null)
            {
                // Mevcut seçili CPU'ları al
                var selectedCpus = new List<int>();
                if (chkList != null)
                {
                    for (int i = 0; i < chkList.Items.Count; i++)
                    {
                        if (chkList.GetItemChecked(i))
                        {
                            var cpuText = chkList.Items[i]?.ToString() ?? "";
                            if (int.TryParse(cpuText.Replace("CPU ", ""), out int cpuId))
                            {
                                selectedCpus.Add(cpuId);
                            }
                        }
                    }
                }

                // Eğer hiç CPU seçilmemişse varsayılan öner
                if (selectedCpus.Count == 0)
                {
                    // İlk NUMA node'daki ilk 4 CPU'yu varsayılan olarak seç
                    if (_processorInfo.Nodes.Count > 0)
                    {
                        selectedCpus.AddRange(_processorInfo.Nodes[0].ProcessorIds.Take(4));
                    }
                }

                // Gelişmiş affinity dialog'unu aç
                using (var dialog = new AffinityDialog(selectedProcess, selectedCpus, _processorInfo))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        var finalSelectedCpus = dialog.SelectedCpus;
                        var applyPermanently = dialog.ApplyPermanently;

                        if (finalSelectedCpus.Count > 0)
                        {
                            // Affinity uygula
                            var affinityMask = NumaService.CalculateAffinityMask(finalSelectedCpus);
                            var success = NumaService.SetProcessAffinity(selectedProcess.ProcessId, affinityMask);
                            
                            // Kalıcı kaydetme
                            if (success && applyPermanently)
                            {
                                SavePermanentAffinity(selectedProcess.ProcessName, affinityMask);
                            }

                            // Sonuç mesajı
                            var permanentText = applyPermanently ? " (Kalıcı olarak kaydedildi)" : " (Sadece bu oturum için)";
                            var message = success 
                                ? $"✅ Process {selectedProcess.ProcessName} (PID: {selectedProcess.ProcessId}) affinity başarıyla uygulandı!\n\n" +
                                  $"🎯 CPU'lar: {string.Join(", ", finalSelectedCpus.OrderBy(x => x))}\n" +
                                  $"🔧 Affinity Mask: {NumaService.CalculateAffinityMaskString(finalSelectedCpus)}\n" +
                                  $"💾 Durum: {permanentText}"
                                : $"❌ Affinity uygulanamadı!\n\nSebep: Process'e erişim izni olmayabilir veya process sonlanmış olabilir.";
                            
                            MessageBox.Show(message, "NUMA Affinity Sonucu", 
                                MessageBoxButtons.OK, 
                                success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                            
                            if (success) 
                            {
                                RefreshProcessList();
                                // UI'daki seçimi de güncelle
                                UpdateUISelection(finalSelectedCpus);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            MessageBox.Show("⚠️ Lütfen listeden bir process seçin!\n\nProcess'e çift tıklayarak seçebilirsiniz.", 
                "Process Seçimi Gerekli", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Kalıcı affinity ayarını registry'ye kaydeder
    /// </summary>
    private void SavePermanentAffinity(string processName, IntPtr affinityMask)
    {
        try
        {
            // Hem HKLM (tüm kullanıcılar) hem HKCU (mevcut kullanıcı) altına yaz
            void WriteTo(Microsoft.Win32.RegistryKey root)
            {
                using var key = root.CreateSubKey(@"SOFTWARE\LabsOffice\NumaManager\ProcessAffinity");
                key.SetValue(processName, affinityMask.ToInt64().ToString("X"));
            }

            try { WriteTo(Microsoft.Win32.Registry.LocalMachine); } catch { }
            try { WriteTo(Microsoft.Win32.Registry.CurrentUser); } catch { }

            // Startup script oluştur (opsiyonel)
            CreateStartupScript(processName, affinityMask);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"⚠️ Kalıcı ayar kaydedilemedi:\n{ex.Message}", "Registry Hatası", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Startup script oluşturur
    /// </summary>
    private void CreateStartupScript(string processName, IntPtr affinityMask)
    {
        try
        {
            var scriptPath = Path.Combine(Application.StartupPath, $"affinity_{processName}.bat");
            var scriptContent = $@"@echo off
REM Auto-generated NUMA affinity script for {processName}
REM Generated by LabsOffice NUMA Manager

timeout /t 10 /nobreak >nul
powershell -Command ""Get-Process '{processName}' -ErrorAction SilentlyContinue | ForEach-Object {{ $_.ProcessorAffinity = [System.IntPtr]{affinityMask.ToInt64()} }}""
echo NUMA affinity applied to {processName}: {affinityMask.ToInt64():X}
";
            
            File.WriteAllText(scriptPath, scriptContent);
        }
        catch (Exception ex)
        {
            // Sessizce hata yut - kritik değil
            Console.WriteLine($"Startup script oluşturulamadı: {ex.Message}");
        }
    }

    /// <summary>
    /// UI'daki CPU seçimini günceller
    /// </summary>
    private void UpdateUISelection(List<int> selectedCpus)
    {
        var chkList = this.Controls.Find("chkListCpus", true).FirstOrDefault() as CheckedListBox;
        if (chkList != null)
        {
            for (int i = 0; i < chkList.Items.Count; i++)
            {
                var cpuText = chkList.Items[i]?.ToString() ?? "";
                if (int.TryParse(cpuText.Replace("CPU ", ""), out int cpuId))
                {
                    chkList.SetItemChecked(i, selectedCpus.Contains(cpuId));
                }
            }
        }
    }

    /// <summary>
    /// Seçili process'in affinity'sini sıfırlar
    /// </summary>
    private void ResetSelectedProcessAffinity()
    {
        var listView = this.Controls.Find("lvProcesses", true).FirstOrDefault() as ListView;
        
        if (listView?.SelectedItems.Count > 0)
        {
            var selectedProcess = listView.SelectedItems[0].Tag as NumaService.ProcessInfo;
            if (selectedProcess != null)
            {
                // Tüm CPU'lara affinity ver
                var allCpuMask = new IntPtr((1L << _processorInfo.LogicalProcessors) - 1);
                var success = NumaService.SetProcessAffinity(selectedProcess.ProcessId, allCpuMask);
                
                var message = success 
                    ? $"Process {selectedProcess.ProcessName} affinity sıfırlandı (tüm CPU'lar)"
                    : "Affinity sıfırlanamadı!";
                
                MessageBox.Show(message, "NUMA Reset", MessageBoxButtons.OK, 
                    success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                
                if (success) RefreshProcessList();
            }
        }
        else
        {
            MessageBox.Show("Lütfen bir process seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Tooltip'leri ayarlar
    /// </summary>
    private void SetupTooltips()
    {
        // Butonlara tooltip ekle
        var btnRefresh = this.Controls.Find("btnRefresh", true).FirstOrDefault() as Button;
        if (btnRefresh != null)
            TooltipHelper.SetTooltip(btnRefresh, TooltipTexts.REFRESH_BUTTON, "🔄 Yenile Butonu");

        var btnApply = this.Controls.Find("btnApply", true).FirstOrDefault() as Button;
        if (btnApply != null)
            TooltipHelper.SetTooltip(btnApply, TooltipTexts.APPLY_AFFINITY_BUTTON, "🎯 Affinity Uygula");

        var btnReset = this.Controls.Find("btnReset", true).FirstOrDefault() as Button;
        if (btnReset != null)
            TooltipHelper.SetTooltip(btnReset, TooltipTexts.RESET_BUTTON, "🔄 Sıfırla");

        var chkAutoRefresh = this.Controls.Find("chkAutoRefresh", true).FirstOrDefault() as CheckBox;
        if (chkAutoRefresh != null)
            TooltipHelper.SetTooltip(chkAutoRefresh, TooltipTexts.AUTO_REFRESH_CHECKBOX, "⏰ Otomatik Yenileme");

        // NUMA kontrollerine tooltip ekle
        var cmbNodes = this.Controls.Find("cmbNumaNodes", true).FirstOrDefault() as ComboBox;
        if (cmbNodes != null)
            TooltipHelper.SetTooltip(cmbNodes, TooltipTexts.NUMA_NODE_COMBO, "🗺️ NUMA Node Seçimi");

        var chkListCpus = this.Controls.Find("chkListCpus", true).FirstOrDefault() as CheckedListBox;
        if (chkListCpus != null)
            TooltipHelper.SetTooltip(chkListCpus, TooltipTexts.CPU_CHECKLIST, "🖥️ CPU Seçimi");

        // Process listesine tooltip ekle
        var lvProcesses = this.Controls.Find("lvProcesses", true).FirstOrDefault() as ListView;
        if (lvProcesses != null)
            TooltipHelper.SetTooltip(lvProcesses, TooltipTexts.PROCESS_LIST, "📋 Process Listesi");

        // Sistem bilgi labellarına tooltip ekle
        var lblCores = this.Controls.Find("lblCores", true).FirstOrDefault() as Label;
        if (lblCores != null)
            TooltipHelper.SetTooltip(lblCores, "💻 Sistem çekirdek sayısı\n\nFiziksel CPU çekirdeklerinin toplam sayısı", "Çekirdek Sayısı");

        var lblThreads = this.Controls.Find("lblThreads", true).FirstOrDefault() as Label;
        if (lblThreads != null)
            TooltipHelper.SetTooltip(lblThreads, "🧵 Mantıksal işlemci sayısı\n\nHyperthreading dahil toplam thread sayısı", "Thread Sayısı");

        var lblSockets = this.Controls.Find("lblSockets", true).FirstOrDefault() as Label;
        if (lblSockets != null)
            TooltipHelper.SetTooltip(lblSockets, "🔌 Fiziksel CPU socket sayısı\n\nSistemdeki fiziksel CPU paketlerinin sayısı", "Socket Sayısı");

        var lblNumaNodes = this.Controls.Find("lblNumaNodes", true).FirstOrDefault() as Label;
        if (lblNumaNodes != null)
            TooltipHelper.SetTooltip(lblNumaNodes, "🗺️ NUMA node sayısı\n\nBellek ve CPU'ların gruplandığı node sayısı", "NUMA Node Sayısı");

        var lblCpuUsage = this.Controls.Find("lblCpuUsage", true).FirstOrDefault() as Label;
        if (lblCpuUsage != null)
            TooltipHelper.SetTooltip(lblCpuUsage, "📊 Anlık CPU kullanım oranı\n\nSistem geneli CPU kullanım yüzdesi", "CPU Kullanımı");

        var lblMemory = this.Controls.Find("lblMemory", true).FirstOrDefault() as Label;
        if (lblMemory != null)
            TooltipHelper.SetTooltip(lblMemory, "💾 Kullanılabilir bellek miktarı\n\nSistemde kullanılabilir RAM miktarı (MB)", "Bellek Durumu");
    }

    /// <summary>
    /// Form kapanırken cleanup
    /// </summary>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        
        // Tooltip'leri temizle
        TooltipHelper.ClearFormTooltips(this);
        
        base.OnFormClosed(e);
    }
}
