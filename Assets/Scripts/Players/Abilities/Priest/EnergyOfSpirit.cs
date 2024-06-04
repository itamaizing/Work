using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class EnergyOfSpirit : MonoBehaviour
{ 
    public event Action<EnergyOfSpirit> Destroyed;
    private GameObject[] _allies;
    private GameObject _priest;
    private ICharacterEvents[] _characters;

    private Coroutine _coroutine;

    private void Start()
    {
        _allies = GameObject.FindGameObjectsWithTag("Allies");
        foreach (GameObject ally in _allies)
        {
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
    public void StartCountdownCoroutine(float time)
    {
        if(_coroutine!=null)
        {
            StopCoroutine(_coroutine);
        }
        _coroutine = StartCoroutine(CountdownRoutine(time));
    }

    private void AlliesHandleEvent(float value)
    {
        if(value > 0 && _priest != null)
        {
            _priest.GetComponent<Mana>().Add(value * 0.05f);
		}
    }

    private void OnDisable()
    {
        if(_characters != null)
        {
            foreach (ICharacterEvents character in _characters)
            {
                character.AbilitiesEvent -= AlliesHandleEvent;
            }
        }
    }

    public IEnumerator CountdownRoutine(float time)
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
        _characters = null;
        Destroyed?.Invoke(this);
    }
}
