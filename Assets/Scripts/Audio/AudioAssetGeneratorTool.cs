#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AudioAssetGeneratorTool
{
    private const string OutputFolder = "Assets/Audio/AudioAssets";

    [MenuItem("Tools/Audio/Generate Missing Audio Assets")]
    public static void GenerateMissingAudioAssets()
    {
        EnsureFolderExists(OutputFolder);

        var existingByClip = FindExistingAudioAssetsByClip();

        var clipGuids = AssetDatabase.FindAssets("t:AudioClip");
        int created = 0;
        int skipped = 0;

        foreach (var guid in clipGuids)
        {
            string clipPath = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null) continue;

            if (existingByClip.ContainsKey(clip))
            {
                skipped++;
                continue;
            }

            CreateAudioAsset(clip);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AudioAssetGeneratorTool] Created {created} new AudioAsset(s), skipped {skipped} already-wrapped clip(s).");
    }

    private static Dictionary<AudioClip, AudioAssetSO> FindExistingAudioAssetsByClip()
    {
        var map = new Dictionary<AudioClip, AudioAssetSO>();

        foreach (var guid in AssetDatabase.FindAssets("t:AudioAssetSO"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<AudioAssetSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null && asset.Clip != null)
                map.TryAdd(asset.Clip, asset);
        }

        return map;
    }

    private static void CreateAudioAsset(AudioClip clip)
    {
        var asset = ScriptableObject.CreateInstance<AudioAssetSO>();
        asset.AssignClipEditor(clip);

        string safeName = MakeSafeAssetName(clip.name);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{OutputFolder}/{safeName}.asset");

        AssetDatabase.CreateAsset(asset, path);
    }

    private static string MakeSafeAssetName(string clipName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            clipName = clipName.Replace(c, '_');

        return clipName;
    }

    private static void EnsureFolderExists(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string[] parts = folder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif