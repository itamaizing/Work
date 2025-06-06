using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bleeding : MonoBehaviour
{
    [SerializeField] private GameObject BleedEffectPrefab;
    private GameObject _newBleedPrefab;
    private float _bleedEndTime = 3f;

    public delegate void BleedingHandler(GameObject target, AbilityBase ability);
    public event BleedingHandler BleedingEvent;

    public void StartBleed(GameObject target, AbilityBase ability)
    {
        if (_newBleedPrefab != null)
        {
            Destroy(_newBleedPrefab);
        }
        _newBleedPrefab = Instantiate(BleedEffectPrefab);
        _newBleedPrefab.transform.SetParent(target.transform);
        _newBleedPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown((int)_bleedEndTime);

        BleedingEvent?.Invoke(target, ability);
    }
}
