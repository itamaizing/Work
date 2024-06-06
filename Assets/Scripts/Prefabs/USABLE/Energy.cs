using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Energy : StaminaComponent
{
	private float _timer = 0;
	[SerializeField] private float _sumDamageGiven = 0;
	private bool _canRegen = true;

	private void Start()
	{
		//StartCoroutine(RegenirateEnergy());
	}

	private void Update()
	{
		if (_canRegen)
		{
			Regen();
			return;
		}
		_timer += Time.deltaTime;
		if(_timer > _regenerationDelay)
		{
			_timer = 0;
			_canRegen = true;
		}
	}
	// ReSharper disable Unity.PerformanceAnalysis
	public override void Add(float EnergyValue)
	{
		_value += EnergyValue;
		if (_value >= _maxValue)
		{
			_value = _maxValue;
		}
		
		if (EnergyValue > 0 && EnergyValue < 1)
		{
			EnergyValue = 1;
		}

		EnergyValue = (int)EnergyValue;
		
		var text = "+" + EnergyValue.ToString();
		var startColor = new Color(0, 0, 1, 1);
		var endColor = new Color(0, 0, 1, 0.5f);
		ShowPopupText(text,startColor,endColor);
		UpdateBar();
	}
	public override bool Use(float EnergyValue)
	{
		if(EnergyValue > _value) 
		{
			Debug.Log("too much");
			return false;
		}
		Debug.Log("energy used " + EnergyValue);
		_canRegen = false;
		_timer = 0;

		_value -= EnergyValue;

		if (_value <= 0)
		{
			_value = 0;
		}
		UpdateBar();
		return true;
	}

	/*private IEnumerator RegenirateEnergy()
	{
		while (true)
		{
			yield return new WaitForSeconds(_regenerationDelay);
			if (_canRegen && _value < _maxValue)
			{
				this.Add(_regenerationValue);
			}
		}
	}*/

	public float UseAllEnergy()
	{
		float usedEnergy = _value;
		_value = 0;
		UpdateBar();
		return usedEnergy;
	}

	public void SumDamageMake(float damage)
	{
		_sumDamageGiven += damage;
		while(_sumDamageGiven >= 10 ) 
		{
			Add(1);
			_sumDamageGiven -= 10;
		}
	}
}
