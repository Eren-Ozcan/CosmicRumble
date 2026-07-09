using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Economy;
using CosmicRumble.Achievements;
using CosmicRumble.Localization;

/// <summary>
/// Listens to Achievement/Level-Up/Prestige/Chest/Streak reward events and
/// shows short-lived, stackable toast notifications in the corner.
/// </summary>
public class RewardPopupManager : MonoBehaviour
{
    const float ToastLifetime = 3f;
    const float ToastWidth    = 340f;
    const float ToastHeight   = 62f;
    const float ToastGap      = 8f;

    static readonly Color BgColor        = new Color(0.05f, 0.05f, 0.13f, 0.95f);
    static readonly Color AccAchievement = new Color(0.48f, 0.20f, 0.85f, 1f); // purple
    static readonly Color AccLevel       = new Color(0.22f, 0.45f, 0.95f, 1f); // blue
    static readonly Color AccChest       = new Color(1.00f, 0.80f, 0.20f, 1f); // gold
    static readonly Color AccStreak      = new Color(0.12f, 0.68f, 0.22f, 1f); // green

    Transform _root;
    readonly List<RectTransform> _activeToasts = new List<RectTransform>();

    void Awake() => BuildRoot();

    void Start()
    {
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnAchievementUnlocked += HandleAchievementUnlocked;
        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnLevelUp  += HandleLevelUp;
            PlayerLevelManager.Instance.OnPrestige += HandlePrestige;
        }
        if (ChestManager.Instance != null)
            ChestManager.Instance.OnChestGranted += HandleChestGranted;
        if (LoginStreakManager.Instance != null)
            LoginStreakManager.Instance.OnStreakRewardGranted += HandleStreakReward;
    }

    void OnDestroy()
    {
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnAchievementUnlocked -= HandleAchievementUnlocked;
        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnLevelUp  -= HandleLevelUp;
            PlayerLevelManager.Instance.OnPrestige -= HandlePrestige;
        }
        if (ChestManager.Instance != null)
            ChestManager.Instance.OnChestGranted -= HandleChestGranted;
        if (LoginStreakManager.Instance != null)
            LoginStreakManager.Instance.OnStreakRewardGranted -= HandleStreakReward;
    }

    // ── Handlers ─────────────────────────────────────────────────────────

    void HandleAchievementUnlocked(AchievementDefinition def)
    {
        string rewardLine = RewardLine(def.rewardXP, def.rewardGold, def.rewardGem);
        ShowToast(string.Format(Loc.T("Achievement: {0}"), Loc.T(def.displayName)), rewardLine, AccAchievement);
    }

    void HandleLevelUp(int oldLevel, int newLevel) =>
        ShowToast(Loc.T("Level Up!"), $"{oldLevel} → {newLevel}", AccLevel);

    void HandlePrestige(int prestigeRank) =>
        ShowToast(Loc.T("Prestige!"), string.Format(Loc.T("New prestige rank: {0}"), prestigeRank), AccLevel);

    void HandleChestGranted(ChestType type, long gold, long gem, string costumeId)
    {
        string reward = RewardLine(0, gold, gem);
        if (!string.IsNullOrEmpty(costumeId))
            reward += (reward.Length > 0 ? "  " : "") + Loc.T("+ Costume");
        ShowToast(string.Format(Loc.T("{0} Chest Opened"), type), reward, AccChest);
    }

    void HandleStreakReward(int streak, long xp, long gold, long gem) =>
        ShowToast(string.Format(Loc.T("{0}-Day Login Streak!"), streak), RewardLine(xp, gold, gem), AccStreak);

    static string RewardLine(long xp, long gold, long gem)
    {
        var parts = new List<string>();
        if (xp   > 0) parts.Add($"+{xp} XP");
        if (gold > 0) parts.Add($"+{gold} Gold");
        if (gem  > 0) parts.Add($"+{gem} Gem");
        return string.Join("   ", parts);
    }

    // ── Toast UI ─────────────────────────────────────────────────────────

    void BuildRoot()
    {
        var canvasGO = new GameObject("RewardPopupCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        _root = canvasGO.transform;
    }

    void ShowToast(string title, string detail, Color accent)
    {
        var go = new GameObject("Toast");
        go.transform.SetParent(_root, false);
        var img = go.AddComponent<Image>();
        img.color = BgColor;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(ToastWidth, ToastHeight);

        var stripGO = new GameObject("Strip");
        stripGO.transform.SetParent(go.transform, false);
        var stripImg = stripGO.AddComponent<Image>();
        stripImg.color = accent;
        stripImg.raycastTarget = false;
        var stripRt = stripImg.rectTransform;
        stripRt.anchorMin = new Vector2(0f, 0f);
        stripRt.anchorMax = new Vector2(0f, 1f);
        stripRt.pivot     = new Vector2(0f, 0.5f);
        stripRt.sizeDelta = new Vector2(5, 0);

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(go.transform, false);
        var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text = title;
        titleTxt.fontSize  = 17;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color     = Color.white;
        titleTxt.raycastTarget = false;
        var titleRt = titleTxt.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot     = new Vector2(0f, 1f);
        titleRt.offsetMin = new Vector2(16, 0);
        titleRt.offsetMax = new Vector2(-10, 0);
        titleRt.sizeDelta = new Vector2(0, 28);
        titleRt.anchoredPosition = new Vector2(0, -8);

        if (!string.IsNullOrEmpty(detail))
        {
            var detailGO = new GameObject("Detail");
            detailGO.transform.SetParent(go.transform, false);
            var detailTxt = detailGO.AddComponent<TextMeshProUGUI>();
            detailTxt.text = detail;
            detailTxt.fontSize = 14;
            detailTxt.color    = new Color(0.75f, 0.78f, 0.88f, 1f);
            detailTxt.raycastTarget = false;
            var detailRt = detailTxt.rectTransform;
            detailRt.anchorMin = new Vector2(0f, 0f);
            detailRt.anchorMax = new Vector2(1f, 0f);
            detailRt.pivot     = new Vector2(0f, 0f);
            detailRt.offsetMin = new Vector2(16, 0);
            detailRt.offsetMax = new Vector2(-10, 0);
            detailRt.sizeDelta = new Vector2(0, 24);
            detailRt.anchoredPosition = new Vector2(0, 8);
        }

        _activeToasts.Add(rt);
        ReflowToasts();
        StartCoroutine(DismissAfter(rt));
    }

    IEnumerator DismissAfter(RectTransform toast)
    {
        yield return new WaitForSeconds(ToastLifetime);
        _activeToasts.Remove(toast);
        if (toast != null) Destroy(toast.gameObject);
        ReflowToasts();
    }

    void ReflowToasts()
    {
        float y = -14f;
        for (int i = _activeToasts.Count - 1; i >= 0; i--)
        {
            var toast = _activeToasts[i];
            if (toast == null) continue;
            toast.anchoredPosition = new Vector2(-14, y);
            y -= ToastHeight + ToastGap;
        }
    }
}
