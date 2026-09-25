using UnityEngine;
using UnityEngine.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "AudioAsset", menuName = "Audio/Audio Asset")]
public class AudioAssetSO : ScriptableObject
{
    [SerializeField] private AudioClip _clip;
    [SerializeField] private float _baseVolume = 1f;
    [SerializeField] private AudioMixerGroup _mixerGroup;
    [SerializeField, HideInInspector] private string _key;

    public AudioClip Clip => _clip;
    public float BaseVolume => _baseVolume;
    public AudioMixerGroup MixerGroup => _mixerGroup;
    public string Key => _key;
    public int Hash => Animator.StringToHash(_key);

#if UNITY_EDITOR
    private string _lastKnownKey;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(_key))
            _key = name;

        // Если ассет переименовали (значит и _key должен обновиться до следующего сохранения),
        // не пересобираем автоматически — переименование обрабатывается через OnAssetRenamed ниже,
        // чтобы избежать рассинхрона хэша с уже сохранёнными ссылками в базе.
        if (_key != _lastKnownKey)
        {
            _lastKnownKey = _key;
            EditorApplication.delayCall += TryRegisterSelf;
        }
    }

    private void TryRegisterSelf()
    {
        if (this == null) return; // ассет мог быть удалён к моменту delayCall

        var database = AudioDatabaseLocatorEditor.FindDatabase();
        if (database == null) return;

        if (database.RegisterAssetEditor(this))
            Debug.Log($"[AudioAssetSO] Auto-registered '{name}' in {database.name}.");
    }
    
    public void AssignClipEditor(AudioClip clip)
    {
        _clip = clip;
        _key = clip.name;
        EditorUtility.SetDirty(this);
    }
#endif

    /// <summary>Вызывается инструментом БД при обнаружении коллизии хэшей.</summary>
    public void SetKey(string newKey)
    {
        _key = newKey;
    }
}