using Mirror;
using UnityEngine;

public class AddMaxEnergy_Scorpion : NetworkBehaviour
{
    [SerializeField] private Character _hero;
    [SerializeField] private float _bonusMaxEnergy = 30f;

    private AttributeModifier _energyMaxModifier;
    private bool isEnabled;

    private void Awake()
    {
        _energyMaxModifier = new AttributeModifier(_bonusMaxEnergy, ModifierType.Flat);
    }

    public void IsIncreaseMaxEnergy(bool value)
    {
        if(value == isEnabled) return;
        isEnabled = value;
        if (_hero == null) return;
        var energyResource = _hero.TryGetResource(ResourceType.Energy);
        if (energyResource == null) return;

        if (value)
        {
            energyResource.AddMax(_bonusMaxEnergy, keepPercent: true);
        }
        else
        {
            energyResource.AddMax(-_bonusMaxEnergy, keepPercent: true);
        }
    }
}
