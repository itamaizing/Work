using System;
using UnityEngine;

public class PriestAbilityEvents : MonoBehaviour, ICharacterEvents
{
    public delegate void PriestAbilitiestHandler(float value);
    public event PriestAbilitiestHandler PriestAbilitiesEvent;

    public event Action<float> AbilitiesEvent;

    private void OnEnable()
    {
        GetComponent<OneRangeAttack>().FirstAbilityEvent += HandleEvent;
        GetComponent<OneRangeAttack>().DarkFirstAbilityEvent += HandleEvent;

        GetComponent<TwoRangeProtection>().SecondAbilityEvent += HandleEvent;
        GetComponent<TwoRangeProtection>().SecondDarkAbilityEvent += HandleEvent;

        GetComponent<ThreeRangeHeal>().ThirdAbilityEvent += HandleEvent;
        GetComponent<ThreeRangeHeal>().DarkThirdAbilityEvent += HandleEvent;

        GetComponent<FourRangeRecovery>().FourthAbilityEvent += HandleEvent;
        GetComponent<FourRangeRecovery>().DarkFourthAbilityEvent += HandleEvent;

        if (GetComponent<FourRangeRecovery>().TargetParent && GetComponent<FourRangeRecovery>().TargetParent.GetComponent<HealthRecovery>())
        {
            GetComponent<FourRangeRecovery>().TargetParent.GetComponent<HealthRecovery>().FourthBaffEvent += HandleEvent;
        }
        if(GetComponent<FourRangeRecovery>().TargetParent && GetComponent<FourRangeRecovery>().TargetParent.GetComponent<Damage>())
        {
            GetComponent<FourRangeRecovery>().TargetParent.GetComponent<Damage>().DarkFourthBaffEvent += HandleEvent;
        }
    }

    private void OnDisable()
    {
        GetComponent<OneRangeAttack>().FirstAbilityEvent -= HandleEvent;
        GetComponent<OneRangeAttack>().DarkFirstAbilityEvent -= HandleEvent;

        GetComponent<TwoRangeProtection>().SecondAbilityEvent -= HandleEvent;
        GetComponent<TwoRangeProtection>().SecondDarkAbilityEvent -= HandleEvent;

        GetComponent<ThreeRangeHeal>().ThirdAbilityEvent -= HandleEvent;
        GetComponent<ThreeRangeHeal>().DarkThirdAbilityEvent -= HandleEvent;

        GetComponent<FourRangeRecovery>().FourthAbilityEvent -= HandleEvent;
        GetComponent<FourRangeRecovery>().DarkFourthAbilityEvent -= HandleEvent;
        if (GetComponent<FourRangeRecovery>().TargetParent && GetComponent<FourRangeRecovery>().TargetParent.GetComponent<HealthRecovery>())
        {
            GetComponent<FourRangeRecovery>().TargetParent.GetComponent<HealthRecovery>().FourthBaffEvent -= HandleEvent;
        }
        if (GetComponent<FourRangeRecovery>().TargetParent && GetComponent<FourRangeRecovery>().TargetParent.GetComponent<Damage>())
        {
            GetComponent<FourRangeRecovery>().TargetParent.GetComponent<Damage>().DarkFourthBaffEvent -= HandleEvent;
        }
    }

    private void HandleEvent(float value)
    {
        PriestAbilitiesEvent?.Invoke(value);
        AbilitiesEvent?.Invoke(value);
    }
}

