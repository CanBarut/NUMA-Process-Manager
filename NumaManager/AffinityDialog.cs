using System.Diagnostics;
using Microsoft.Win32;

namespace NumaManager;

/// <summary>
/// Affinity ayarları için gelişmiş dialog
/// </summary>
public partial class AffinityDialog : Form
{
    private NumaService.ProcessInfo _processInfo;
    private List<int> _selectedCpus;
    private NumaService.ProcessorInfo _systemInfo;
    
    public bool ApplyPermanently { get; private set; }
    public List<int> SelectedCpus => _selectedCpus;

    public AffinityDialog(NumaService.ProcessInfo processInfo, List<int> selectedCpus, NumaService.ProcessorInfo systemInfo)
    {
        _processInfo = processInfo;
        _selectedCpus = new List<int>(selectedCpus);
        _systemInfo = systemInfo;
        
        InitializeComponent();
        SetupDialog();
        AnalyzeAffinitySource();
        AnalyzeAndSuggest();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(900, 650); // Daha büyük boyut
        this.Text = $"NUMA Affinity Manager - {_processInfo.ProcessName}";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable; // Yeniden boyutlandırılabilir
        this.MaximizeBox = true;
        this.MinimizeBox = true;
        this.Font = new Font("Segoe UI", 9F);
    }

    private void SetupDialog()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(15)
        };

        // Row styles - optimize edilmiş
        mainLayout.RowCount = 7; // 7 satır
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));  // Process info
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F)); // Current status
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));  // Affinity source analysis
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));   // Suggestions
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));   // CPU selection - daha büyük
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));  // Options
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // Buttons

        // Process bilgileri
        var processPanel = CreateProcessInfoPanel();
        mainLayout.Controls.Add(processPanel, 0, 0);

        // Mevcut durum
        var statusPanel = CreateCurrentStatusPanel();
        mainLayout.Controls.Add(statusPanel, 0, 1);

        // Affinity kaynak analizi - YENİ
        var sourcePanel = CreateAffinitySourcePanel();
        mainLayout.Controls.Add(sourcePanel, 0, 2);

        // Öneriler
        var suggestionsPanel = CreateSuggestionsPanel();
        mainLayout.Controls.Add(suggestionsPanel, 0, 3);

        // CPU seçimi
        var cpuPanel = CreateCpuSelectionPanel();
        mainLayout.Controls.Add(cpuPanel, 0, 4);

        // Seçenekler
        var optionsPanel = CreateOptionsPanel();
        mainLayout.Controls.Add(optionsPanel, 0, 5);

        // Butonlar
        var buttonPanel = CreateButtonPanel();
        mainLayout.Controls.Add(buttonPanel, 0, 6);

        this.Controls.Add(mainLayout);
    }

    private GroupBox CreateProcessInfoPanel()
    {
        var panel = new GroupBox
        {
            Text = "Process Bilgileri",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };

        var lblProcess = new Label
        {
            Text = $"Process: {_processInfo.ProcessName} (PID: {_processInfo.ProcessId})",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.DarkBlue,
            AutoSize = true
        };

        var lblMemory = new Label
        {
            Text = $"RAM: {_processInfo.WorkingSetMB} MB",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.DarkGreen,
            AutoSize = true,
            Margin = new Padding(20, 0, 0, 0)
        };

        layout.Controls.AddRange(new Control[] { lblProcess, lblMemory });
        panel.Controls.Add(layout);
        return panel;
    }

    private GroupBox CreateCurrentStatusPanel()
    {
        var panel = new GroupBox
        {
            Text = "Mevcut Affinity Durumu",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(10)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // Mevcut affinity
        var lblCurrentTitle = new Label { Text = "Mevcut Mask:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Dock = DockStyle.Fill };
        var lblCurrentValue = new Label 
        { 
            Text = _processInfo.CurrentAffinityMask, 
            Font = new Font("Consolas", 9F), 
            ForeColor = Color.Red,
            Dock = DockStyle.Fill 
        };

        // Yeni affinity
        var lblNewTitle = new Label { Text = "Yeni Mask:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Dock = DockStyle.Fill };
        var lblNewValue = new Label 
        { 
            Name = "lblNewAffinity",
            Text = CalculateAffinityMaskString(_selectedCpus), 
            Font = new Font("Consolas", 9F), 
            ForeColor = Color.Green,
            Dock = DockStyle.Fill 
        };

        // CPU'lar
        var lblCpusTitle = new Label { Text = "Seçili CPU'lar:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Dock = DockStyle.Fill };
        var lblCpusValue = new Label 
        { 
            Name = "lblSelectedCpus",
            Text = string.Join(", ", _selectedCpus), 
            Font = new Font("Segoe UI", 9F), 
            ForeColor = Color.Blue,
            Dock = DockStyle.Fill 
        };

        layout.Controls.Add(lblCurrentTitle, 0, 0);
        layout.Controls.Add(lblCurrentValue, 1, 0);
        layout.Controls.Add(lblNewTitle, 0, 1);
        layout.Controls.Add(lblNewValue, 1, 1);
        layout.Controls.Add(lblCpusTitle, 0, 2);
        layout.Controls.Add(lblCpusValue, 1, 2);

        panel.Controls.Add(layout);
        return panel;
    }

    /// <summary>
    /// Affinity kaynak analizi paneli oluşturur
    /// </summary>
    private GroupBox CreateAffinitySourcePanel()
    {
        var panel = new GroupBox
        {
            Text = "🔍 Affinity Kaynağı Analizi",
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

        var lblSource = new Label
        {
            Name = "lblAffinitySource",
            Text = "Analiz ediliyor...",
            Font = new Font("Segoe UI", 9F),
            AutoSize = true,
            ForeColor = Color.DarkBlue
        };

        var lblRecommendation = new Label
        {
            Name = "lblAffinityRecommendation", 
            Text = "",
            Font = new Font("Segoe UI", 9F),
            AutoSize = true,
            ForeColor = Color.DarkGreen,
            Margin = new Padding(20, 0, 0, 0)
        };

        layout.Controls.AddRange(new Control[] { lblSource, lblRecommendation });
        panel.Controls.Add(layout);
        return panel;
    }

    private GroupBox CreateSuggestionsPanel()
    {
        var panel = new GroupBox
        {
            Text = "Akıllı Öneriler",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var textBox = new RichTextBox
        {
            Name = "rtbSuggestions",
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.LightYellow,
            Margin = new Padding(10)
        };

        panel.Controls.Add(textBox);
        return panel;
    }

    private GroupBox CreateCpuSelectionPanel()
    {
        var panel = new GroupBox
        {
            Text = $"🖥️ CPU Çekirdekleri ({_systemInfo.LogicalProcessors} adet) - Tıklayarak seç/deseç",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F)); // Kontrol butonları
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // CPU grid
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F)); // Bilgi

        // Kontrol butonları
        var controlPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0)
        };

        var btnSelectAll = new Button
        {
            Text = "Tümünü Seç",
            Size = new Size(80, 25),
            Font = new Font("Segoe UI", 8F),
            UseVisualStyleBackColor = true
        };
        btnSelectAll.Click += (s, e) => SelectAllCpus(true);

        var btnDeselectAll = new Button
        {
            Text = "Hiçbirini Seçme",
            Size = new Size(90, 25),
            Font = new Font("Segoe UI", 8F),
            UseVisualStyleBackColor = true
        };
        btnDeselectAll.Click += (s, e) => SelectAllCpus(false);

        // CPU sayısı seçici
        var lblCpuCount = new Label
        {
            Text = "CPU Sayısı:",
            Font = new Font("Segoe UI", 8F),
            Size = new Size(60, 25),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var cmbCpuCount = new ComboBox
        {
            Name = "cmbCpuCount",
            Size = new Size(50, 25),
            Font = new Font("Segoe UI", 8F),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbCpuCount.Items.AddRange(new object[] { 2, 4, 8, 12, 16, 24, 32 });
        cmbCpuCount.SelectedItem = 4;

        var btnSelectFirst = new Button
        {
            Text = "İlk N CPU",
            Size = new Size(70, 25),
            Font = new Font("Segoe UI", 8F),
            BackColor = Color.LightBlue,
            UseVisualStyleBackColor = false
        };
        btnSelectFirst.Click += (s, e) => {
            var count = (int)(cmbCpuCount.SelectedItem ?? 4);
            SelectCpuRangeWithWarning(0, count);
        };

        var btnSelectLast = new Button
        {
            Text = "Son N CPU", 
            Size = new Size(70, 25),
            Font = new Font("Segoe UI", 8F),
            BackColor = Color.LightGreen,
            UseVisualStyleBackColor = false
        };
        btnSelectLast.Click += (s, e) => {
            var count = (int)(cmbCpuCount.SelectedItem ?? 4);
            SelectCpuRangeWithWarning(_systemInfo.LogicalProcessors - count, count);
        };

        var btnSelectMiddle = new Button
        {
            Text = "Orta N CPU",
            Size = new Size(70, 25),
            Font = new Font("Segoe UI", 8F),
            BackColor = Color.LightYellow,
            UseVisualStyleBackColor = false
        };
        btnSelectMiddle.Click += (s, e) => {
            var count = (int)(cmbCpuCount.SelectedItem ?? 4);
            var start = (_systemInfo.LogicalProcessors - count) / 2;
            SelectCpuRangeWithWarning(start, count);
        };

        controlPanel.Controls.AddRange(new Control[] { btnSelectAll, btnDeselectAll, lblCpuCount, cmbCpuCount, btnSelectFirst, btnSelectLast, btnSelectMiddle });
        mainLayout.Controls.Add(controlPanel, 0, 0);

        // CPU Grid - daha büyük butonlarla
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        var flowLayout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            Padding = new Padding(5)
        };

        // CPU butonları oluştur - daha büyük ve renkli
        for (int i = 0; i < _systemInfo.LogicalProcessors; i++)
        {
            var cpuButton = new Button
            {
                Name = $"btnCpu{i}",
                Text = $"CPU\n{i}",
                Size = new Size(45, 35), // Daha büyük butonlar
                Tag = i,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Margin = new Padding(1),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // NUMA node'a göre renk kodlama
            var nodeColor = GetNumaNodeColor(i);
            cpuButton.BackColor = _selectedCpus.Contains(i) ? Color.LimeGreen : nodeColor;
            cpuButton.ForeColor = _selectedCpus.Contains(i) ? Color.White : Color.Black;
            
            cpuButton.Click += CpuButton_Click;
            flowLayout.Controls.Add(cpuButton);
        }

        scrollPanel.Controls.Add(flowLayout);
        mainLayout.Controls.Add(scrollPanel, 0, 1);

        // Bilgi etiketi
        var lblInfo = new Label
        {
            Name = "lblCpuInfo",
            Text = $"💡 Seçili: {_selectedCpus.Count} CPU | Toplam: {_systemInfo.LogicalProcessors} CPU",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleLeft
        };
        mainLayout.Controls.Add(lblInfo, 0, 2);

        panel.Controls.Add(mainLayout);
        return panel;
    }

    /// <summary>
    /// NUMA node'a göre CPU rengi döner
    /// </summary>
    private Color GetNumaNodeColor(int cpuId)
    {
        foreach (var node in _systemInfo.Nodes)
        {
            if (node.ProcessorIds.Contains(cpuId))
            {
                return node.NodeId switch
                {
                    0 => Color.LightCyan,
                    1 => Color.LightPink,
                    2 => Color.LightGoldenrodYellow,
                    3 => Color.LightSalmon,
                    _ => Color.LightGray
                };
            }
        }
        return Color.LightGray;
    }

    /// <summary>
    /// Tüm CPU'ları seç/deseç
    /// </summary>
    private void SelectAllCpus(bool select)
    {
        _selectedCpus.Clear();
        if (select)
        {
            for (int i = 0; i < _systemInfo.LogicalProcessors; i++)
            {
                _selectedCpus.Add(i);
            }
        }
        UpdateAllCpuButtons();
        UpdateAffinityDisplay();
    }

    /// <summary>
    /// Belirli aralıktaki CPU'ları seçer
    /// </summary>
    private void SelectCpuRange(int start, int count)
    {
        _selectedCpus.Clear();
        for (int i = start; i < Math.Min(start + count, _systemInfo.LogicalProcessors); i++)
        {
            _selectedCpus.Add(i);
        }
        UpdateAllCpuButtons();
        UpdateAffinityDisplay();
    }

    /// <summary>
    /// Core birleşme uyarısı ile CPU aralığı seçer
    /// </summary>
    private void SelectCpuRangeWithWarning(int start, int count)
    {
        var selectedCpus = new List<int>();
        for (int i = start; i < Math.Min(start + count, _systemInfo.LogicalProcessors); i++)
        {
            selectedCpus.Add(i);
        }

        // Core birleşme kontrolü
        var coreCollisionInfo = CheckCoreCollisions(selectedCpus);
        
        if (coreCollisionInfo.HasCollisions)
        {
            var warningMessage = $"⚠️ CORE BİRLEŞME UYARISI!\n\n" +
                $"Seçtiğiniz CPU'larda core birleşmesi var:\n" +
                $"• Birleşen Core'lar: {string.Join(", ", coreCollisionInfo.CollidingCores)}\n" +
                $"• Etkilenen CPU'lar: {string.Join(", ", coreCollisionInfo.CollidingCpus)}\n\n" +
                $"Bu durum performans düşüklüğüne neden olabilir.\n" +
                $"Hyperthreading çakışması riski vardır.\n\n" +
                $"Yine de devam etmek istiyor musunuz?";

            var result = MessageBox.Show(warningMessage, "Core Birleşme Riski", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                
            if (result == DialogResult.No)
            {
                return; // İptal et
            }
        }

        _selectedCpus = selectedCpus;
        UpdateAllCpuButtons();
        UpdateAffinityDisplay();
    }

    /// <summary>
    /// Core birleşme kontrolü yapar
    /// </summary>
    private (bool HasCollisions, List<int> CollidingCores, List<int> CollidingCpus) CheckCoreCollisions(List<int> cpuIds)
    {
        var collidingCores = new List<int>();
        var collidingCpus = new List<int>();
        
        // CPU'ları core'lara göre grupla (her core 2 thread'e sahip varsayımı)
        var coreGroups = cpuIds.GroupBy(cpu => cpu / 2).ToList();
        
        foreach (var coreGroup in coreGroups)
        {
            var coreId = coreGroup.Key;
            var cpusInCore = coreGroup.ToList();
            
            // Aynı core'da birden fazla CPU seçilmişse birleşme var
            if (cpusInCore.Count > 1)
            {
                collidingCores.Add(coreId);
                collidingCpus.AddRange(cpusInCore);
            }
        }
        
        return (collidingCores.Count > 0, collidingCores, collidingCpus);
    }

    /// <summary>
    /// Tüm CPU butonlarını günceller
    /// </summary>
    private void UpdateAllCpuButtons()
    {
        for (int i = 0; i < _systemInfo.LogicalProcessors; i++)
        {
            var button = this.Controls.Find($"btnCpu{i}", true).FirstOrDefault() as Button;
            if (button != null)
            {
                UpdateCpuButtonState(button, _selectedCpus.Contains(i));
            }
        }
        
        // Bilgi etiketini güncelle
        var lblInfo = this.Controls.Find("lblCpuInfo", true).FirstOrDefault() as Label;
        if (lblInfo != null)
        {
            lblInfo.Text = $"💡 Seçili: {_selectedCpus.Count} CPU | Toplam: {_systemInfo.LogicalProcessors} CPU";
        }
    }

    private GroupBox CreateOptionsPanel()
    {
        var panel = new GroupBox
        {
            Text = "Uygulama Seçenekleri",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(10)
        };

        var chkPermanent = new CheckBox
        {
            Name = "chkPermanent",
            Text = "Kalıcı olarak kaydet (Registry'ye yaz)",
            Font = new Font("Segoe UI", 9F),
            AutoSize = true,
            ForeColor = Color.DarkRed
        };

        var lblNote = new Label
        {
            Text = "⚠️ Kalıcı seçenek: Process her açıldığında bu ayarlar otomatik uygulanır",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.Gray,
            AutoSize = true
        };

        layout.Controls.AddRange(new Control[] { chkPermanent, lblNote });
        panel.Controls.Add(layout);
        return panel;
    }

    private Panel CreateButtonPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 50
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10)
        };

        var btnCancel = new Button
        {
            Text = "İptal",
            Size = new Size(80, 35),
            DialogResult = DialogResult.Cancel,
            UseVisualStyleBackColor = true
        };

        var btnApply = new Button
        {
            Text = "Affinity Uygula",
            Size = new Size(120, 35),
            DialogResult = DialogResult.OK,
            BackColor = Color.LightGreen,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var btnOptimal = new Button
        {
            Text = "Optimal Ayar",
            Size = new Size(100, 35),
            BackColor = Color.LightBlue,
            UseVisualStyleBackColor = false
        };
        btnOptimal.Click += BtnOptimal_Click;

        var btnProfessional = new Button
        {
            Text = "Profesyonel Mod",
            Size = new Size(120, 35),
            BackColor = Color.DarkSlateBlue,
            ForeColor = Color.White,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        btnProfessional.Click += BtnProfessional_Click;

        var btnRegistryManager = new Button
        {
            Text = "Registry Yönetimi",
            Size = new Size(130, 35),
            BackColor = Color.DarkRed,
            ForeColor = Color.White,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold)
        };
        btnRegistryManager.Click += BtnRegistryManager_Click;

        layout.Controls.AddRange(new Control[] { btnCancel, btnApply, btnOptimal, btnProfessional, btnRegistryManager });
        panel.Controls.Add(layout);
        return panel;
    }

    /// <summary>
    /// Affinity kaynağını analiz eder
    /// </summary>
    private void AnalyzeAffinitySource()
    {
        var lblSource = this.Controls.Find("lblAffinitySource", true).FirstOrDefault() as Label;
        var lblRecommendation = this.Controls.Find("lblAffinityRecommendation", true).FirstOrDefault() as Label;
        
        if (lblSource == null || lblRecommendation == null) return;

        var currentMask = _processInfo.CurrentAffinity.ToInt64();
        var currentCpuCount = CountSetBits(currentMask);
        var totalCpus = _systemInfo.LogicalProcessors;

        string source, recommendation;
        Color sourceColor, recommendationColor;

        if (currentMask == (1L << totalCpus) - 1)
        {
            // Tüm CPU'lar seçili
            source = "🟢 Microsoft Varsayılan (Tüm CPU'lar)";
            recommendation = "✅ İyi - Sistem otomatik yönetiyor";
            sourceColor = Color.DarkGreen;
            recommendationColor = Color.Green;
        }
        else if (currentCpuCount == 1)
        {
            // Tek CPU
            source = "🔴 Manuel Atama (Tek CPU)";
            recommendation = "⚠️ Çok kısıtlayıcı - Daha fazla CPU verin";
            sourceColor = Color.Red;
            recommendationColor = Color.Orange;
        }
        else if (IsPowerOfTwo(currentMask))
        {
            // 2'nin kuvveti (1,2,4,8,16...)
            source = $"🟡 Manuel/Otomatik ({currentCpuCount} CPU)";
            recommendation = currentCpuCount >= 4 ? "✅ Makul" : "⚠️ Daha fazla CPU ekleyin";
            sourceColor = Color.DarkGoldenrod;
            recommendationColor = currentCpuCount >= 4 ? Color.Green : Color.Orange;
        }
        else if (IsSequentialCpus(currentMask))
        {
            // Ardışık CPU'lar (0,1,2,3 gibi)
            source = $"🔵 Manuel NUMA Optimizasyonu ({currentCpuCount} CPU)";
            recommendation = "✅ NUMA-aware - İyi seçim";
            sourceColor = Color.Blue;
            recommendationColor = Color.Green;
        }
        else if (IsRandomPattern(currentMask))
        {
            // Rastgele desen
            source = $"🟠 Uygulama Kodu Ataması ({currentCpuCount} CPU)";
            recommendation = "🔧 Delphi/C++ kodunda SetProcessAffinityMask çağrısı var";
            sourceColor = Color.DarkOrange;
            recommendationColor = Color.Purple;
        }
        else
        {
            // Diğer
            source = $"❓ Bilinmeyen Kaynak ({currentCpuCount} CPU)";
            recommendation = "🔍 Manuel inceleme gerekli";
            sourceColor = Color.Gray;
            recommendationColor = Color.Gray;
        }

        lblSource.Text = source;
        lblSource.ForeColor = sourceColor;
        lblRecommendation.Text = recommendation;
        lblRecommendation.ForeColor = recommendationColor;
    }

    /// <summary>
    /// Sayının 2'nin kuvveti olup olmadığını kontrol eder
    /// </summary>
    private bool IsPowerOfTwo(long value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }

    /// <summary>
    /// CPU'ların ardışık olup olmadığını kontrol eder
    /// </summary>
    private bool IsSequentialCpus(long mask)
    {
        var binary = Convert.ToString(mask, 2);
        var firstOne = binary.IndexOf('1');
        var lastOne = binary.LastIndexOf('1');
        
        if (firstOne == -1) return false;
        
        var expectedLength = lastOne - firstOne + 1;
        var actualOnes = binary.Count(c => c == '1');
        
        return expectedLength == actualOnes;
    }

    /// <summary>
    /// Rastgele desen olup olmadığını kontrol eder
    /// </summary>
    private bool IsRandomPattern(long mask)
    {
        var binary = Convert.ToString(mask, 2);
        var ones = binary.Count(c => c == '1');
        
        // Eğer CPU'lar dağınık ve 2'den fazlaysa rastgele desendir
        return ones > 2 && !IsSequentialCpus(mask) && !IsPowerOfTwo(mask);
    }

    private void AnalyzeAndSuggest()
    {
        var rtb = this.Controls.Find("rtbSuggestions", true).FirstOrDefault() as RichTextBox;
        if (rtb == null) return;

        var suggestions = new List<string>();
        
        // Process türü analizi
        var processType = AnalyzeProcessType(_processInfo.ProcessName.ToLower());
        suggestions.Add($"🔍 Process Türü: {processType.Type}");
        suggestions.Add($"📊 Önerilen CPU Sayısı: {processType.RecommendedCpus}");
        suggestions.Add($"💡 Açıklama: {processType.Description}");
        suggestions.Add("");

        // Mevcut sistem analizi
        var currentMask = _processInfo.CurrentAffinity.ToInt64();
        var currentCpuCount = CountSetBits(currentMask);
        suggestions.Add($"📈 Mevcut Durumu:");
        suggestions.Add($"   • {currentCpuCount} CPU kullanıyor (Toplam {_systemInfo.LogicalProcessors} CPU'dan)");
        suggestions.Add($"   • Affinity Mask: {_processInfo.CurrentAffinityMask}");
        suggestions.Add("");

        // NUMA analizi
        var numaRecommendation = AnalyzeNumaRecommendation();
        suggestions.Add($"🎯 NUMA Önerisi:");
        suggestions.Add($"   • {numaRecommendation.RecommendedNode}. NUMA node kullanın");
        suggestions.Add($"   • CPU'lar: {string.Join(", ", numaRecommendation.RecommendedCpus)}");
        suggestions.Add($"   • Sebep: {numaRecommendation.Reason}");
        suggestions.Add("");

        // Performans tahmin
        var perfEstimate = EstimatePerformanceImpact();
        suggestions.Add($"⚡ Performans Tahmini:");
        suggestions.Add($"   • Beklenen İyileşme: {perfEstimate.ExpectedImprovement}");
        suggestions.Add($"   • Risk Seviyesi: {perfEstimate.RiskLevel}");
        suggestions.Add($"   • Önerilen Aksiyon: {perfEstimate.RecommendedAction}");

        rtb.Text = string.Join(Environment.NewLine, suggestions);
        
        // Renklendirme
        ColorizeText(rtb);
    }

    private (string Type, int RecommendedCpus, string Description) AnalyzeProcessType(string processName)
    {
        return processName switch
        {
            var name when name.Contains("capital") => ("Finansal Uygulama", 4, "Yoğun hesaplama ve veritabanı işlemleri için 4-8 CPU önerilir"),
            var name when name.Contains("labsmobile") => ("Web Uygulaması", 6, "ASP.NET Core uygulaması için 4-8 CPU optimal"),
            var name when name.Contains("chrome") => ("Web Tarayıcı", 2, "Tab başına 1-2 CPU yeterli"),
            var name when name.Contains("notepad") => ("Metin Editörü", 1, "Hafif uygulama, 1 CPU yeterli"),
            var name when name.Contains("sql") => ("Veritabanı", 8, "Veritabanı sunucuları için çok CPU gerekir"),
            var name when name.Contains("iis") => ("Web Sunucu", 6, "IIS için 4-8 CPU önerilir"),
            _ => ("Bilinmeyen", 2, "Genel amaçlı uygulama için 2-4 CPU")
        };
    }

    private (int RecommendedNode, List<int> RecommendedCpus, string Reason) AnalyzeNumaRecommendation()
    {
        // En az yüklü NUMA node'u bul (basit analiz)
        var bestNode = 0;
        var bestNodeCpus = _systemInfo.Nodes[bestNode].ProcessorIds;
        
        var reason = $"Node {bestNode} seçildi: Memory locality için aynı node'daki CPU'lar";
        
        return (bestNode, bestNodeCpus, reason);
    }

    private (string ExpectedImprovement, string RiskLevel, string RecommendedAction) EstimatePerformanceImpact()
    {
        var currentCpus = CountSetBits(_processInfo.CurrentAffinity.ToInt64());
        var newCpus = _selectedCpus.Count;
        
        if (newCpus < currentCpus)
        {
            return ("CPU sayısı azalacak, performans düşebilir", "YÜKSEK", "Dikkatli olun!");
        }
        else if (newCpus == currentCpus)
        {
            return ("CPU sayısı aynı, NUMA locality iyileşecek", "DÜŞÜK", "Güvenli değişiklik");
        }
        else
        {
            var improvement = ((double)(newCpus - currentCpus) / currentCpus * 100).ToString("F0");
            return ($"~%{improvement} performans artışı beklenir", "DÜŞÜK", "Önerilen değişiklik");
        }
    }

    private void ColorizeText(RichTextBox rtb)
    {
        // Emoji'leri ve önemli kısımları renklendir
        var lines = rtb.Text.Split('\n');
        rtb.Clear();
        
        foreach (var line in lines)
        {
            if (line.StartsWith("🔍") || line.StartsWith("📊") || line.StartsWith("💡"))
            {
                rtb.SelectionColor = Color.Blue;
            }
            else if (line.StartsWith("📈"))
            {
                rtb.SelectionColor = Color.DarkGreen;
            }
            else if (line.StartsWith("🎯"))
            {
                rtb.SelectionColor = Color.Purple;
            }
            else if (line.StartsWith("⚡"))
            {
                rtb.SelectionColor = Color.Red;
            }
            else
            {
                rtb.SelectionColor = Color.Black;
            }
            
            rtb.AppendText(line + Environment.NewLine);
        }
    }

    private void CpuButton_Click(object sender, EventArgs e)
    {
        var button = sender as Button;
        var cpuId = (int)button.Tag;
        
        if (_selectedCpus.Contains(cpuId))
        {
            _selectedCpus.Remove(cpuId);
        }
        else
        {
            _selectedCpus.Add(cpuId);
        }
        
        UpdateCpuButtonState(button, _selectedCpus.Contains(cpuId));
        UpdateAffinityDisplay();
    }

    private void UpdateCpuButtonState(Button button, bool selected)
    {
        var cpuId = (int)button.Tag;
        var nodeColor = GetNumaNodeColor(cpuId);
        
        if (selected)
        {
            button.BackColor = Color.LimeGreen;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        }
        else
        {
            button.BackColor = nodeColor;
            button.ForeColor = Color.Black;
            button.Font = new Font("Segoe UI", 7.5F);
        }
    }

    private void UpdateAffinityDisplay()
    {
        var lblNewAffinity = this.Controls.Find("lblNewAffinity", true).FirstOrDefault() as Label;
        var lblSelectedCpus = this.Controls.Find("lblSelectedCpus", true).FirstOrDefault() as Label;
        
        if (lblNewAffinity != null)
            lblNewAffinity.Text = CalculateAffinityMaskString(_selectedCpus);
            
        if (lblSelectedCpus != null)
            lblSelectedCpus.Text = string.Join(", ", _selectedCpus.OrderBy(x => x));
    }

    private void BtnOptimal_Click(object sender, EventArgs e)
    {
        // Optimal ayarı uygula
        var recommendation = AnalyzeNumaRecommendation();
        _selectedCpus.Clear();
        _selectedCpus.AddRange(recommendation.RecommendedCpus.Take(4)); // İlk 4 CPU'yu al
        
        // Button'ları güncelle
        foreach (Control control in this.Controls.Find("", true))
        {
            if (control is Button btn && btn.Name.StartsWith("btnCpu"))
            {
                var cpuId = (int)btn.Tag;
                UpdateCpuButtonState(btn, _selectedCpus.Contains(cpuId));
            }
        }
        
        UpdateAffinityDisplay();
        AnalyzeAndSuggest(); // Önerileri yenile
    }

    /// <summary>
    /// Profesyonel mod - gelişmiş CPU seçim dialog'u
    /// </summary>
    private void BtnProfessional_Click(object sender, EventArgs e)
    {
        using var professionalDialog = new ProfessionalCpuDialog(_selectedCpus, _systemInfo);
        if (professionalDialog.ShowDialog(this) == DialogResult.OK)
        {
            _selectedCpus = professionalDialog.SelectedCpus;
            UpdateAllCpuButtons();
            UpdateAffinityDisplay();
            AnalyzeAndSuggest();
        }
    }

    /// <summary>
    /// Registry yönetimi dialog'unu açar
    /// </summary>
    private void BtnRegistryManager_Click(object sender, EventArgs e)
    {
        using var registryDialog = new RegistryManagerDialog();
        registryDialog.ShowDialog(this);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (this.DialogResult == DialogResult.OK)
        {
            var chkPermanent = this.Controls.Find("chkPermanent", true).FirstOrDefault() as CheckBox;
            ApplyPermanently = chkPermanent?.Checked ?? false;
            
            if (_selectedCpus.Count == 0)
            {
                MessageBox.Show("En az bir CPU seçmelisiniz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }
        }
        
        base.OnFormClosing(e);
    }

    private string CalculateAffinityMaskString(List<int> cpus)
    {
        ulong mask = 0;
        foreach (int cpu in cpus)
        {
            if (cpu < 64)
                mask |= (1UL << cpu);
        }
        return $"0x{mask:X}";
    }

    private int CountSetBits(long value)
    {
        int count = 0;
        while (value != 0)
        {
            count++;
            value &= value - 1;
        }
        return count;
    }
}
