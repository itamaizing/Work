using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Audio/Sound Database")]
public class SoundDatabase : ScriptableObject
{
    [SerializeField] private List<AudioAssetSO> _assets = new();

    private Dictionary<int, AudioAssetSO> _lookup;

    public bool TryGet(int hash, out AudioAssetSO asset)
    {
        BuildLookupIfNeeded();
        return _lookup.TryGetValue(hash, out asset);
    }

    private void BuildLookupIfNeeded()
    {
        if (_lookup != null) return;

        _lookup = new Dictionary<int, AudioAssetSO>();
        foreach (var asset in _assets)
        {
            if (asset == null) continue;

            if (!_lookup.TryAdd(asset.Hash, asset))
                Debug.LogError($"[SoundDatabase] Hash collision: '{asset.name}' and '{_lookup[asset.Hash].name}' share key '{asset.Key}'. Rename one of them.");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Populate From Project")]
    public void PopulateFromProjectEditor()
    {
        _assets.Clear();

        foreach (var guid in AssetDatabase.FindAssets("t:AudioAssetSO"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<AudioAssetSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) _assets.Add(asset);
        }

        _lookup = null;
        EditorUtility.SetDirty(this);
        Debug.Log($"[SoundDatabase] Populated {_assets.Count} audio assets.");
    }
    
    public bool RegisterAssetEditor(AudioAssetSO asset)
    {
        if (asset == null) return false;
        if (_assets.Contains(asset)) return false;

        _assets.Add(asset);
        _lookup = null;
        EditorUtility.SetDirty(this);
        return true;
    }

    public bool UnregisterAssetEditor(AudioAssetSO asset)
    {
        if (asset == null) return false;
        if (!_assets.Remove(asset)) return false;

        _lookup = null;
        EditorUtility.SetDirty(this);
        return true;
    }
    
    public IReadOnlyList<AudioAssetSO> Assets => _assets;
    public string[] GetAllKeys()
    {
        return _assets
            .Where(a => a != null && !string.IsNullOrEmpty(a.Key))
            .Select(a => a.Key)
            .Distinct()
            .OrderBy(k => k)
            .ToArray();
    }
#endif
}