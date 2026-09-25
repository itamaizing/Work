using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Sfx_Skill
{
    PrepareStart,
    PrepareLoop,
    PrepareEnd,
    CastStart,
    CastLoop,
    CastEnd,
    Custom
}

[Serializable]
public class AudioKeyList
{
    public List<string> Keys = new();
}

[Serializable]
public class SfxEntry
{
    public Sfx_Skill Type;
    public AudioKeyList Clips = new();
}

[Serializable]
public class SoundComponent : BaseSkillComponent
{
    private static readonly List<string> EmptyKeys = new();

    [SerializeField] private SoundDatabase _database;
    [SerializeField] private List<SfxEntry> _entries = new();

    public List<string> this[Sfx_Skill type] => GetEntry(type)?.Clips.Keys ?? EmptyKeys;

    public AudioAssetSO Get(Sfx_Skill type, int? minId = null, int? maxId = null)
    {
        if (_database == null)
        {
            Debug.LogWarning("[SoundComponent] SoundDatabase is not assigned.");
            return null;
        }

        var entry = GetEntry(type);
        if (entry == null || entry.Clips.Keys.Count == 0)
            return null;

        if (!minId.HasValue)
            return Resolve(entry.Clips.Keys[0]);

        int upper = maxId.HasValue ? Mathf.Min(maxId.Value, entry.Clips.Keys.Count - 1) : minId.Value;
        int lower = Mathf.Min(minId.Value, upper);

        int index = lower == upper ? lower : UnityEngine.Random.Range(lower, upper + 1);
        index = Mathf.Clamp(index, 0, entry.Clips.Keys.Count - 1);

        return Resolve(entry.Clips.Keys[index]);
    }

    public AudioAssetSO GetRandom(Sfx_Skill type) => Get(type, 0, int.MaxValue);

    public AudioData BuildAudioData(AudioAssetSO asset, SfxPlayMode mode = SfxPlayMode.Once, bool followCaster = true)
    {
        var data = new AudioData { ClipHash = asset.Hash, PlayMode = mode };

        if (followCaster && _character != null)
        {
            data.FollowTargetNetId = _character.netId;
        }
        else if (_character != null)
        {
            data.HasPosition = true;
            data.Position = _character.transform.position;
        }

        return data;
    }

    private SfxEntry GetEntry(Sfx_Skill type) => _entries.FirstOrDefault(e => e.Type == type);

#if UNITY_EDITOR
    private void OnValidate()
    {
        var allTypes = (Sfx_Skill[])Enum.GetValues(typeof(Sfx_Skill));

        foreach (var type in allTypes)
        {
            if (_entries.All(e => e.Type != type))
                _entries.Add(new SfxEntry { Type = type });
        }

        _entries.RemoveAll(e => allTypes.Contains(e.Type) == false);
        _entries = _entries.OrderBy(e => Array.IndexOf(allTypes, e.Type)).ToList();
    }
#endif

    private AudioAssetSO Resolve(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        int hash = Animator.StringToHash(key);
        if (_database.TryGet(hash, out var asset))
            return asset;

        Debug.LogWarning($"[SoundComponent] Clip with key '{key}' not found in database.");
        return null;
    }
}