using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnergyPlayer : MonoBehaviour
{
	[SerializeField][Range(0, 100)] private float _regenerationValue = 10;
	[SerializeField][Range(0, 100)] private float _regenerationDelay = 3;
	[SerializeField] private float _maxValue;
	private WaitForSeconds _waitForRegen;
	private float _regenDelay = 3;
	private float _timer = 0;
	private bool _canRegen = true;

	public float Energy;
	public GameObject EnergyBar;
	public Transform DamageSpawn;
	public TextMeshPro PrefabText;
	private void Start()
	{
		_waitForRegen = new WaitForSeconds(_regenerationDelay);
		StartCoroutine(RegenirateEnergy());
	}

	private void Update()
	{
		if (_canRegen) return;

		Debug.Log("timer start");
		_timer += Time.deltaTime;
		if(_timer > _regenDelay)
		{
			_timer = 0;
			_canRegen = true;

			Debug.Log("timer stop");
		}
	}
	public void AddEnergy(float EnergyValue)
	{
		Energy += EnergyValue;
		if (Energy <= 0)
		{
			Energy = 0;
		}

		if (Energy >= _maxValue)
		{
			Energy = _maxValue;
		}
		float newScaleX = Energy / _maxValue;
		EnergyBar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

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
	public void UseEnergy(float EnergyValue)
	{
		_canRegen = false;
		_timer = 0;

		Energy -= EnergyValue;

		float newScaleX = Energy / _maxValue;
		EnergyBar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

		if (Energy <= 0)
		{
			Energy = 0;
		}
		if (Energy >= _maxValue)
		{
			Energy = _maxValue;
		}
	}

	private IEnumerator RegenirateEnergy()
	{
		while (true)
		{
			yield return _waitForRegen;
			if (_canRegen && Energy < _maxValue)
			{
				this.AddEnergy(_regenerationValue);
			}
		}
	}
}
