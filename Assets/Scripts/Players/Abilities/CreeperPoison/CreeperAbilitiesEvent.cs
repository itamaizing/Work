using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreeperAbilitiesEvent : MonoBehaviour ,ICharacterEvents
{
    public delegate void CreeperAbilitiestHandler(float value);
    public event CreeperAbilitiestHandler CreeperPoisonAbilitiesEvent;

    public event Action<float> AbilitiesEvent;

    private void OnEnable()
    {
        GetComponent<CreeperSingleAttack>().FirstAbilityEvent += HandleEvent;
        GetComponent<CreeperTwoFastAttack>().SecondAbilityEvent += HandleEvent;
        GetComponent<CreeperPosionSphere>().ThirdAbilityEvent += HandleEvent;
    }

    private void OnDisable()
    {
        GetComponent<CreeperSingleAttack>().FirstAbilityEvent -= HandleEvent;
        GetComponent<CreeperTwoFastAttack>().SecondAbilityEvent -= HandleEvent;
        GetComponent<CreeperPosionSphere>().ThirdAbilityEvent -= HandleEvent;
    }

    private void HandleEvent(float value)
    {
        CreeperPoisonAbilitiesEvent?.Invoke(value);
        AbilitiesEvent?.Invoke(value);
    }
}
