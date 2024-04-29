using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ManaPlayer : PlayerStamina
{
    private WaitForSeconds _waitForRegenMana;

    private void Start()
    {
        _waitForRegenMana = new WaitForSeconds(_regenerationDelay);
        StartCoroutine(CoroutineRegenirateMana());
    }
    public override void Add(float manaValue)
    {
        _value += manaValue;

        float newScaleX = _value / 1000.0f;
        Bar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

        if (manaValue > 0 && manaValue < 1)
        {
            manaValue = 1;
        }

        manaValue = (int)manaValue;
        PrefabText.text = "+" + manaValue.ToString();
        PrefabText.GetComponent<DamagePrefab>().StartColor = new Color(0, 0, 1, 1);
        PrefabText.GetComponent<DamagePrefab>().EndColor = new Color(0, 0, 1, 0.5f);
        TextMeshPro newPrefab = Instantiate(PrefabText, DamageSpawn.position, Quaternion.identity);
        newPrefab.transform.parent = transform;

        if (_value <= 0)
        {
            _value = 0;
        }

        if (_value >= _maxValue)
        {
            _value = 1000;
        }
    }

    public void RegenMana(float manaValue) // для регена, тот же самый AddMana, но без префаба значения
    {
        _value += manaValue;

        float newScaleX = _value / 1000.0f;
        Bar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

        if (manaValue > 0 && manaValue < 1)
        {
            manaValue = 1;
        }

        manaValue = (int)manaValue;
        PrefabText.text = "+" + manaValue.ToString();
        PrefabText.GetComponent<DamagePrefab>().StartColor = new Color(0, 0, 1, 1);
        PrefabText.GetComponent<DamagePrefab>().EndColor = new Color(0, 0, 1, 0.5f);

        if (_value <= 0)
        {
            _value = 0;
        }

        if (_value >= _maxValue)
        {
            _value = _maxValue;
        }
    }

	public override bool Use(float manaValue)
    {
		if (manaValue > _value)
		{
			return false;
		}
		_value -= manaValue;

        float newScaleX = _value / _maxValue;
        Bar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

        if (_value >= _maxValue)
        {
			_value = _maxValue;
        }

        return true;
    }

    private IEnumerator CoroutineRegenirateMana()
    {
        while (true)
        {
            yield return _waitForRegenMana;
            this.RegenMana(_regenerationValue);
        }
    }
}
