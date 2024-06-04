using TMPro;
using UnityEngine;

public class Mana : StaminaComponent
{
    public override void Add(float manaValue)
    {
        _value += manaValue;
        _value = Mathf.Clamp(_value, 0, _maxValue);
        
        if (manaValue > 0 && manaValue < 1)
        {
            manaValue = 1;
        }

        var text = "+" + manaValue.ToString("0.0");
        var startColor = new Color(0, 0, 1, 1);
        var endColor = new Color(0, 0, 1, 0.5f);
        ShowPopupText(text,startColor,endColor);
        UpdateBar();

    }

    public override bool Use(float manaValue)
    {
        _value -= manaValue;
        _value = Mathf.Clamp(_value, 0, _maxValue);
        UpdateBar();
        return false;
    }

}
