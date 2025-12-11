using UnityEngine;

public class Energy : Resource
{
	[SerializeField] private float _sumDamageGiven = 0;

	private float _regenValue = 1;

/*	public override void Add(float EnergyValue)
	{
        Debug.Log("Regen " + EnergyValue, this);
        CurrentValue += EnergyValue;
		if (CurrentValue >= _maxValue)
		{
			CurrentValue = _maxValue;
		}
	}
	public override bool TryUse(float EnergyValue)
	{
		if(EnergyValue > _currentValue) 
		{
			return false;
		}

		_currentValue -= EnergyValue;

		if (_currentValue <= 0)
		{
			_currentValue = 0;
		}
		return true;
	}

	private IEnumerator RegenirateEnergy()
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
		TryUse(CurrentValue);

        ClientStopRegenerateJob();
        ClientStartRegenirateJob();
        //CurrentValue = 0;
        return usedEnergy;
	}

	public void SumDamageMake(float damage)
	{
		//Debug.Log("SUM DAMAGE MAKE Energy" + damage);

		_sumDamageGiven += damage;
		while(_sumDamageGiven >= 10 ) 
		{
			CmdAdd(_regenValue);
			_sumDamageGiven -= 10;
		}
	}

	public void ForceRegenNow()
	{
		if (_regenCoroutine == null) 
			Regenerate();
	}

	public void TalentRegenEnergy(float value)
	{
		_regenValue = value;
	}
}
