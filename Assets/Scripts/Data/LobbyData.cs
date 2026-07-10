using System.Collections.Generic;
using CosmicRumble.Data;

/// <summary>
/// MenuScene → GameScene arasında lobi ayarlarını taşır.
/// </summary>
public static class LobbyData
{
    public static int    BotCount = 0;
    public static string MapName  = "CosmicArena";

    /// <summary>Seçili maç modu — TurnManager'ın kazanma şartını ve spawner'ların takım
    /// atamasını belirler. Varsayılan Duel1v1 (mevcut hotseat/1v1 akışlarıyla geriye dönük uyumlu).</summary>
    public static GameModeType SelectedMode = GameModeType.Duel1v1;

    /// <summary>Yalnızca SelectedMode==Ffa iken anlamlı — host'un seçtiği toplam oyuncu sayısı (3-8).</summary>
    public static int FfaPlayerCount = GameModeCatalog.MinFfaPlayers;

    /// <summary>Parti lobisindeki oyuncular (host dahil) — online parti/takım modlarında
    /// PartyLobbyPanelUI tarafından doldurulur, NetworkPlayerSpawner clientId→teamId eşlemesi
    /// için PlayerId'yi kullanır. Offline hotseat/Antrenman'da boş kalır (GameInitializer kendi
    /// bot atamasını yapar).</summary>
    public static List<PartyMemberSlot> PartyMembers = new List<PartyMemberSlot>();

    /// <summary>Antrenman modu: botlar TurnManager.characters'a hiç eklenmez (isActive hep false
    /// kalır, hiçbir zaman hareket/ateş etmez — sadece hedef), maç oyuncu sayısı 1 diye bitmez.</summary>
    public static bool IsTraining = false;

    /// <summary>Bu maç bir arkadaş daveti (FriendLobbyPanelUI) ile mi kuruldu — arkadaşın PlayerId'si.
    /// Quick Match/hotseat'te null. TurnManager.FinishMatchLocally KOZMIK_EKIP için okuyup temizler.</summary>
    public static string FriendOpponentId = null;
}
