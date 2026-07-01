using UnityEngine;
using UnityEditor;
using CosmicRumble.Achievements;

public class AchievementDebugWindow : EditorWindow
{
    private AchievementDatabase _db;
    private Vector2             _scrollPos;

    [MenuItem("CosmicRumble/Achievement Debug")]
    public static void Open() => GetWindow<AchievementDebugWindow>("Achievement Debug");

    private void OnEnable()
    {
        _db = Resources.Load<AchievementDatabase>("Achievements/AchievementDatabase");
    }

    private void OnGUI()
    {
        // ── Play mode guard ───────────────────────────────────────────────────
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use this window.", MessageType.Warning);
            return;
        }

        // ── Event fire buttons ────────────────────────────────────────────────
        EditorGUILayout.LabelField("Fire Events", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Match Won"))          AchievementEvents.FireMatchWon();
        if (GUILayout.Button("Headshot"))           AchievementEvents.FireHeadshotLanded();
        if (GUILayout.Button("Planet Destroyed"))   AchievementEvents.FirePlanetDestroyed();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Damage (1000)"))      AchievementEvents.FireDamageDealt(1000);
        if (GUILayout.Button("Ability (blackhole)")) AchievementEvents.FireAbilityUsed("skill_blackhole");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // ── Achievement list ──────────────────────────────────────────────────
        EditorGUILayout.LabelField("Achievements", EditorStyles.boldLabel);

        if (_db == null)
        {
            EditorGUILayout.HelpBox("AchievementDatabase not found at Resources/Achievements/AchievementDatabase", MessageType.Error);
            if (GUILayout.Button("Retry Load"))
                _db = Resources.Load<AchievementDatabase>("Achievements/AchievementDatabase");
            return;
        }

        // Instance null ise sahnede ara (DontDestroyOnLoad objeleri dahil)
        var manager = AchievementManager.Instance
                      ?? Object.FindFirstObjectByType<AchievementManager>();

        if (manager == null)
        {
            EditorGUILayout.HelpBox(
                "AchievementManager sahnede bulunamadı.\n" +
                "Sahneye bir GameObject ekleyip AchievementManager component'ini atayın.",
                MessageType.Error);
            if (GUILayout.Button("Repaint"))
                Repaint();
            return;
        }

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        foreach (var def in _db.allAchievements)
        {
            if (def == null) continue;

            bool unlocked = manager.IsUnlocked(def.achievementId);

            EditorGUILayout.BeginHorizontal();

            // Unlocked indicator
            GUI.color = unlocked ? Color.green : Color.white;
            EditorGUILayout.LabelField(unlocked ? "✓" : " ", GUILayout.Width(16));
            GUI.color = Color.white;

            // Name + rarity
            EditorGUILayout.LabelField(
                $"[{def.rarity}] {def.displayName}",
                GUILayout.MinWidth(200));

            // ID (greyed out)
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            EditorGUILayout.LabelField(def.achievementId, GUILayout.Width(160));
            GUI.color = Color.white;

            // Unlock button — disabled if already unlocked
            EditorGUI.BeginDisabledGroup(unlocked);
            if (GUILayout.Button("Unlock", GUILayout.Width(60)))
                manager.UnlockAchievement(def.achievementId);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }
}
