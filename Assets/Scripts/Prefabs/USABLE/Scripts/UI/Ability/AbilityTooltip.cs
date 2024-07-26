using UnityEngine;

public class AbilityTooltip : MonoBehaviour
{
    [SerializeField] private TMProLocalizer _title;
    [SerializeField] private TMProLocalizer _description;
    [SerializeField] private TMProLocalizer _manaCost;
    [SerializeField] private TMProLocalizer _energyCost;
    [SerializeField] private TMProLocalizer _runeCost;
    [SerializeField] private TMProLocalizer _cooldownValue;
    [SerializeField] private TMProLocalizer _castTime;
    [SerializeField] private TMProLocalizer _mainValue;
    [SerializeField] private TMProLocalizer _bonusValue;

    public void Fill(string title, string description, EnergyCost staminaType,
        float cooldownValue, float castTime, AbilityDataType abilityType, float mainValue, float bonusValue)
    {
        _title.Localize(title);
        _description.Localize(description);
        _cooldownValue.Localize(cooldownValue);
        _castTime.Localize(castTime);
        _mainValue.Localize(abilityType, mainValue);
        _bonusValue.Localize(bonusValue);
        
        _energyCost.gameObject.SetActive(staminaType.energyType == StaminaType.Energy);
        _manaCost.gameObject.SetActive(staminaType.energyType == StaminaType.Mana);
        _runeCost.gameObject.SetActive(staminaType.energyType == StaminaType.Rune);
        
            switch (staminaType.energyType)
            {
                case StaminaType.Mana:
                    _manaCost.Localize(staminaType.energyType, staminaType.costValue);
                    break;
                case StaminaType.Energy:
                    _energyCost.Localize(staminaType.energyType, staminaType.costValue);
                    break;
                case StaminaType.Rune:
                    _runeCost.Localize(staminaType.energyType, staminaType.costValue);
                    break;
            }
    }
}
