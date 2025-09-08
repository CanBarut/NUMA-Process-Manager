using Microsoft.Win32;
using System.Text;

namespace NumaManager;

/// <summary>
/// Registry'deki NUMA affinity ayarlarını yöneten dialog
/// </summary>
public partial class RegistryManagerDialog : Form
{
    private const string REGISTRY_PATH = @"SOFTWARE\LabsOffice\NumaManager\ProcessAffinity";
    private List<ProcessAffinityRecord> _records = new();

    public RegistryManagerDialog()
    {
        InitializeComponent();
        SetupDialog();
        LoadRegistryRecords();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(700, 500);
        this.Text = "🗃️ Registry NUMA Yönetimi";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.Font = new Font("Segoe UI", 9F);
    }

    private void SetupDialog()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(15)
        };

        // Row styles
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));  // Bilgi
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Liste
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F)); // Detaylar
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // Butonlar

        // Bilgi paneli
        var infoPanel = CreateInfoPanel();
        mainLayout.Controls.Add(infoPanel, 0, 0);

        // Liste paneli
        var listPanel = CreateListPanel();
        mainLayout.Controls.Add(listPanel, 0, 1);

        // Detay paneli
        var detailPanel = CreateDetailPanel();
        mainLayout.Controls.Add(detailPanel, 0, 2);

        // Buton paneli
        var buttonPanel = CreateButtonPanel();
        mainLayout.Controls.Add(buttonPanel, 0, 3);

        this.Controls.Add(mainLayout);
    }

    private Panel CreateInfoPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        
        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };

        var lblInfo = new Label
        {
            Text = "🗃️ Registry'de kayıtlı NUMA affinity ayarları:",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.DarkBlue,
            AutoSize = true
        };

        var lblPath = new Label
        {
            Text = $"Konum: HKEY_CURRENT_USER\\{REGISTRY_PATH}",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.Gray,
            AutoSize = true,
            Margin = new Padding(20, 5, 0, 0)
        };

        layout.Controls.AddRange(new Control[] { lblInfo, lblPath });
        panel.Controls.Add(layout);
        return panel;
    }

    private GroupBox CreateListPanel()
    {
        var panel = new GroupBox
        {
            Text = "💾 Kayıtlı Affinity Ayarları",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var listView = new ListView
        {
            Name = "lvRecords",
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false,
            Font = new Font("Consolas", 9F)
        };

        // Kolonlar
        listView.Columns.Add("Process Adı", 150);
        listView.Columns.Add("Affinity Mask", 120);
        listView.Columns.Add("CPU Sayısı", 80);
        listView.Columns.Add("CPU ID'leri", 200);
        listView.Columns.Add("Kayıt Tarihi", 120);

        listView.SelectedIndexChanged += ListView_SelectedIndexChanged;

        panel.Controls.Add(listView);
        return panel;
    }

    private GroupBox CreateDetailPanel()
    {
        var panel = new GroupBox
        {
            Text = "🔍 Detaylar",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var rtb = new RichTextBox
        {
            Name = "rtbDetails",
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.AliceBlue
        };

        panel.Controls.Add(rtb);
        return panel;
    }

    private Panel CreateButtonPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };

        var btnRefresh = new Button
        {
            Text = "🔄 Yenile",
            Size = new Size(80, 35),
            UseVisualStyleBackColor = true
        };
        btnRefresh.Click += BtnRefresh_Click;

        var btnDelete = new Button
        {
            Text = "🗑️ Sil",
            Size = new Size(80, 35),
            BackColor = Color.LightCoral,
            UseVisualStyleBackColor = false
        };
        btnDelete.Click += BtnDelete_Click;

        var btnDeleteAll = new Button
        {
            Text = "🗑️ Tümünü Sil",
            Size = new Size(100, 35),
            BackColor = Color.Red,
            ForeColor = Color.White,
            UseVisualStyleBackColor = false
        };
        btnDeleteAll.Click += BtnDeleteAll_Click;

        var btnExport = new Button
        {
            Text = "📤 Dışa Aktar",
            Size = new Size(100, 35),
            BackColor = Color.LightBlue,
            UseVisualStyleBackColor = false
        };
        btnExport.Click += BtnExport_Click;

        var btnImport = new Button
        {
            Text = "📥 İçe Aktar",
            Size = new Size(100, 35),
            BackColor = Color.LightGreen,
            UseVisualStyleBackColor = false
        };
        btnImport.Click += BtnImport_Click;

        var btnClose = new Button
        {
            Text = "Kapat",
            Size = new Size(80, 35),
            DialogResult = DialogResult.OK,
            UseVisualStyleBackColor = true
        };

        layout.Controls.AddRange(new Control[] { btnRefresh, btnDelete, btnDeleteAll, btnExport, btnImport, btnClose });
        panel.Controls.Add(layout);
        return panel;
    }

    private void LoadRegistryRecords()
    {
        _records.Clear();

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH);
            if (key != null)
            {
                foreach (string valueName in key.GetValueNames())
                {
                    var valueData = key.GetValue(valueName)?.ToString();
                    if (!string.IsNullOrEmpty(valueData))
                    {
                        var record = new ProcessAffinityRecord
                        {
                            ProcessName = valueName,
                            AffinityMaskHex = valueData,
                            RegistryDate = GetRegistryValueDate(key, valueName)
                        };

                        // Affinity mask'ı parse et
                        if (long.TryParse(valueData, System.Globalization.NumberStyles.HexNumber, null, out long mask))
                        {
                            record.AffinityMask = mask;
                            record.CpuIds = ParseAffinityMask(mask);
                            record.CpuCount = record.CpuIds.Count;
                        }

                        _records.Add(record);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Registry okuma hatası: {ex.Message}", "Hata", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        UpdateListView();
    }

    private void UpdateListView()
    {
        var listView = this.Controls.Find("lvRecords", true).FirstOrDefault() as ListView;
        if (listView == null) return;

        listView.Items.Clear();

        foreach (var record in _records.OrderBy(r => r.ProcessName))
        {
            var item = new ListViewItem(record.ProcessName);
            item.SubItems.Add($"0x{record.AffinityMaskHex}");
            item.SubItems.Add(record.CpuCount.ToString());
            item.SubItems.Add(string.Join(", ", record.CpuIds));
            item.SubItems.Add(record.RegistryDate.ToString("dd.MM.yyyy HH:mm"));
            item.Tag = record;

            listView.Items.Add(item);
        }

        // Başlık güncelle
        var panel = listView.Parent as GroupBox;
        if (panel != null)
        {
            panel.Text = $"💾 Kayıtlı Affinity Ayarları ({_records.Count} kayıt)";
        }
    }

    private void UpdateDetails(ProcessAffinityRecord record)
    {
        var rtb = this.Controls.Find("rtbDetails", true).FirstOrDefault() as RichTextBox;
        if (rtb == null || record == null) return;

        var details = new StringBuilder();
        details.AppendLine($"🎯 Process: {record.ProcessName}");
        details.AppendLine($"🔢 Affinity Mask: 0x{record.AffinityMaskHex}");
        details.AppendLine($"📊 CPU Sayısı: {record.CpuCount}");
        details.AppendLine($"🖥️ CPU ID'leri: {string.Join(", ", record.CpuIds)}");
        details.AppendLine($"📅 Kayıt Tarihi: {record.RegistryDate:dd.MM.yyyy HH:mm:ss}");
        details.AppendLine();

        // Binary representation
        var binary = Convert.ToString(record.AffinityMask, 2).PadLeft(32, '0');
        details.AppendLine($"🔢 Binary: {binary}");
        details.AppendLine();

        // NUMA analizi
        details.AppendLine("🗺️ NUMA Analizi:");
        var numaGroups = record.CpuIds.GroupBy(cpu => cpu / 16); // Basit NUMA grouping
        foreach (var group in numaGroups)
        {
            details.AppendLine($"   Node ~{group.Key}: {string.Join(", ", group)} ({group.Count()} CPU)");
        }
        details.AppendLine();

        // Core analizi
        var coreGroups = record.CpuIds.GroupBy(cpu => cpu / 2);
        var hyperthreadingCores = coreGroups.Where(g => g.Count() > 1).ToList();
        
        details.AppendLine("🔧 Core Analizi:");
        details.AppendLine($"   Kullanılan Core Sayısı: {coreGroups.Count()}");
        details.AppendLine($"   Hyperthreading Core'lar: {hyperthreadingCores.Count()}");
        
        if (hyperthreadingCores.Any())
        {
            details.AppendLine("   ⚠️ HT Çakışması:");
            foreach (var core in hyperthreadingCores.Take(5)) // İlk 5'ini göster
            {
                details.AppendLine($"      Core {core.Key}: CPU {string.Join(",", core)}");
            }
        }

        rtb.Text = details.ToString();
    }

    private List<int> ParseAffinityMask(long mask)
    {
        var cpuIds = new List<int>();
        for (int i = 0; i < 64; i++)
        {
            if ((mask & (1L << i)) != 0)
            {
                cpuIds.Add(i);
            }
        }
        return cpuIds;
    }

    private DateTime GetRegistryValueDate(RegistryKey key, string valueName)
    {
        try
        {
            // Registry'de değer oluşturma tarihi doğrudan alınamaz
            // Bu nedenle mevcut tarihi döndürüyoruz
            return DateTime.Now;
        }
        catch
        {
            return DateTime.Now;
        }
    }

    // Event Handlers
    private void ListView_SelectedIndexChanged(object sender, EventArgs e)
    {
        var listView = sender as ListView;
        if (listView?.SelectedItems.Count > 0)
        {
            var record = listView.SelectedItems[0].Tag as ProcessAffinityRecord;
            UpdateDetails(record);
        }
        else
        {
            var rtb = this.Controls.Find("rtbDetails", true).FirstOrDefault() as RichTextBox;
            if (rtb != null) rtb.Text = "Detayları görmek için bir kayıt seçin...";
        }
    }

    private void BtnRefresh_Click(object sender, EventArgs e)
    {
        LoadRegistryRecords();
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        var listView = this.Controls.Find("lvRecords", true).FirstOrDefault() as ListView;
        if (listView?.SelectedItems.Count > 0)
        {
            var record = listView.SelectedItems[0].Tag as ProcessAffinityRecord;
            if (record != null)
            {
                var result = MessageBox.Show(
                    $"'{record.ProcessName}' process'inin affinity ayarını silmek istediğinizden emin misiniz?\n\n" +
                    $"Bu işlem geri alınamaz!",
                    "Kayıt Silme Onayı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    DeleteRegistryRecord(record.ProcessName);
                    LoadRegistryRecords();
                }
            }
        }
        else
        {
            MessageBox.Show("Lütfen silinecek kaydı seçin!", "Uyarı", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnDeleteAll_Click(object sender, EventArgs e)
    {
        if (_records.Count == 0)
        {
            MessageBox.Show("Silinecek kayıt yok!", "Bilgi", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            $"TÜM affinity ayarlarını silmek istediğinizden emin misiniz?\n\n" +
            $"📊 Silinecek kayıt sayısı: {_records.Count}\n" +
            $"⚠️ Bu işlem geri alınamaz!",
            "Tüm Kayıtları Silme Onayı",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(REGISTRY_PATH, false);
                LoadRegistryRecords();
                MessageBox.Show("Tüm kayıtlar başarıyla silindi!", "Başarılı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Silme işleminde hata: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void BtnExport_Click(object sender, EventArgs e)
    {
        if (_records.Count == 0)
        {
            MessageBox.Show("Dışa aktarılacak kayıt yok!", "Bilgi", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var saveDialog = new SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|Text Files (*.txt)|*.txt",
            DefaultExt = "json",
            FileName = $"NumaAffinityBackup_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (saveDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_records, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(saveDialog.FileName, json);
                
                MessageBox.Show($"Kayıtlar başarıyla dışa aktarıldı!\n\nDosya: {saveDialog.FileName}", 
                    "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void BtnImport_Click(object sender, EventArgs e)
    {
        using var openDialog = new OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|Text Files (*.txt)|*.txt",
            Multiselect = false
        };

        if (openDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var json = File.ReadAllText(openDialog.FileName);
                var importedRecords = System.Text.Json.JsonSerializer.Deserialize<List<ProcessAffinityRecord>>(json);

                if (importedRecords?.Count > 0)
                {
                    var result = MessageBox.Show(
                        $"İçe aktarılacak kayıt sayısı: {importedRecords.Count}\n\n" +
                        $"Mevcut kayıtlar üzerine yazılacak. Devam edilsin mi?",
                        "İçe Aktarma Onayı",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        ImportRecords(importedRecords);
                        LoadRegistryRecords();
                        
                        MessageBox.Show($"{importedRecords.Count} kayıt başarıyla içe aktarıldı!", 
                            "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İçe aktarma hatası: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void DeleteRegistryRecord(string processName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH, true);
            key?.DeleteValue(processName, false);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Registry silme hatası: {ex.Message}", "Hata", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportRecords(List<ProcessAffinityRecord> records)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(REGISTRY_PATH);
            foreach (var record in records)
            {
                key.SetValue(record.ProcessName, record.AffinityMaskHex);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Registry yazma hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Process affinity kaydı
    /// </summary>
    public class ProcessAffinityRecord
    {
        public string ProcessName { get; set; } = "";
        public string AffinityMaskHex { get; set; } = "";
        public long AffinityMask { get; set; }
        public List<int> CpuIds { get; set; } = new();
        public int CpuCount { get; set; }
        public DateTime RegistryDate { get; set; }
    }
}
