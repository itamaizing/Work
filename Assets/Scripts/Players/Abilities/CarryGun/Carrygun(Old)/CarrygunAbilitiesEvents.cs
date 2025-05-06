using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarrygunAbilitiesEvents : MonoBehaviour, ICharacterEvents
{
    public delegate void CarrygunAbilitiestHandler(float value);
    public event CarrygunAbilitiestHandler CarrygunAbilitiesEvent;

    public event Action<float> AbilitiesEvent;

    private void OnEnable()
    {
        GetComponent<OneMeleeAttack>().FirstAbilityEvent += HandleEvent;
        GetComponent<TwoMeleeAttack>().SecondAbilityEvent += HandleEvent;
        GetComponent<ThreeMeleeAttack>().ThirdAbilityEvent += HandleEvent;
        GetComponent<FourMeleeAttack>().FourthAbilityEvent += HandleEvent;
    }

    private void OnDisable()
    {
        GetComponent<OneMeleeAttack>().FirstAbilityEvent -= HandleEvent;
        GetComponent<TwoMeleeAttack>().SecondAbilityEvent -= HandleEvent;
        GetComponent<ThreeMeleeAttack>().ThirdAbilityEvent -= HandleEvent;
        GetComponent<FourMeleeAttack>().FourthAbilityEvent -= HandleEvent;
    }

    private void HandleEvent(float value)
    {
        CarrygunAbilitiesEvent?.Invoke(value);
        AbilitiesEvent?.Invoke(value);
    }
}
