using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shield_PsionicaTalent4 : MonoBehaviour
{
    [SerializeField] private Toggle _toggleTalent;
    [SerializeField] private Toggle _toggleAbility;
    [SerializeField] private Toggle _toggleSkillsOfPredator;
    [SerializeField] private Component [] _typesPsionicTalents;
    [SerializeField] private GameObject _shieldEffect;

    private GameObject _player;
    private GameObject _newPrefab;
    private float _endurance;

    void Start()
    {
        _player = transform.parent.gameObject;

        _player.GetComponent<HealthComponent>().MakeMagicDamageEvent += CheckDamagePsionicAbilities;
        _player.GetComponent<HealthComponent>().MakePhisicDamageEvent += CheckDamagePsionicAbilities;

    }

    void Update()
    {
        if (_toggleTalent.isOn && _toggleAbility.gameObject.activeSelf == false)
        {
            _toggleAbility.gameObject.SetActive(true);
        }
        else if (!_toggleTalent.isOn && _toggleAbility.gameObject.activeSelf)
        {
            _toggleAbility.gameObject.SetActive(false);
        }

        int activeChildCount = 0;
        foreach (Transform child in _toggleAbility.transform.parent)
        {
            if (child.gameObject.activeSelf)
            {
                activeChildCount++;

                if (child.gameObject == _toggleAbility.gameObject)
                {
                    break;
                }
            }
        }
        string key = "Alpha" + activeChildCount;

        if (Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), key)) && _toggleAbility.gameObject.activeSelf && _toggleAbility.transform.parent.gameObject.activeSelf)
        {
            if (_toggleAbility.enabled)
            {
                _toggleAbility.isOn = !_toggleAbility.isOn;
            }
        }

        if (_toggleAbility.isOn && _toggleAbility.gameObject.activeSelf)
        {
           StartCoroutine(ActivateShield());
        }
    }

    private IEnumerator ActivateShield()
    {
        if (_player.GetComponent<PsionicaMelee>().Psionica > 0)
        {
            _endurance = _player.GetComponent<PsionicaMelee>().Psionica;
            _player.GetComponent<PsionicaMelee>().Psionica = 0;

            if (_newPrefab != null)
            {
                Destroy(_newPrefab);
            }

            _newPrefab = Instantiate(_shieldEffect);
            _newPrefab.GetComponent<DamageAbsorption>().MaxAbsorption = _endurance;
            _newPrefab.transform.SetParent(_player.transform);
            _newPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(6);

            yield return new WaitForSeconds(6f);

            _toggleAbility.isOn = false;
        }
        else
        {
            _toggleAbility.isOn = false;

        }
    }

    private void CheckDamagePsionicAbilities(HealthComponent.DamageInfo damageInfo)
    {
        if(_toggleTalent.isOn && _toggleSkillsOfPredator.isOn)
        {

            foreach (Component obj in _typesPsionicTalents)
            {
                if(damageInfo.CallerType == obj.GetType())
                {
                    _player.GetComponent<PsionicaMelee>().MakePsionica(damageInfo.OriginalDamage * 0.5f) ;
                    break;
                }
            }
        }
    }
}
