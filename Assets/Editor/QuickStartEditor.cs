#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Unity Editor menü çubuğu: Tools → CosmicRumble → ▶ Quick Start
/// Menüyü bypass ederek doğrudan SampleScene'i açar ve Play moduna girer.
/// Klavye kısayolu: Ctrl + Alt + P
/// </summary>
public static class QuickStartEditor
{
    const string GAME_SCENE = "Assets/Scenes/SampleScene.unity";
    const string MENU_ITEM  = "Tools/CosmicRumble/▶ Quick Start  %&p";   // Ctrl+Alt+P

    [MenuItem(MENU_ITEM)]
    public static void QuickStart()
    {
        if (EditorApplication.isPlaying)
        {
            // Zaten oynanıyorsa durdur
            EditorApplication.isPlaying = false;
            Debug.Log("[QuickStart] Play mode durduruldu.");
            return;
        }

        // Değiştirilmemiş sahneleri kaydet (onay iste)
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // SampleScene'i aç
        var scene = EditorSceneManager.OpenScene(GAME_SCENE, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[QuickStart] Sahne bulunamadı: {GAME_SCENE}");
            return;
        }

        Debug.Log("[QuickStart] SampleScene yüklendi → Play mode başlıyor…");
        EditorApplication.isPlaying = true;
    }

    [MenuItem(MENU_ITEM, true)]
    static bool Validate() => true;   // her zaman aktif
}
#endif
