using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using CosmicRumble.Achievements;

[System.Serializable]
public class UserEntry
{
    public string username;
    public string passwordHash;
    public string createdAt;
}

[System.Serializable]
public class UserDatabase
{
    public List<UserEntry> users = new List<UserEntry>();
}

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    public bool          IsLoggedIn      { get; private set; }
    public bool          IsGuest         { get; private set; }
    public string        CurrentUsername { get; private set; }
    public PlayerProfile CurrentProfile  { get; private set; }

    private string       _dbPath;
    private UserDatabase _db;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _dbPath = Path.Combine(Application.persistentDataPath, "users.json");
        LoadDatabase();
    }

    // ── Persistence ──────────────────────────────────────────────────────

    void LoadDatabase()
    {
        if (File.Exists(_dbPath))
        {
            try
            {
                string json = File.ReadAllText(_dbPath);
                _db = JsonUtility.FromJson<UserDatabase>(json);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[AuthManager] users.json okunamadı: {e.Message}");
#endif
            }
        }

        if (_db == null) _db = new UserDatabase();
    }

    void SaveDatabase()
    {
        try
        {
            File.WriteAllText(_dbPath, JsonUtility.ToJson(_db, true));
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.LogError($"[AuthManager] users.json kaydedilemedi: {e.Message}");
#endif
        }
    }

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>Yeni kullanıcı kaydeder. Başarılı → true, kullanıcı zaten varsa → false.</summary>
    public bool Register(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        if (_db.users.Exists(u =>
            string.Equals(u.username, username, StringComparison.OrdinalIgnoreCase)))
            return false;

        _db.users.Add(new UserEntry
        {
            username     = username.Trim(),
            passwordHash = HashPassword(password),
            createdAt    = DateTime.UtcNow.ToString("o")
        });

        SaveDatabase();
        return true;
    }

    /// <summary>Giriş yapar. Başarılıysa profili yükler ve true döner.</summary>
    public bool Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        var entry = _db.users.Find(u =>
            string.Equals(u.username, username, StringComparison.OrdinalIgnoreCase));

        if (entry == null) return false;
        if (entry.passwordHash != HashPassword(password)) return false;

        IsLoggedIn      = true;
        CurrentUsername = entry.username;

        CurrentProfile = PlayerProfile.Load(entry.username) ?? new PlayerProfile { username = entry.username };
        CurrentProfile.lastLogin = DateTime.UtcNow.ToString("o");
        CurrentProfile.Save();

        AchievementManager.Instance?.LoadForUser(entry.username);

        return true;
    }

    /// <summary>Hesap açmadan misafir olarak oynar. Profil kaydedilmez.</summary>
    public void LoginAsGuest()
    {
        IsLoggedIn      = true;
        IsGuest         = true;
        CurrentUsername = "Misafir";
        CurrentProfile  = null;
        AchievementManager.Instance?.LoadForUser(null);
    }

    /// <summary>Oturumu kapatır ve profili kaydeder.</summary>
    public void Logout()
    {
        CurrentProfile?.Save();
        IsLoggedIn      = false;
        IsGuest         = false;
        CurrentUsername = null;
        CurrentProfile  = null;

        AchievementManager.Instance?.LoadForUser(null);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        var sb = new StringBuilder(64);
        foreach (byte b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
