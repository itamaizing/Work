using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public abstract class PlayerStamina : MonoBehaviour
{
	public float Value { get { return _value; } }

	[SerializeField] protected float _value;
	[SerializeField] protected float _maxValue;

	[SerializeField] protected float _regenerationValue = 10;
	[SerializeField] protected float _regenerationDelay = 3;
	[SerializeField] protected float _timerDelay = 0;

	[SerializeField] protected GameObject Bar;
	[SerializeField] protected Transform DamageSpawn;
	[SerializeField] protected TextMeshPro PrefabText;

	public abstract void Add(float value);
	public abstract bool Use(float value);

	protected virtual void Regen()
	{
		if (_value >= _maxValue) return;

        _timerDelay += Time.deltaTime;
		if (_timerDelay > _regenerationDelay)
		{
			_timerDelay = 0;
			Add(_regenerationValue);
		}
	}
}
