using TMPro;
using UnityEngine;

public abstract class PlayerStamina : MonoBehaviour
{
	public float Value { get { return _value; } }
	public float MaxValue { get { return _maxValue; } }

	protected float _value;
	protected float _maxValue;
    protected float _regenerationValue = 10;
	protected float _regenerationDelay = 3;

	[SerializeField] protected GameObject Bar;
	[SerializeField] protected Transform DamageSpawn;
	[SerializeField] protected TextMeshPro PrefabText;

	private float _timerDelay = 0;

	public void Initialize(float maxValue,float regenValue,float regenDelay)
	{
		_value = maxValue;
		_maxValue = maxValue;
		_regenerationValue = regenValue;
		_regenerationDelay = regenDelay;
	}

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
