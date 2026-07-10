using UnityEngine;

namespace CosmicRumble.Utilities
{
    /// <summary>Takım/oyuncu renk paleti — GravityBody.teamId ve parti lobisi rozetleri ortak
    /// kullanır. Takım modlarında teamId gerçek takımı (0-3) temsil eder; Duel1v1/Ffa'da her
    /// oyuncu kendi tekil "takımı"dır (0..N-1) — palet her iki durumda da aynı şekilde
    /// döngüsel uygulanır, ayrı bir kod yolu gerekmez.</summary>
    public static class TeamColors
    {
        static readonly Color[] Palette =
        {
            new Color(0.25f, 0.55f, 1.00f), // mavi
            new Color(1.00f, 0.30f, 0.30f), // kırmızı
            new Color(0.30f, 0.85f, 0.40f), // yeşil
            new Color(1.00f, 0.80f, 0.20f), // sarı
            new Color(0.85f, 0.35f, 0.95f), // mor
            new Color(1.00f, 0.55f, 0.15f), // turuncu
            new Color(0.30f, 0.90f, 0.90f), // camgöbeği
            new Color(0.95f, 0.55f, 0.75f), // pembe
        };

        public static Color Get(int teamId) =>
            Palette[((teamId % Palette.Length) + Palette.Length) % Palette.Length];
    }
}
