using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CosmicRumble.Economy;

public class AvatarAssetGenerator
{
    [MenuItem("CosmicRumble/Economy/Generate Avatar Assets")]
    public static void GenerateAll()
    {
        var avatars = GenerateAvatarDefinitions();
        GenerateAvatarDatabase(avatars);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AvatarAssetGenerator] Done — {avatars.Count} avatars generated.");
    }

    private static List<AvatarDefinition> GenerateAvatarDefinitions()
    {
        const string dir = "Assets/Resources/Avatars";
        EnsureDir(dir);

        // (id, name, color) — placeholder color+letter until real icon art exists.
        var defs = new List<(string id, string name, Color color)>
        {
            ("av01", "Nova",     new Color(0.95f, 0.30f, 0.35f, 1f)),
            ("av02", "Comet",    new Color(0.25f, 0.70f, 0.95f, 1f)),
            ("av03", "Blaze",    new Color(1.00f, 0.60f, 0.15f, 1f)),
            ("av04", "Nebula",   new Color(0.65f, 0.35f, 1.00f, 1f)),
            ("av05", "Pulsar",   new Color(0.20f, 0.85f, 0.55f, 1f)),
            ("av06", "Quasar",   new Color(1.00f, 0.80f, 0.20f, 1f)),
            ("av07", "Meteor",   new Color(0.85f, 0.25f, 0.55f, 1f)),
            ("av08", "Orbit",    new Color(0.30f, 0.55f, 0.95f, 1f)),
            ("av09", "Solstice", new Color(0.95f, 0.45f, 0.20f, 1f)),
            ("av10", "Eclipse",  new Color(0.40f, 0.40f, 0.48f, 1f)),
            ("av11", "Vortex",   new Color(0.20f, 0.75f, 0.80f, 1f)),
            ("av12", "Cosmos",   new Color(0.55f, 0.20f, 0.85f, 1f)),
            ("av13", "Photon",   new Color(1.00f, 0.90f, 0.30f, 1f)),
            ("av14", "Asteroid", new Color(0.60f, 0.62f, 0.68f, 1f)),
            ("av15", "Aurora",   new Color(0.30f, 0.90f, 0.70f, 1f)),
            ("av16", "Zenith",   new Color(0.90f, 0.30f, 0.80f, 1f)),
        };

        var result = new List<AvatarDefinition>();
        foreach (var (id, name, color) in defs)
        {
            string path = $"{dir}/{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<AvatarDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<AvatarDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.avatarId         = id;
            def.displayName      = name;
            def.placeholderColor = color;
            EditorUtility.SetDirty(def);
            result.Add(def);
        }
        return result;
    }

    private static void GenerateAvatarDatabase(List<AvatarDefinition> avatars)
    {
        const string dir  = "Assets/Resources/Economy";
        const string path = dir + "/AvatarDatabase.asset";
        EnsureDir(dir);

        var db = AssetDatabase.LoadAssetAtPath<AvatarDatabase>(path);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<AvatarDatabase>();
            AssetDatabase.CreateAsset(db, path);
        }
        db.allAvatars = avatars;
        EditorUtility.SetDirty(db);
        Debug.Log("[AvatarAssetGenerator] AvatarDatabase.asset ready.");
    }

    private static void EnsureDir(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
