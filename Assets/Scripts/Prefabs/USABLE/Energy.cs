using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Energy : Resource
{
	[SerializeField] private float _sumDamageGiven = 0;

	private float _timer = 0;
	private bool _canRegen = true;

	private void Update()
	{
		if (_canRegen && _regenCoroutine == null)
		{
			ClientStartRegenerateJob();
			return;
		}
        else
        {
			ClientStopRegenerateJob();
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
		CurrentValue += EnergyValue;
		if (CurrentValue >= _maxValue)
		{
			CurrentValue = _maxValue;
		}
	}
	public override bool TryUse(float EnergyValue)
	{
		if(EnergyValue > CurrentValue) 
		{
			Debug.Log("too much");
			return false;
		}
		Debug.Log("energy used " + EnergyValue);
		_canRegen = false;
		_timer = 0;

		CurrentValue -= EnergyValue;

		if (CurrentValue <= 0)
		{
			CurrentValue = 0;
		}
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
		float usedEnergy = CurrentValue;
		CurrentValue = 0;
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
