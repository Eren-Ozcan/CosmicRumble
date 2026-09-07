// Assets/Scripts/UI/UiTheme.cs
using UnityEngine;

/// <summary>
/// Tek kaynak: "CosmicRumble UI Kit" tasarım dosyasındaki palet, geometri ve tipografi ölçeği.
/// Menü ve panel scriptleri kendi renk sabitlerini burada tanımlı değerlere bağlar; böylece
/// tasarımda bir renk değişince tek dosya güncellenir ve ekranlar arası tutarsızlık oluşmaz.
///
/// Hex değerleri tasarım dosyasından birebir alınmıştır (bkz. docs/ui-design-kit.md).
/// </summary>
public static class UiTheme
{
    // ── Yardımcı ─────────────────────────────────────────────────────────────
    /// <summary>0xRRGGBB sabitinden Color üretir (alfa ayrı verilir).</summary>
    public static Color Hex(int rgb, float a = 1f) => new Color(
        ((rgb >> 16) & 0xFF) / 255f,
        ((rgb >> 8) & 0xFF) / 255f,
        (rgb & 0xFF) / 255f,
        a);

    // ── Zemin / uzay ─────────────────────────────────────────────────────────
    public static readonly Color BgDeep      = Hex(0x0A081A);       // en dış uzay zemini
    public static readonly Color BgSpaceTop  = Hex(0x3A1F7A);       // radyal gradyanın merkezi
    public static readonly Color BgSpaceMid  = Hex(0x1B1140);
    public static readonly Color MenuGradTop = Hex(0x2B1A66);       // menü/giriş dikey gradyanı
    public static readonly Color MenuGradBot = Hex(0x571A54);
    public static readonly Color Backdrop    = Hex(0x060510, 0.72f); // modal arkası karartma

    // ── Yüzeyler ─────────────────────────────────────────────────────────────
    public static readonly Color Card      = Hex(0x121229, 0.98f);  // panel/modal gövdesi
    public static readonly Color CardDeep  = Hex(0x0F0F22, 0.98f);  // iç bölüm kutusu
    public static readonly Color Row       = Hex(0x1C1C36);         // liste satırı
    public static readonly Color RowAlt    = Hex(0x212140);         // zebra satır
    public static readonly Color Plate     = Hex(0x2A2C37);         // chunky plaka gövdesi
    public static readonly Color PlateEdge = Hex(0x16171D);         // plakanın alt kalınlığı
    public static readonly Color Slot      = Hex(0x26264A);         // envanter slotu / input zemini
    public static readonly Color SlotAlt   = Hex(0x26243A);
    public static readonly Color Locked    = Hex(0x1F1F29, 0.85f);  // seviye kilidi kaplaması
    public static readonly Color Stroke    = new Color(1f, 1f, 1f, 0.09f);
    public static readonly Color Separator = Hex(0x404066, 0.60f);

    // ── Aksan renkleri (gövde + alt kenar çifti) ────────────────────────────
    public static readonly Color Gold       = Hex(0xFDC91A);
    public static readonly Color GoldEdge   = Hex(0xB88005);
    public static readonly Color GoldBright = Hex(0xFFB800);

    public static readonly Color Blue       = Hex(0x4A9EFF);
    public static readonly Color BlueEdge   = Hex(0x2A5296);
    public static readonly Color BlueDeep   = Hex(0x22598F);

    public static readonly Color Green      = Hex(0x22B859);
    public static readonly Color GreenEdge  = Hex(0x0F6E35);
    public static readonly Color GreenBright= Hex(0x44FF88);

    public static readonly Color Danger     = Hex(0xB82E2E);
    public static readonly Color DangerEdge = Hex(0x6B1616);
    public static readonly Color DangerLight= Hex(0xFF5959);

    public static readonly Color CloseRed     = Hex(0xDB383F);
    public static readonly Color CloseRedEdge = Hex(0x8E1F25);

    public static readonly Color Purple = Hex(0xAB45FF);
    public static readonly Color Cyan   = Hex(0x26B3BF);
    public static readonly Color Pink   = Hex(0xE7539E);

    // ── Metin ────────────────────────────────────────────────────────────────
    /// <summary>Sari/acik zemin uzerindeki koyu yazi (tasarim: PLAY etiketi, "+" rozeti).</summary>
    public static readonly Color Ink         = Hex(0x26243A);
    public static readonly Color TextPrimary = Hex(0xEDEBF6);
    public static readonly Color TextDim     = Hex(0xA6B3D1);
    public static readonly Color TextMuted   = Hex(0x8A8FB0);
    public static readonly Color TextFaint   = Hex(0x6E7396);
    public static readonly Color NameBlue    = Hex(0x73CCFF);
    public static readonly Color LinkHover   = Hex(0xA9E1FF);
    public static readonly Color IceBlue     = Hex(0x8FD0FF);

    // ── Para birimi ──────────────────────────────────────────────────────────
    public static readonly Color GoldChip = Hex(0xFDC91A);
    public static readonly Color GemChip  = Hex(0x8FD0FF);
    public static readonly Color XpBar    = Hex(0x4A9EFF);

    // ── Nadirlik ─────────────────────────────────────────────────────────────
    public static readonly Color RarityCommon    = Hex(0xABABAB);
    public static readonly Color RarityUncommon  = Hex(0x5AE06B);
    public static readonly Color RarityRare      = Hex(0x4A9EFF);
    public static readonly Color RarityEpic      = Hex(0xAB45FF);
    public static readonly Color RarityLegendary = Hex(0xFFB800);

    // ── Çevrimiçi durumu ─────────────────────────────────────────────────────
    public static readonly Color StatusOnline  = Hex(0x44FF88);
    public static readonly Color StatusInMatch = Hex(0xFDC91A);
    public static readonly Color StatusAway    = Hex(0xC08A2A);
    public static readonly Color StatusOffline = Hex(0x6E7396);

    // ── Envanter slot durumları (UIManager filtre kaplamaları) ──────────────
    public static readonly Color SlotSelected  = new Color(1f, 0.79f, 0.06f, 0.45f);  // sarı
    public static readonly Color SlotConfirmed = new Color(0.13f, 0.72f, 0.35f, 0.50f); // yeşil
    public static readonly Color SlotEmpty     = new Color(0.72f, 0.18f, 0.18f, 0.55f); // kırmızı
    public static readonly Color SlotLocked    = Locked;
    public static readonly Color SlotNone      = new Color(0f, 0f, 0f, 0f);

    // ── Geometri ─────────────────────────────────────────────────────────────
    // UiKit.Round(cornerScale): DEĞER BÜYÜDÜKÇE köşe yarıçapı KÜÇÜLÜR.
    public const float CornerCard  = 1.0f;   // ~r22 modal/panel
    public const float CornerPlate = 1.4f;   // ~r16 plaka/buton
    public const float CornerRow   = 1.8f;   // ~r14 satır
    public const float CornerChip  = 2.6f;   // ~r10 çip/rozet

    /// <summary>Chunky plakaların alt "kalınlık" şeridi (px).</summary>
    public const float PlateEdgeHeight = 6f;

    /// <summary>
    /// Dokunmatikte güvenli minimum buton kenarı — canvas'ın 1080 birimlik kısa kenarına göre.
    /// Tasarım kitindeki (960x540 artboard) en küçük tıklanabilir öğe 34-40px, yani 68-80
    /// tasarım birimi; eşik bunun alt sınırına yakın tutulur.
    /// </summary>
    public const float MinTouchSize = 72f;

    // ── Tipografi ölçeği ─────────────────────────────────────────────────────
    public const float FontScreenTitle = 34f;
    public const float FontPanelTitle  = 30f;
    public const float FontButtonBig   = 24f;
    public const float FontButton      = 19f;
    public const float FontBody        = 15f;
    public const float FontBodySmall   = 14f;
    public const float FontHelper      = 12f;
    public const float FontMicro       = 11f;
}
