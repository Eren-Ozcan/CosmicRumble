/// <summary>
/// MenuScene → GameScene arasında lobi ayarlarını taşır.
/// Photon gelene kadar tüm değerler local.
/// </summary>
public static class LobbyData
{
    public static int    BotCount = 0;
    public static string MapName  = "CosmicArena";
    public static string GameMode = "Deathmatch";

    /// <summary>Antrenman modu: botlar TurnManager.characters'a hiç eklenmez (isActive hep false
    /// kalır, hiçbir zaman hareket/ateş etmez — sadece hedef), maç oyuncu sayısı 1 diye bitmez.</summary>
    public static bool IsTraining = false;

    /// <summary>Bu maç bir arkadaş daveti (FriendLobbyPanelUI) ile mi kuruldu — arkadaşın PlayerId'si.
    /// Quick Match/hotseat'te null. TurnManager.FinishMatchLocally KOZMIK_EKIP için okuyup temizler.</summary>
    public static string FriendOpponentId = null;
}
