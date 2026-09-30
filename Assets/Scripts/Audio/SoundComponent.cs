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
    Custom,
}

/*[Serializable] // Probably To Delete
public class AudioKeyList
{
    public List<AudioAssetSO> Keys = new();
}*/

[Serializable]
#if UNITY_EDITOR
public class SfxEntry<T> : ISerializationCallbackReceiver where T : Enum
{
    [HideInInspector, SerializeField] public string nameToShow;

    public void OnBeforeSerialize() { nameToShow = Type.ToString(); }
    public void OnAfterDeserialize() { }

#else
public class SfxEntry<T> where T : Enum
{
#endif
    public T Type;
    public List<AudioAssetSO> Clips = new();
}

[Serializable]public abstract class SoundComponentBase { }

[Serializable]
public class SoundComponent<T> where T : Enum
{
    private static readonly List<AudioAssetSO> EmptyKeys = new();

    [SerializeField] private List<SfxEntry<T>> _entries = new();

    public List<AudioAssetSO> this[T type] => GetEntry(type)?.Clips ?? EmptyKeys;

    public AudioAssetSO Get(T type, int? minId = null, int? maxId = null)
    {
        var entry = GetEntry(type);
        if (entry == null || entry.Clips.Count == 0)
            return null;

        if (!minId.HasValue)
            return GetRandom(type);

        int upper = maxId.HasValue ? Mathf.Min(maxId.Value, entry.Clips.Count - 1) : minId.Value;
        int lower = Mathf.Min(minId.Value, upper);

        int index = lower == upper ? lower : UnityEngine.Random.Range(lower, upper + 1);
        index = Mathf.Clamp(index, 0, entry.Clips.Count - 1);

        return entry.Clips[index]; //Resolve(entry.Clips.Keys[index]);
    }

    public AudioAssetSO GetRandom(T type) => Get(type, 0, int.MaxValue);

    public AudioData BuildAudioData(AudioAssetSO asset, uint? target = null, Vector3? pos = null, SfxPlayMode mode = SfxPlayMode.Once)
    {
        var data = new AudioData { ClipHash = asset.Hash, PlayMode = mode };

        if (target.HasValue)
        {
            Debug.Log("Sound should follow");
            data.FollowTargetNetId = target.Value;
        }
        else if (pos.HasValue)
        {
            Debug.Log("Sound is positioned");
            data.HasPosition = true;
            data.Position = pos.Value;
        }

        return data;
    }

    private SfxEntry<T> GetEntry(T type) => _entries.FirstOrDefault(e => EqualityComparer<T>.Default.Equals(e.Type, type));

#if UNITY_EDITOR
    private void OnValidate()
    {
        var allTypes = (T[])Enum.GetValues(typeof(T));

        foreach (var type in allTypes)
        {
            if (_entries.All(e => !EqualityComparer<T>.Default.Equals(e.Type, type)))
                _entries.Add(new SfxEntry<T> { Type = type });
        }

        _entries.RemoveAll(e => allTypes.Contains(e.Type) == false);
        _entries = _entries.OrderBy(e => Array.IndexOf(allTypes, e.Type)).ToList();
    }
#endif

    private AudioAssetSO Resolve(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        int hash = Animator.StringToHash(key);
        if (AudioManager.Database.TryGet(hash, out var asset))
        {
            Debug.Log($"Successfully found {key} sfx");
            return asset;
        }

        Debug.LogWarning($"[SoundComponent] Clip with key '{key}' not found in database.");
        return null;
    }
}