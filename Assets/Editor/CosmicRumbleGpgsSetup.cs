using UnityEditor;
using UnityEngine;

/// <summary>One-shot helper that runs the Google Play Games Android setup with the IDs the
/// Play Console generated for this game, so the setup window does not have to be filled in by hand.
/// Safe to delete once the generated GameInfo.cs and the manifest resource are committed.</summary>
public static class CosmicRumbleGpgsSetup
{
    const string AppId       = "4377355611";
    const string WebClientId = "4377355611-i89p0s2fh46gk30pp8kmrk31vqdsipcn.apps.googleusercontent.com";

    [MenuItem("Tools/Android/Run Play Games setup")]
    public static void Execute()
    {
        bool ok = GooglePlayGames.Editor.GPGSAndroidSetupUI.PerformSetup(WebClientId, AppId, null);
        Debug.Log($"[CosmicRumbleGpgsSetup] PerformSetup returned {ok}");
        AssetDatabase.Refresh();
    }
}
