using System.Text;

namespace NumaManager;

/// <summary>
/// Profesyonel kullanıcılar için gelişmiş CPU seçim dialog'u
/// </summary>
public partial class ProfessionalCpuDialog : Form
{
    private List<int> _selectedCpus;
    private NumaService.ProcessorInfo _systemInfo;
    
    public List<int> SelectedCpus => _selectedCpus;

    public ProfessionalCpuDialog(List<int> selectedCpus, NumaService.ProcessorInfo systemInfo)
    {
        _selectedCpus = new List<int>(selectedCpus);
        _systemInfo = systemInfo;
        
        InitializeComponent();
        SetupDialog();
        LoadCurrentSelection();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(800, 600);
        this.Text = "🔧 Profesyonel CPU Seçimi";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.Font = new Font("Segoe UI", 9F);
    }

    private void SetupDialog()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(15)
        };

        // Column styles
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

        // Row styles
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F)); // Manuel seçim
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));   // CPU grid
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));   // Analiz
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // Butonlar

        // Manuel seçim paneli
        var manualPanel = CreateManualSelectionPanel();
        mainLayout.Controls.Add(manualPanel, 0, 0);
        mainLayout.SetColumnSpan(manualPanel, 2);

        // CPU grid
        var cpuPanel = CreateAdvancedCpuPanel();
        mainLayout.Controls.Add(cpuPanel, 0, 1);
        mainLayout.SetRowSpan(cpuPanel, 2);

        // Analiz paneli
        var analysisPanel = CreateAnalysisPanel();
        mainLayout.Controls.Add(analysisPanel, 1, 1);

        // Öneriler paneli
        var suggestionsPanel = CreateSuggestionsPanel();
        mainLayout.Controls.Add(suggestionsPanel, 1, 2);

        // Butonlar
        var buttonPanel = CreateButtonPanel();
        mainLayout.Controls.Add(buttonPanel, 0, 3);
        mainLayout.SetColumnSpan(buttonPanel, 2);

        this.Controls.Add(mainLayout);
    }

    private GroupBox CreateManualSelectionPanel()
    {
        var panel = new GroupBox
        {
            Text = "🎯 Manuel CPU Seçimi",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 3,
            Padding = new Padding(10)
        };

        // CPU ID'leri manuel girişi
        var lblManual = new Label { Text = "CPU ID'leri:", Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var txtManual = new TextBox 
        { 
            Name = "txtManualCpus",
            Font = new Font("Consolas", 9F),
            PlaceholderText = "Örnek: 0,1,4,5,8-11,16,20-23"
        };
        
        var btnApplyManual = new Button
        {
            Text = "Uygula",
            Size = new Size(60, 25),
            UseVisualStyleBackColor = true
        };
        btnApplyManual.Click += BtnApplyManual_Click;

        var btnValidate = new Button
        {
            Text = "Doğrula",
            Size = new Size(60, 25),
            BackColor = Color.LightBlue,
            UseVisualStyleBackColor = false
        };
        btnValidate.Click += BtnValidate_Click;

        // Affinity mask girişi
        var lblMask = new Label { Text = "Affinity Mask:", Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var txtMask = new TextBox 
        { 
            Name = "txtAffinityMask",
            Font = new Font("Consolas", 9F),
            PlaceholderText = "Örnek: 0xFF, 0x15, 255"
        };

        var btnApplyMask = new Button
        {
            Text = "Mask Uygula",
            Size = new Size(80, 25),
            UseVisualStyleBackColor = true
        };
        btnApplyMask.Click += BtnApplyMask_Click;

        var btnGenerate = new Button
        {
            Text = "Oluştur",
            Size = new Size(60, 25),
            BackColor = Color.LightGreen,
            UseVisualStyleBackColor = false
        };
        btnGenerate.Click += BtnGenerate_Click;

        // Hızlı seçim
        var lblQuick = new Label { Text = "Hızlı Seçim:", Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var cmbQuick = new ComboBox
        {
            Name = "cmbQuickSelection",
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F)
        };
        cmbQuick.Items.AddRange(new object[] {
            "Tek Çekirdekler (0,2,4,6...)",
            "Çift Çekirdekler (1,3,5,7...)", 
            "İlk Yarı (0-N/2)",
            "Son Yarı (N/2-N)",
            "NUMA Node 0",
            "NUMA Node 1",
            "Fibonacci (1,1,2,3,5,8...)",
            "Asal Sayılar (2,3,5,7,11...)"
        });

        var btnApplyQuick = new Button
        {
            Text = "Hızlı Uygula",
            Size = new Size(80, 25),
            BackColor = Color.Orange,
            UseVisualStyleBackColor = false
        };
        btnApplyQuick.Click += BtnApplyQuick_Click;

        layout.Controls.Add(lblManual, 0, 0);
        layout.Controls.Add(txtManual, 1, 0);
        layout.Controls.Add(btnApplyManual, 2, 0);
        layout.Controls.Add(btnValidate, 3, 0);

        layout.Controls.Add(lblMask, 0, 1);
        layout.Controls.Add(txtMask, 1, 1);
        layout.Controls.Add(btnApplyMask, 2, 1);
        layout.Controls.Add(btnGenerate, 3, 1);

        layout.Controls.Add(lblQuick, 0, 2);
        layout.Controls.Add(cmbQuick, 1, 2);
        layout.Controls.Add(btnApplyQuick, 2, 2);

        panel.Controls.Add(layout);
        return panel;
    }

    private GroupBox CreateAdvancedCpuPanel()
    {
        var panel = new GroupBox
        {
            Text = $"🖥️ CPU Haritası ({_systemInfo.LogicalProcessors} CPU) - Gelişmiş Görünüm",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10)
        };

        // CPU'ları 8'li satırlar halinde düzenle
        var mainFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Padding = new Padding(5)
        };

        for (int row = 0; row < Math.Ceiling(_systemInfo.LogicalProcessors / 8.0); row++)
        {
            var rowPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 2)
            };

            // Satır etiketi
            var rowLabel = new Label
            {
                Text = $"{row * 8:D2}-{Math.Min((row + 1) * 8 - 1, _systemInfo.LogicalProcessors - 1):D2}:",
                Size = new Size(40, 35),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.LightGray
            };
            rowPanel.Controls.Add(rowLabel);

            // CPU butonları
            for (int col = 0; col < 8 && row * 8 + col < _systemInfo.LogicalProcessors; col++)
            {
                int cpuId = row * 8 + col;
                var cpuButton = new Button
                {
                    Name = $"btnCpu{cpuId}",
                    Text = $"{cpuId}",
                    Size = new Size(35, 35),
                    Tag = cpuId,
                    Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                    Margin = new Padding(1),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                // Core bilgisi - tooltip yerine hover event kullanacağız
                var coreId = cpuId / 2;
                var threadId = cpuId % 2;

                cpuButton.Click += CpuButton_Click;
                rowPanel.Controls.Add(cpuButton);
            }

            mainFlow.Controls.Add(rowPanel);
        }

        scrollPanel.Controls.Add(mainFlow);
        panel.Controls.Add(scrollPanel);
        return panel;
    }

    private GroupBox CreateAnalysisPanel()
    {
        var panel = new GroupBox
        {
            Text = "📊 Seçim Analizi",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var rtb = new RichTextBox
        {
            Name = "rtbAnalysis",
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Segoe UI", 8.5F),
            BackColor = Color.AliceBlue
        };

        panel.Controls.Add(rtb);
        return panel;
    }

    private GroupBox CreateSuggestionsPanel()
    {
        var panel = new GroupBox
        {
            Text = "💡 Akıllı Öneriler",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(10)
        };

        var btnOptimalPerf = new Button
        {
            Text = "🚀 Maksimum Performans",
            Size = new Size(150, 30),
            BackColor = Color.LightGreen,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI", 8F)
        };
        btnOptimalPerf.Click += (s, e) => ApplyOptimalPerformance();

        var btnOptimalPower = new Button
        {
            Text = "🔋 Güç Tasarrufu",
            Size = new Size(150, 30),
            BackColor = Color.LightBlue,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI", 8F)
        };
        btnOptimalPower.Click += (s, e) => ApplyPowerSaving();

        var btnOptimalBalance = new Button
        {
            Text = "⚖️ Dengeli",
            Size = new Size(150, 30),
            BackColor = Color.LightYellow,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI", 8F)
        };
        btnOptimalBalance.Click += (s, e) => ApplyBalanced();

        layout.Controls.AddRange(new Control[] { btnOptimalPerf, btnOptimalPower, btnOptimalBalance });
        panel.Controls.Add(layout);
        return panel;
    }

    private Panel CreateButtonPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };

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

        var btnOK = new Button
        {
            Text = "Tamam",
            Size = new Size(80, 35),
            DialogResult = DialogResult.OK,
            BackColor = Color.LightGreen,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        layout.Controls.AddRange(new Control[] { btnCancel, btnOK });
        panel.Controls.Add(layout);
        return panel;
    }

    private void LoadCurrentSelection()
    {
        UpdateAllCpuButtons();
        UpdateAnalysis();
        
        // Manuel girişi güncelle
        var txtManual = this.Controls.Find("txtManualCpus", true).FirstOrDefault() as TextBox;
        if (txtManual != null)
        {
            txtManual.Text = string.Join(",", _selectedCpus.OrderBy(x => x));
        }
    }

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
    }

    private void UpdateCpuButtonState(Button button, bool selected)
    {
        var cpuId = (int)button.Tag;
        
        if (selected)
        {
            button.BackColor = Color.LimeGreen;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI", 7F, FontStyle.Bold);
        }
        else
        {
            // NUMA node'a göre renk
            var nodeColor = GetNumaNodeColor(cpuId);
            button.BackColor = nodeColor;
            button.ForeColor = Color.Black;
            button.Font = new Font("Segoe UI", 7F);
        }
    }

    private Color GetNumaNodeColor(int cpuId)
    {
        var nodeId = GetNumaNodeId(cpuId);
        return nodeId switch
        {
            0 => Color.LightCyan,
            1 => Color.LightPink,
            2 => Color.LightGoldenrodYellow,
            3 => Color.LightSalmon,
            _ => Color.LightGray
        };
    }

    private int GetNumaNodeId(int cpuId)
    {
        foreach (var node in _systemInfo.Nodes)
        {
            if (node.ProcessorIds.Contains(cpuId))
                return node.NodeId;
        }
        return 0;
    }

    private void UpdateAnalysis()
    {
        var rtb = this.Controls.Find("rtbAnalysis", true).FirstOrDefault() as RichTextBox;
        if (rtb == null) return;

        var analysis = new StringBuilder();
        analysis.AppendLine($"🎯 Seçili CPU Sayısı: {_selectedCpus.Count}");
        analysis.AppendLine($"📊 Toplam CPU: {_systemInfo.LogicalProcessors}");
        analysis.AppendLine($"📈 Kullanım Oranı: %{(_selectedCpus.Count * 100.0 / _systemInfo.LogicalProcessors):F1}");
        analysis.AppendLine();

        // NUMA dağılımı
        var numaDistribution = _selectedCpus.GroupBy(GetNumaNodeId).ToDictionary(g => g.Key, g => g.Count());
        analysis.AppendLine("🗺️ NUMA Dağılımı:");
        foreach (var kvp in numaDistribution.OrderBy(x => x.Key))
        {
            analysis.AppendLine($"   Node {kvp.Key}: {kvp.Value} CPU");
        }
        analysis.AppendLine();

        // Core analizi
        var coreGroups = _selectedCpus.GroupBy(cpu => cpu / 2);
        var hyperthreadingCores = coreGroups.Where(g => g.Count() > 1).ToList();
        
        analysis.AppendLine("🔧 Core Analizi:");
        analysis.AppendLine($"   Kullanılan Core Sayısı: {coreGroups.Count()}");
        analysis.AppendLine($"   Hyperthreading Core'lar: {hyperthreadingCores.Count()}");
        
        if (hyperthreadingCores.Any())
        {
            analysis.AppendLine("   ⚠️ HT Çakışma Riski Var!");
            foreach (var core in hyperthreadingCores)
            {
                analysis.AppendLine($"      Core {core.Key}: CPU {string.Join(",", core)}");
            }
        }

        rtb.Text = analysis.ToString();
    }

    // Event Handlers
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
        UpdateAnalysis();
        LoadCurrentSelection(); // Manuel girişi güncelle
    }

    private void BtnApplyManual_Click(object sender, EventArgs e)
    {
        var txtManual = this.Controls.Find("txtManualCpus", true).FirstOrDefault() as TextBox;
        if (txtManual == null) return;

        try
        {
            var cpuIds = ParseCpuIds(txtManual.Text);
            _selectedCpus = cpuIds.Where(id => id >= 0 && id < _systemInfo.LogicalProcessors).ToList();
            UpdateAllCpuButtons();
            UpdateAnalysis();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Geçersiz format: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnValidate_Click(object sender, EventArgs e)
    {
        var txtManual = this.Controls.Find("txtManualCpus", true).FirstOrDefault() as TextBox;
        if (txtManual == null) return;

        try
        {
            var cpuIds = ParseCpuIds(txtManual.Text);
            var validIds = cpuIds.Where(id => id >= 0 && id < _systemInfo.LogicalProcessors).ToList();
            var invalidIds = cpuIds.Where(id => id < 0 || id >= _systemInfo.LogicalProcessors).ToList();

            var message = $"✅ Geçerli CPU'lar: {validIds.Count}\n";
            if (invalidIds.Any())
            {
                message += $"❌ Geçersiz CPU'lar: {string.Join(", ", invalidIds)}\n";
            }
            message += $"📊 Toplam: {cpuIds.Count}";

            MessageBox.Show(message, "Doğrulama Sonucu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Geçersiz format: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnApplyMask_Click(object sender, EventArgs e)
    {
        var txtMask = this.Controls.Find("txtAffinityMask", true).FirstOrDefault() as TextBox;
        if (txtMask == null) return;

        try
        {
            var maskText = txtMask.Text.Trim();
            long mask;
            
            if (maskText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                mask = Convert.ToInt64(maskText, 16);
            }
            else
            {
                mask = long.Parse(maskText);
            }

            _selectedCpus.Clear();
            for (int i = 0; i < _systemInfo.LogicalProcessors && i < 64; i++)
            {
                if ((mask & (1L << i)) != 0)
                {
                    _selectedCpus.Add(i);
                }
            }

            UpdateAllCpuButtons();
            UpdateAnalysis();
            LoadCurrentSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Geçersiz mask formatı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnGenerate_Click(object sender, EventArgs e)
    {
        var txtMask = this.Controls.Find("txtAffinityMask", true).FirstOrDefault() as TextBox;
        if (txtMask == null) return;

        ulong mask = 0;
        foreach (int cpuId in _selectedCpus)
        {
            if (cpuId < 64)
                mask |= (1UL << cpuId);
        }

        txtMask.Text = $"0x{mask:X}";
    }

    private void BtnApplyQuick_Click(object sender, EventArgs e)
    {
        var cmbQuick = this.Controls.Find("cmbQuickSelection", true).FirstOrDefault() as ComboBox;
        if (cmbQuick?.SelectedItem == null) return;

        var selection = cmbQuick.SelectedItem.ToString();
        _selectedCpus.Clear();

        switch (selection)
        {
            case var s when s.Contains("Tek Çekirdekler"):
                for (int i = 0; i < _systemInfo.LogicalProcessors; i += 2)
                    _selectedCpus.Add(i);
                break;
                
            case var s when s.Contains("Çift Çekirdekler"):
                for (int i = 1; i < _systemInfo.LogicalProcessors; i += 2)
                    _selectedCpus.Add(i);
                break;
                
            case var s when s.Contains("İlk Yarı"):
                for (int i = 0; i < _systemInfo.LogicalProcessors / 2; i++)
                    _selectedCpus.Add(i);
                break;
                
            case var s when s.Contains("Son Yarı"):
                for (int i = _systemInfo.LogicalProcessors / 2; i < _systemInfo.LogicalProcessors; i++)
                    _selectedCpus.Add(i);
                break;
                
            case var s when s.Contains("NUMA Node 0"):
                _selectedCpus.AddRange(_systemInfo.Nodes.FirstOrDefault()?.ProcessorIds ?? new List<int>());
                break;
                
            case var s when s.Contains("NUMA Node 1"):
                _selectedCpus.AddRange(_systemInfo.Nodes.Skip(1).FirstOrDefault()?.ProcessorIds ?? new List<int>());
                break;
                
            case var s when s.Contains("Fibonacci"):
                var fib = GenerateFibonacci(_systemInfo.LogicalProcessors);
                _selectedCpus.AddRange(fib.Where(f => f < _systemInfo.LogicalProcessors));
                break;
                
            case var s when s.Contains("Asal Sayılar"):
                var primes = GeneratePrimes(_systemInfo.LogicalProcessors);
                _selectedCpus.AddRange(primes.Where(p => p < _systemInfo.LogicalProcessors));
                break;
        }

        UpdateAllCpuButtons();
        UpdateAnalysis();
        LoadCurrentSelection();
    }

    private List<int> ParseCpuIds(string input)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(input)) return result;

        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            
            if (trimmed.Contains('-'))
            {
                // Aralık (örnek: 8-11)
                var rangeParts = trimmed.Split('-');
                if (rangeParts.Length == 2 && 
                    int.TryParse(rangeParts[0].Trim(), out int start) &&
                    int.TryParse(rangeParts[1].Trim(), out int end))
                {
                    for (int i = start; i <= end; i++)
                    {
                        result.Add(i);
                    }
                }
            }
            else
            {
                // Tek sayı
                if (int.TryParse(trimmed, out int cpuId))
                {
                    result.Add(cpuId);
                }
            }
        }

        return result.Distinct().OrderBy(x => x).ToList();
    }

    private List<int> GenerateFibonacci(int max)
    {
        var fib = new List<int> { 1, 1 };
        while (true)
        {
            var next = fib[^1] + fib[^2];
            if (next >= max) break;
            fib.Add(next);
        }
        return fib;
    }

    private List<int> GeneratePrimes(int max)
    {
        var primes = new List<int>();
        for (int i = 2; i < max; i++)
        {
            bool isPrime = true;
            for (int j = 2; j * j <= i; j++)
            {
                if (i % j == 0)
                {
                    isPrime = false;
                    break;
                }
            }
            if (isPrime) primes.Add(i);
        }
        return primes;
    }

    private void ApplyOptimalPerformance()
    {
        // Tüm fiziksel core'ları kullan ama hyperthreading'den kaçın
        _selectedCpus.Clear();
        for (int i = 0; i < _systemInfo.LogicalProcessors; i += 2)
        {
            _selectedCpus.Add(i);
        }
        UpdateAllCpuButtons();
        UpdateAnalysis();
        LoadCurrentSelection();
    }

    private void ApplyPowerSaving()
    {
        // Sadece ilk NUMA node'daki ilk 4 CPU
        _selectedCpus.Clear();
        var firstNode = _systemInfo.Nodes.FirstOrDefault();
        if (firstNode != null)
        {
            _selectedCpus.AddRange(firstNode.ProcessorIds.Take(4));
        }
        UpdateAllCpuButtons();
        UpdateAnalysis();
        LoadCurrentSelection();
    }

    private void ApplyBalanced()
    {
        // Her NUMA node'dan eşit sayıda CPU
        _selectedCpus.Clear();
        int cpusPerNode = Math.Max(1, 8 / _systemInfo.Nodes.Count);
        
        foreach (var node in _systemInfo.Nodes)
        {
            _selectedCpus.AddRange(node.ProcessorIds.Take(cpusPerNode));
        }
        
        UpdateAllCpuButtons();
        UpdateAnalysis();
        LoadCurrentSelection();
    }
}
