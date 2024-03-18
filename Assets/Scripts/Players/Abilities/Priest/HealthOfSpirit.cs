using System;
using System.Collections;
using UnityEngine;

public class HealthOfSpirit : MonoBehaviour
{
    public event Action<HealthOfSpirit> Destroyed;
    private GameObject[] _allies;
    private GameObject _priest;
    private PriestAbilityEvents _priestAbilityEvents;

    private void Start()
    {
        StartCoroutine(CountdownRoutine(5));

        _allies = GameObject.FindGameObjectsWithTag("Allies");

        foreach (GameObject ally in _allies)
        {
            _priestAbilityEvents = ally.GetComponent<PriestAbilityEvents>();

            if (_priestAbilityEvents != null)
            {
                _priest = _priestAbilityEvents.gameObject;
                _priestAbilityEvents.PriestAbilitiesEvent += PriestHandleEvent;
            }
        }
    }

    private void PriestHandleEvent(float value)
    {
        if(value > 0 && _priest != null) 
        {
            _priest.GetComponent<HealthPlayer>().AddHeal(value * 0.09f);

        }
    }

    private void OnDisable()
    {
        if (_priestAbilityEvents != null)
        {
            _priestAbilityEvents.PriestAbilitiesEvent -= PriestHandleEvent;
        }
    }

    public IEnumerator CountdownRoutine(int time)
    {
        while (time > 0)
        {
            yield return new WaitForSeconds(1f);
            time--;
        }
        Destroy(this);
    }
    private void OnDestroy()
    {
        _allies = null;
        _priest = null;
        _priestAbilityEvents = null;
        Destroyed?.Invoke(this);
    }
}
