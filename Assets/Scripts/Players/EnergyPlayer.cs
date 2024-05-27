using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnergyPlayer : PlayerStamina
{
	private float _timer = 0;
	private bool _canRegen = true;

	private void Start()
	{
		StartCoroutine(RegenirateEnergy());
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
	public override void Add(float EnergyValue)
	{
		_value += EnergyValue;
		if (_value >= _maxValue)
		{
			_value = _maxValue;
		}
		float newScaleX = _value / _maxValue;
		Bar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

		if (EnergyValue > 0 && EnergyValue < 1)
		{
			EnergyValue = 1;
		}

		EnergyValue = (int)EnergyValue;
		PrefabText.text = "+" + EnergyValue.ToString();
		PrefabText.GetComponent<DamagePrefab>().StartColor = new Color(0, 0, 1, 1);
		PrefabText.GetComponent<DamagePrefab>().EndColor = new Color(0, 0, 1, 0.5f);
		TextMeshPro newPrefab = Instantiate(PrefabText, DamageSpawn.position, Quaternion.identity);
		newPrefab.transform.parent = transform;

		
	}
	public override bool Use(float EnergyValue)
	{
		if(EnergyValue > _value) 
		{
			return false;
		}
		_canRegen = false;
		_timer = 0;

		_value -= EnergyValue;

		float newScaleX = _value / _maxValue;
		Bar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

		if (_value <= 0)
		{
			_value = 0;
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
	}

	public float UseAllEnergy()
	{
		float usedEnergy = _value;
		_value -= usedEnergy;
		return usedEnergy;
	}
}
