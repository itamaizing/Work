using UnityEngine;

public class StaminaBar : Bar
{
    public override void UpdateValue(float mana, float maxMana)
    {
        _bar.value = mana/maxMana;
        _barText.text = Mathf.RoundToInt(mana).ToString();
    }
}
