using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CreatureIcon : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private SpawnType _spawnType;
    [SerializeField] private CreatureSpawn _creatureSpawn;

    private Quaternion _rotation;

    private void Awake()
    {
        _button.onClick.AddListener(OnClick);
        _creatureSpawn.TargetingSuccess += OnCastStarted;
        _creatureSpawn.CastEnded += OnCanceled;
        _creatureSpawn.Canceled += OnCanceled;
        _creatureSpawn.UnlockedTypesChanged += OnUnlockedChanged;

        gameObject.SetActive(false);
        _rotation = transform.rotation;
    }

    private void OnEnable()
    {
        transform.Rotate(Vector3.up, 180);

        transform.DOLocalRotate(_rotation.eulerAngles, 1);
    }

    private void OnDestroy()
    {
        _creatureSpawn.TargetingSuccess -= OnCastStarted;
        _creatureSpawn.CastEnded -= OnCanceled;
        _creatureSpawn.Canceled -= OnCanceled;
        _creatureSpawn.UnlockedTypesChanged -= OnUnlockedChanged;
    }


    private void OnClick()
    {
        if (!_creatureSpawn.IsSpawnTypeUnlocked(_spawnType)) return;
        _creatureSpawn.SpawnType = _spawnType;
    }

    private void OnCastStarted(Skill skill)
    {
        if (_creatureSpawn.IsSpawnTypeUnlocked(_spawnType))
            gameObject.SetActive(true);
    }

    private void OnUnlockedChanged()
    {
        if (gameObject.activeSelf && !_creatureSpawn.IsSpawnTypeUnlocked(_spawnType))
            gameObject.SetActive(false);
    }

    private void OnCanceled()
    {
        gameObject.SetActive(false);
    }
}
