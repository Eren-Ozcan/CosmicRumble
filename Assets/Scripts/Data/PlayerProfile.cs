using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Oyuncu ilerleme verisi. ScriptableObject değil — sade serializable class.
/// Dosya: Application.persistentDataPath/profiles/&lt;username&gt;.json
/// </summary>
[System.Serializable]
public class PlayerProfile
{
    public string username;
    public int    level            = 1;
    public long   totalXP;
    public int    matchesPlayed;
    public int    matchesWon;
    public int    totalDamageDealt;
    public int    totalShotsFired;
    public string lastLogin;   // ISO 8601

    // ── Persistence ──────────────────────────────────────────────────────

    static string ProfilesDir =>
        Path.Combine(Application.persistentDataPath, "profiles");

    static string GetPath(string user) =>
        Path.Combine(ProfilesDir, user + ".json");

    /// <summary>Profili dosyadan yükler. Yoksa null döner.</summary>
    public static PlayerProfile Load(string username)
    {
        string path = GetPath(username);
        if (!File.Exists(path)) return null;

        try
        {
            return JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[PlayerProfile] {username}.json okunamadı: {e.Message}");
#endif
            return null;
        }
    }

    /// <summary>Profili diske kaydeder.</summary>
    public void Save()
    {
        try
        {
            if (!Directory.Exists(ProfilesDir))
                Directory.CreateDirectory(ProfilesDir);

            File.WriteAllText(GetPath(username), JsonUtility.ToJson(this, true));
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.LogError($"[PlayerProfile] {username}.json kaydedilemedi: {e.Message}");
#endif
        }
    }
}
