using System.Collections;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Acid : MonoBehaviour
{
    [SerializeField] private GameObject _venomEffectPrefab;

    [HideInInspector] public GameObject Target;
    [HideInInspector] public int Damage;
    [HideInInspector] public GameObject Spisnscider;

    private Vector2 _startPosition;
    private Vector2 _targetPosition;
    private float _timeFlight = 0f;
    private float _chanceMakeVenom = 0.3f;
    private GameObject _newEffectPrefab;


    void Start()
    {
        _startPosition = transform.position;
        _targetPosition = Target.transform.position;
        FlightTimeCalculation();
        StartCoroutine(SmoothMove());
    }

    private void FlightTimeCalculation()
    {
        float distanceToTarget = (_targetPosition - _startPosition).magnitude;
        _timeFlight = distanceToTarget / 1.94f * 0.1f;
    }

    private IEnumerator SmoothMove()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _timeFlight)
        {
            transform.position = Vector3.Lerp(_startPosition, _targetPosition, elapsedTime / _timeFlight);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = _targetPosition;
        MakeDamage();
    }

    private void MakeDamage()
    {
        Spisnscider.GetComponent<HealthComponent>().MakePhisicDamage(Damage, Target);

        if(Random.value <= _chanceMakeVenom)
        {
            if (_newEffectPrefab == null)
            {
                _newEffectPrefab = Instantiate(_venomEffectPrefab);
                _newEffectPrefab.transform.SetParent(Target.transform);
                _newEffectPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(6);
            }
            else
            {
                Target.transform.Find("Effects").transform.Find("VenomDebuff").GetComponentInChildren<BaffDebaffEffectPrefab>().Timer = 6;
                _newEffectPrefab.GetComponent<VenomSpisnacider>().Timer = 6;
                _newEffectPrefab.GetComponent<VenomSpisnacider>().PercentageOfProtectionReduction += Mathf.Min(0.02f, 0.2f);

                _newEffectPrefab = Instantiate(_venomEffectPrefab);

                foreach (Transform child in _newEffectPrefab.transform)
                {
                    Destroy(child.gameObject);
                }

                _newEffectPrefab.transform.SetParent(Target.transform);
            }
        }
        Destroy(gameObject);
    }
}
