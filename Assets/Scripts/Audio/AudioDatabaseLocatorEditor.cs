#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Находит единственную SoundDatabase проекта, кэшируя GUID в EditorPrefs,
/// чтобы AudioAssetSO.OnValidate не сканировал AssetDatabase на каждый чих.
/// </summary>
public static class AudioDatabaseLocatorEditor
{
    private const string CachedGuidKey = "Audio_SoundDatabase_GUID";

    public static SoundDatabase FindDatabase()
    {
        string cachedGuid = EditorPrefs.GetString(CachedGuidKey, string.Empty);

        if (!string.IsNullOrEmpty(cachedGuid))
        {
            string cachedPath = AssetDatabase.GUIDToAssetPath(cachedGuid);
            var cached = AssetDatabase.LoadAssetAtPath<SoundDatabase>(cachedPath);
            if (cached != null) return cached;
        }

        var guids = AssetDatabase.FindAssets("t:SoundDatabase");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[AudioAssetSO] No SoundDatabase found in project. Create one via Assets > Create > Audio > Sound Database.");
            return null;
        }

        if (guids.Length > 1)
            Debug.LogWarning($"[AudioAssetSO] Multiple SoundDatabase assets found ({guids.Length}). Using the first one. This system assumes a single database.");

        EditorPrefs.SetString(CachedGuidKey, guids[0]);
        return AssetDatabase.LoadAssetAtPath<SoundDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }
}
#endif