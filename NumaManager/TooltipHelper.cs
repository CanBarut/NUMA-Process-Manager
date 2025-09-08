using System.ComponentModel;

namespace NumaManager;

/// <summary>
/// Tooltip yönetimi için yardımcı sınıf
/// </summary>
public static class TooltipHelper
{
    private static readonly Dictionary<Control, ToolTip> _tooltips = new();

    /// <summary>
    /// Kontrole tooltip ekler
    /// </summary>
    /// <param name="control">Tooltip eklenecek kontrol</param>
    /// <param name="text">Tooltip metni</param>
    /// <param name="title">Tooltip başlığı (opsiyonel)</param>
    public static void SetTooltip(Control control, string text, string title = "")
    {
        if (control == null || string.IsNullOrEmpty(text)) return;

        // Eğer zaten tooltip varsa güncelle
        if (_tooltips.ContainsKey(control))
        {
            _tooltips[control].SetToolTip(control, text);
            if (!string.IsNullOrEmpty(title))
            {
                _tooltips[control].ToolTipTitle = title;
            }
            return;
        }

        // Yeni tooltip oluştur
        var tooltip = new ToolTip
        {
            IsBalloon = true,
            ToolTipIcon = ToolTipIcon.Info,
            ToolTipTitle = title,
            AutoPopDelay = 10000, // 10 saniye
            InitialDelay = 500,   // 0.5 saniye
            ReshowDelay = 100,    // 0.1 saniye
            ShowAlways = true
        };

        tooltip.SetToolTip(control, text);
        _tooltips[control] = tooltip;

        // Form kapanırken tooltip'i temizle
        control.Disposed += (s, e) => {
            if (_tooltips.ContainsKey(control))
            {
                _tooltips[control].Dispose();
                _tooltips.Remove(control);
            }
        };
    }

    /// <summary>
    /// Kontrolden tooltip'i kaldırır
    /// </summary>
    /// <param name="control">Tooltip kaldırılacak kontrol</param>
    public static void RemoveTooltip(Control control)
    {
        if (control == null || !_tooltips.ContainsKey(control)) return;

        _tooltips[control].Dispose();
        _tooltips.Remove(control);
    }

    /// <summary>
    /// Tüm tooltip'leri temizler
    /// </summary>
    public static void ClearAllTooltips()
    {
        foreach (var tooltip in _tooltips.Values)
        {
            tooltip.Dispose();
        }
        _tooltips.Clear();
    }

    /// <summary>
    /// Form için tooltip'leri temizler
    /// </summary>
    /// <param name="form">Tooltip'leri temizlenecek form</param>
    public static void ClearFormTooltips(Form form)
    {
        if (form == null) return;

        var controlsToRemove = _tooltips.Keys.Where(c => c.FindForm() == form).ToList();
        foreach (var control in controlsToRemove)
        {
            RemoveTooltip(control);
        }
    }
}

/// <summary>
/// Tooltip metinleri için sabitler
/// </summary>
public static class TooltipTexts
{
    // Ana Form Tooltip'leri
    public const string REFRESH_BUTTON = "🔄 Process listesini ve sistem bilgilerini yeniler\n\n" +
                                        "• Çalışan process'leri günceller\n" +
                                        "• CPU kullanım oranlarını yeniler\n" +
                                        "• NUMA node bilgilerini kontrol eder\n\n" +
                                        "💡 İpucu: Otomatik yenileme aktifse bu butona gerek yok";

    public const string APPLY_AFFINITY_BUTTON = "🎯 Seçili process'e CPU affinity uygular\n\n" +
                                               "• Process'i seçin (çift tıklayın)\n" +
                                               "• CPU'ları seçin (NUMA node'dan)\n" +
                                               "• Bu butona tıklayın\n\n" +
                                               "⚠️ Dikkat: Process'e yönetici izni gerekebilir";

    public const string RESET_BUTTON = "🔄 Seçili process'in affinity'sini sıfırlar\n\n" +
                                      "• Tüm CPU'ları kullanmasına izin verir\n" +
                                      "• Sistem varsayılan ayarına döner\n" +
                                      "• Affinity kısıtlamasını kaldırır\n\n" +
                                      "💡 İpucu: Process performans sorunları yaşıyorsa kullanın";

    public const string AUTO_REFRESH_CHECKBOX = "⏰ Otomatik yenileme ayarı\n\n" +
                                               "✅ Aktif: Her 5 saniyede bir otomatik güncelleme\n" +
                                               "❌ Pasif: Manuel yenileme gerekir\n\n" +
                                               "💡 İpucu: Sürekli izleme için aktif bırakın";

    public const string NUMA_NODE_COMBO = "🗺️ NUMA Node seçimi\n\n" +
                                         "• Her node farklı bellek alanına sahip\n" +
                                         "• Aynı node'daki CPU'lar daha hızlı\n" +
                                         "• Process'i seçtiğiniz node'da çalıştırır\n\n" +
                                         "💡 İpucu: Process'in bellek kullanımına göre seçin";

    public const string CPU_CHECKLIST = "🖥️ CPU çekirdek seçimi\n\n" +
                                       "• Tıklayarak CPU'ları seçin/deseç edin\n" +
                                       "• Yeşil: Seçili CPU'lar\n" +
                                       "• Renkli: NUMA node'ları\n\n" +
                                       "💡 İpucu: Çok fazla CPU seçmek performansı düşürebilir";

    public const string PROCESS_LIST = "📋 Çalışan process'ler\n\n" +
                                      "• PID: Process ID numarası\n" +
                                      "• Process Adı: Uygulama adı\n" +
                                      "• RAM: Bellek kullanımı (MB)\n" +
                                      "• Affinity: Mevcut CPU ataması\n\n" +
                                      "💡 İpucu: Process'e çift tıklayarak seçin";

    // Affinity Dialog Tooltip'leri
    public const string OPTIMAL_BUTTON = "🚀 Optimal ayar önerisi\n\n" +
                                        "• Sistem analizi yapar\n" +
                                        "• En uygun CPU'ları seçer\n" +
                                        "• NUMA optimizasyonu uygular\n\n" +
                                        "💡 İpucu: En iyi performans için kullanın";

    public const string PROFESSIONAL_BUTTON = "🔧 Profesyonel mod\n\n" +
                                             "• Gelişmiş CPU seçim araçları\n" +
                                             "• Manuel mask girişi\n" +
                                             "• Detaylı analiz\n\n" +
                                             "💡 İpucu: Uzman kullanıcılar için";

    public const string REGISTRY_MANAGER_BUTTON = "🗃️ Registry yönetimi\n\n" +
                                                  "• Kalıcı ayarları görüntüle\n" +
                                                  "• Kayıtları sil/düzenle\n" +
                                                  "• Yedekle/geri yükle\n\n" +
                                                  "💡 İpucu: Kalıcı ayarlar için";

    public const string ERP_SMART_BUTTON = "🏢 ERP Akıllı Atama\n\n" +
                                          "• ERP yazılımları için özel NUMA stratejisi\n" +
                                          "• Process türüne göre optimal node seçimi\n" +
                                          "• Yatay mimari optimizasyonu\n" +
                                          "• Veritabanı: Son node'lar (daha az kullanılır)\n" +
                                          "• Web: Orta node'lar (dengeli yük)\n" +
                                          "• Rapor: İlk node (daha stabil)\n" +
                                          "• Uygulama: En az yüklü node\n\n" +
                                          "💡 İpucu: ERP sistemleri için ideal performans!";

    public const string PERMANENT_CHECKBOX = "💾 Kalıcı kaydetme\n\n" +
                                            "✅ Aktif: Registry'ye kaydeder\n" +
                                            "❌ Pasif: Sadece bu oturum için\n\n" +
                                            "⚠️ Dikkat: Process her açıldığında uygulanır";

    public const string CPU_BUTTON = "🖥️ CPU çekirdeği\n\n" +
                                    "• Tıklayarak seçin/deseç edin\n" +
                                    "• Yeşil: Seçili\n" +
                                    "• Renkli: NUMA node'u\n\n" +
                                    "💡 İpucu: Aynı node'daki CPU'lar daha hızlı";

    // Professional Dialog Tooltip'leri
    public const string MANUAL_CPU_INPUT = "✏️ Manuel CPU ID girişi\n\n" +
                                          "Format örnekleri:\n" +
                                          "• 0,1,2,3 (tek tek)\n" +
                                          "• 0-7 (aralık)\n" +
                                          "• 0,1,4-7,12 (karışık)\n\n" +
                                          "💡 İpucu: Virgülle ayırın, tire ile aralık belirtin";

    public const string AFFINITY_MASK_INPUT = "🔢 Affinity mask girişi\n\n" +
                                             "Format örnekleri:\n" +
                                             "• 0xFF (hex)\n" +
                                             "• 255 (decimal)\n" +
                                             "• 0x15 (hex)\n\n" +
                                             "💡 İpucu: 0x ile hex, sadece sayı ile decimal";

    public const string QUICK_SELECTION = "⚡ Hızlı seçim modları\n\n" +
                                         "• Tek/Çift çekirdekler\n" +
                                         "• İlk/Son yarı\n" +
                                         "• NUMA node'ları\n" +
                                         "• Özel desenler\n\n" +
                                         "💡 İpucu: Hızlı test için kullanın";

    public const string VALIDATE_BUTTON = "✅ Doğrulama\n\n" +
                                         "• CPU ID'lerini kontrol eder\n" +
                                         "• Geçersiz değerleri gösterir\n" +
                                         "• Toplam sayıyı hesaplar\n\n" +
                                         "💡 İpucu: Uygulamadan önce doğrulayın";

    public const string GENERATE_MASK_BUTTON = "🔧 Mask oluştur\n\n" +
                                              "• Seçili CPU'lardan mask oluşturur\n" +
                                              "• Hex formatında gösterir\n" +
                                              "• Kopyalayabilirsiniz\n\n" +
                                              "💡 İpucu: Manuel mask girişi için";

    // Registry Dialog Tooltip'leri
    public const string DELETE_BUTTON = "🗑️ Kayıt silme\n\n" +
                                       "• Seçili kaydı siler\n" +
                                       "• Registry'den kaldırır\n" +
                                       "• Geri alınamaz\n\n" +
                                       "⚠️ Dikkat: Kalıcı silme işlemi";

    public const string DELETE_ALL_BUTTON = "🗑️ Tümünü sil\n\n" +
                                           "• Tüm kayıtları siler\n" +
                                           "• Registry anahtarını temizler\n" +
                                           "• Geri alınamaz\n\n" +
                                           "⚠️ Dikkat: Tüm kalıcı ayarlar silinir";

    public const string EXPORT_BUTTON = "📤 Dışa aktarma\n\n" +
                                       "• Kayıtları JSON/TXT olarak kaydet\n" +
                                       "• Yedekleme için kullanın\n" +
                                       "• Başka bilgisayara taşıyın\n\n" +
                                       "💡 İpucu: Ayarlarınızı yedekleyin";

    public const string IMPORT_BUTTON = "📥 İçe aktarma\n\n" +
                                       "• JSON/TXT dosyasından yükle\n" +
                                       "• Yedekten geri yükle\n" +
                                       "• Mevcut kayıtlar üzerine yazar\n\n" +
                                       "💡 İpucu: Yedekten geri yükleme";

    // Genel Tooltip'ler
    public const string CLOSE_BUTTON = "❌ Kapat\n\n" +
                                      "• Dialog'u kapatır\n" +
                                      "• Değişiklikleri kaydetmez\n" +
                                      "• İptal işlemi\n\n" +
                                      "💡 İpucu: Değişiklikleri kaydetmek için 'Tamam' kullanın";

    public const string OK_BUTTON = "✅ Tamam\n\n" +
                                   "• Değişiklikleri onaylar\n" +
                                   "• Dialog'u kapatır\n" +
                                   "• Ayarları uygular\n\n" +
                                   "💡 İpucu: İşlemi tamamlamak için";

    public const string CANCEL_BUTTON = "❌ İptal\n\n" +
                                       "• Değişiklikleri iptal eder\n" +
                                       "• Dialog'u kapatır\n" +
                                       "• Ayarları uygulamaz\n\n" +
                                       "💡 İpucu: Değişiklikleri kaydetmek istemiyorsanız";
}
