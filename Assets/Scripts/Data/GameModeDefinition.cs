using System.Collections.Generic;
using UnityEngine;

namespace CosmicRumble.Data
{
    /// <summary>Desteklenen maç modları. Ffa'da gerçek oyuncu sayısı sabit değil — host
    /// LobbyData.FfaPlayerCount ile 3-8 arası seçer; diğer modlarda GameModeCatalog'daki
    /// TotalPlayers sabittir. Lobi kapasitesi max 8 ile sınırlı — 3v3v3 (9 gerektirirdi)
    /// bu yüzden desteklenmiyor.</summary>
    public enum GameModeType
    {
        Duel1v1,
        Ffa,
        Team2v2,
        Team3v3,
        Team4v4,
        Team2v2v2v2,
    }

    /// <summary>Bir modun sabit yapısı. TeamCount==0 → takımsız (Duel1v1/Ffa), her karakter
    /// kendi başına oynar ve TurnManager'ın kazanma şartında kendi teamId'si tekildir.</summary>
    public readonly struct GameModeDefinition
    {
        public readonly GameModeType Type;
        public readonly string       DisplayName; // Loc.T ile çevrilecek İngilizce anahtar
        public readonly int          TotalPlayers; // Ffa için minimum; gerçek sayı LobbyData.FfaPlayerCount
        public readonly int          TeamCount;    // 0 = takımsız
        public readonly int          TeamSize;     // 0 = takımsız

        public bool IsTeamMode => TeamCount > 0;

        public GameModeDefinition(GameModeType type, string displayName, int totalPlayers, int teamCount, int teamSize)
        {
            Type         = type;
            DisplayName  = displayName;
            TotalPlayers = totalPlayers;
            TeamCount    = teamCount;
            TeamSize     = teamSize;
        }
    }

    public static class GameModeCatalog
    {
        public const int MinFfaPlayers = 3;
        public const int MaxFfaPlayers = 8;
        public const int MaxLobbySize  = 8; // projedeki en büyük mod (4v4 / 2v2v2v2 / Ffa-8)

        public static readonly Dictionary<GameModeType, GameModeDefinition> All = new()
        {
            { GameModeType.Duel1v1,     new GameModeDefinition(GameModeType.Duel1v1,     "1v1",           2, 0, 0) },
            { GameModeType.Ffa,         new GameModeDefinition(GameModeType.Ffa,         "Free-For-All",  MinFfaPlayers, 0, 0) },
            { GameModeType.Team2v2,     new GameModeDefinition(GameModeType.Team2v2,     "2v2",           4, 2, 2) },
            { GameModeType.Team3v3,     new GameModeDefinition(GameModeType.Team3v3,     "3v3",           6, 2, 3) },
            { GameModeType.Team4v4,     new GameModeDefinition(GameModeType.Team4v4,     "4v4",           8, 2, 4) },
            { GameModeType.Team2v2v2v2, new GameModeDefinition(GameModeType.Team2v2v2v2, "2v2v2v2",       8, 4, 2) },
        };

        /// <summary>Ffa için gerçek (host'un seçtiği) oyuncu sayısını, diğer modlarda sabit
        /// TotalPlayers'ı döner.</summary>
        public static int ResolveTotalPlayers(GameModeType type, int ffaPlayerCount)
            => type == GameModeType.Ffa
                ? Mathf.Clamp(ffaPlayerCount, MinFfaPlayers, MaxFfaPlayers)
                : All[type].TotalPlayers;
    }

    /// <summary>Parti lobisindeki tek bir slot — hem online (gerçek arkadaş/UGS PlayerId) hem
    /// offline hotseat (bot) için ortak model.</summary>
    [System.Serializable]
    public class PartyMemberSlot
    {
        public string PlayerId;
        public string DisplayName;
        public int    TeamId;   // 0-tabanlı; yalnızca IsTeamMode modlarında anlamlı
        public bool   IsBot;
    }
}
