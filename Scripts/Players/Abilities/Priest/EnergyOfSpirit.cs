using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class EnergyOfSpirit : MonoBehaviour
{
    public event Action<EnergyOfSpirit> Destroyed;
    private GameObject[] _allies;
    private GameObject _priest;
    private PriestAbilityEvents _priestAbilityEvents;
    private ICharacterEvents[] _characters;

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
            if (ally.GetComponent<EnergyOfSpirit>() != null)
            {
                _characters = FindObjectsOfType<MonoBehaviour>().OfType<ICharacterEvents>().ToArray();

                foreach (ICharacterEvents character in _characters)
                {
                    character.AbilitiesEvent += AlliesHandleEvent;
                }
            }
        }
    }

    private void PriestHandleEvent(float value)
    {
        if(value > 0 && _priest != null)
        {
            _priest.GetComponent<ManaPlayer>().AddMana(value * 0.09f);
        }
    }

    private void AlliesHandleEvent(float value)
    {
        if(value > 0 && _priest != null)
        {
            _priest.GetComponent<ManaPlayer>().AddMana(value * 0.05f);
        }
    }

    private void OnDisable()
    {
        if (_priestAbilityEvents != null)
        {
            _priestAbilityEvents.PriestAbilitiesEvent -= PriestHandleEvent;
        }
        if(_characters != null)
        {
            foreach (ICharacterEvents character in _characters)
            {
                character.AbilitiesEvent -= AlliesHandleEvent;
            }
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
        _characters = null;
        Destroyed?.Invoke(this);
    }
}
