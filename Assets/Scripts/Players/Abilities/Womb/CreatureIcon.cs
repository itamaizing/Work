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
        _creatureSpawn.CastStarted += OnCastStarted;
        _creatureSpawn.CastEnded += OnCanceled;
        _creatureSpawn.Canceled += OnCanceled;

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
        _creatureSpawn.CastStarted -= OnCastStarted;
        _creatureSpawn.CastEnded -= OnCanceled;
        _creatureSpawn.Canceled -= OnCanceled;
    }

    private void OnClick()
    {
        _creatureSpawn.SpawnType = _spawnType;
    }

    private void OnCastStarted()
    {
        gameObject.SetActive(true);
    }

    private void OnCanceled()
    {
        gameObject.SetActive(false);
    }
}
