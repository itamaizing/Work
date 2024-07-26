using UnityEngine;

public class HealthBar : Bar
{
    public override void UpdateValue(float hp, float maxHp)
    {
        _bar.value = hp/maxHp;
        _barText.text = Mathf.RoundToInt(hp).ToString();
    }
}
