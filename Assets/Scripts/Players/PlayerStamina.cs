using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public abstract class PlayerStamina : MonoBehaviour
{
	public float Value { get { return _value; }}

	[SerializeField] protected float _value;
	[SerializeField] protected float _maxValue;

	[SerializeField] protected float _regenerationValue = 10;
	[SerializeField] protected float _regenerationDelay = 3;

	protected GameObject Bar;
	protected Transform DamageSpawn;
	protected TextMeshPro PrefabText;

	public abstract void Add(float value);
	public abstract bool Use(float value);
}
