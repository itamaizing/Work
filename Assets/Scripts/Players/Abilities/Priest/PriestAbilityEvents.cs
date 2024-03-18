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
        GetComponent<TwoRangeProtection>().SecondAbilityEvent += HandleEvent;
        GetComponent<ThreeRangeHeal>().ThirdAbilityEvent += HandleEvent;
        GetComponent<FourRangeRecovery>().FourthAbilityEvent += HandleEvent;
        if(GetComponent<FourRangeRecovery>().Target && GetComponent<FourRangeRecovery>().Target.GetComponent<HealthRecovery>())
        {
            GetComponent<FourRangeRecovery>().Target.GetComponent<HealthRecovery>().FourthBaffEvent += HandleEvent;
        }
        GetComponent<DarkOneRangeAttack>().DarkFirstAbilityEvent += HandleEvent;
        GetComponent<DarkTwoRangeProtection>().SecondDarkAbilityEvent += HandleEvent;
        GetComponent<DarkThreeRangeHeal>().DarkThirdAbilityEvent += HandleEvent;
        GetComponent<DarkFourRangeRecovery>().DarkFourthAbilityEvent += HandleEvent;
        if(GetComponent<DarkFourRangeRecovery>().Target && GetComponent<DarkFourRangeRecovery>().Target.GetComponent<Damage>())
        {
            GetComponent<DarkFourRangeRecovery>().Target.GetComponent<Damage>().DarkFourthBaffEvent += HandleEvent;
        }
    }

    private void OnDisable()
    {
        GetComponent<OneRangeAttack>().FirstAbilityEvent -= HandleEvent;
        GetComponent<TwoRangeProtection>().SecondAbilityEvent -= HandleEvent;
        GetComponent<ThreeRangeHeal>().ThirdAbilityEvent -= HandleEvent;
        GetComponent<FourRangeRecovery>().FourthAbilityEvent -= HandleEvent;
        if (GetComponent<FourRangeRecovery>().Target && GetComponent<FourRangeRecovery>().Target.GetComponent<HealthRecovery>())
        {
            GetComponent<FourRangeRecovery>().Target.GetComponent<HealthRecovery>().FourthBaffEvent -= HandleEvent;
        }
        GetComponent<DarkOneRangeAttack>().DarkFirstAbilityEvent -= HandleEvent;
        GetComponent<DarkTwoRangeProtection>().SecondDarkAbilityEvent -= HandleEvent;
        GetComponent<DarkThreeRangeHeal>().DarkThirdAbilityEvent -= HandleEvent;
        GetComponent<DarkFourRangeRecovery>().DarkFourthAbilityEvent -= HandleEvent;
        if (GetComponent<DarkFourRangeRecovery>().Target && GetComponent<DarkFourRangeRecovery>().Target.GetComponent<Damage>())
        {
            GetComponent<DarkFourRangeRecovery>().Target.GetComponent<Damage>().DarkFourthBaffEvent -= HandleEvent;
        }
    }

    private void HandleEvent(float value)
    {
        PriestAbilitiesEvent?.Invoke(value);
        AbilitiesEvent?.Invoke(value);
    }
}

