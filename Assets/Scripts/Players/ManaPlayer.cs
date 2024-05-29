using TMPro;
using UnityEngine;

public class ManaPlayer : PlayerStamina
{
    public override void Add(float manaValue)
    {
        _value += manaValue;

        float newScaleX = _value / _maxValue;
        Bar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

        if (manaValue > 0 && manaValue < 1)
        {
            manaValue = 1;
        }

        PrefabText.text = "+" + manaValue.ToString("0.0");
        PrefabText.GetComponent<DamagePrefab>().StartColor = new Color(0, 0, 1, 1);
        PrefabText.GetComponent<DamagePrefab>().EndColor = new Color(0, 0, 1, 0.5f);
        TextMeshPro newPrefab = Instantiate(PrefabText, DamageSpawn.position, Quaternion.identity);
        newPrefab.transform.SetParent(transform);

        _value = Mathf.Clamp(_value, 0, _maxValue);

    }

    public override bool Use(float manaValue)
    {
        _value -= manaValue;

        float newScaleX = _value / _maxValue;
        Bar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

        _value = Mathf.Clamp(_value, 0, _maxValue);
        
        return false;
    }

}
