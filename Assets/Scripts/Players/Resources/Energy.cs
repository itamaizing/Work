using System.Collections;
using Mirror;
using UnityEngine;

public class Energy : Resource
{
	[SerializeField] private float _sumDamageGiven = 0;

	private float _regenValue = 1;
	
	private float _lastUsingTime = -999f;

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
			if (_canRegen && _baseValue < _maxValue)
			{
				this.Add(_regenerationValue);
			}
		}
	}*/

	#region override

	protected override IEnumerator RegenerateJob()
	{
		while (true)
		{
			if (!isServer) { yield return null; continue; }
			if (_attr_regenValue.GetValue() <= 0) { yield return null; continue; }

			if (_currentValue < _maxValue)
			{
				float delayValue = _attr_regenDelay != null
					? _attr_regenDelay.GetValue()
					: _regenerationDelay;

				while ((float)NetworkTime.time - _lastUsingTime < delayValue)
					yield return null;

				while (_currentValue < _maxValue)
				{
					if ((float)NetworkTime.time - _lastUsingTime < delayValue)
						break;
                    
					Add(_attr_regenValue.GetValue());
					yield return new WaitForSeconds(RegenerationPeriod);
				}
			}

			yield return null;
		}
	}
	
	[Command]
	private void CmdServerSetLastUseTime(float time)
	{
		_lastUsingTime = time;
	}
	
	public override bool TryUse(float value)
	{
		_lastUsingTime = isServer ? (float)NetworkTime.time : Time.time;
    
		if (!isServer)
			CmdServerSetLastUseTime(_lastUsingTime);

		if (_currentValue - value >= 0)
		{
			_currentValue -= value;
			return true;
		}
		else
		{
			_currentValue = 0;
			return false;
		}
	}

	#endregion

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
