using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

// Produces a Windows development build used only as a standalone second
// process for testing multiplayer connectivity locally (Claude drives the
// Editor as host via MCP; this build runs as the second client, since a
// second Editor window isn't available for MCP-driven testing).
public static class BuildScript
{
    public static void BuildDevClient()
    {
        string[] scenePaths = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        var options = new BuildPlayerOptions
        {
            scenes = scenePaths,
            locationPathName = "Builds/DevClient/CosmicRumble.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        UnityEngine.Debug.Log($"[DevBuildScript] Build result: {report.summary.result}, errors={report.summary.totalErrors}, size={report.summary.totalSize}");
    }
}
