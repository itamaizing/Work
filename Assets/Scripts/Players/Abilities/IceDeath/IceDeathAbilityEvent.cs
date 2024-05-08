using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceDeathAbilityEvent : MonoBehaviour, ICharacterEvents
{
	public delegate void IceDeathAbilitiestHandler(float value);
	public event IceDeathAbilitiestHandler CarrygunAbilitiesEvent;

	public event Action<float> AbilitiesEvent;

	private void OnEnable()
	{
		//GetComponent<Icecloud>().IceCloudAbilityEvent += HandleEvent;
		/*GetComponent<TwoMeleeAttack>().SecondAbilityEvent += HandleEvent;
		GetComponent<ThreeMeleeAttack>().ThirdAbilityEvent += HandleEvent;
		GetComponent<FourMeleeAttack>().FourthAbilityEvent += HandleEvent;*/
	}

	private void OnDisable()
	{
		//GetComponent<Icecloud>().IceCloudAbilityEvent -= HandleEvent;
		/*GetComponent<TwoMeleeAttack>().SecondAbilityEvent -= HandleEvent;
		GetComponent<ThreeMeleeAttack>().ThirdAbilityEvent -= HandleEvent;
		GetComponent<FourMeleeAttack>().FourthAbilityEvent -= HandleEvent;*/
	}

	private void HandleEvent(float value)
	{
		CarrygunAbilitiesEvent?.Invoke(value);
		AbilitiesEvent?.Invoke(value);
	}
}
