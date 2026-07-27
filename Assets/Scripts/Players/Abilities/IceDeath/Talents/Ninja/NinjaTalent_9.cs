using UnityEngine;

public class NinjaTalent_9 : Talent
{
    private const float BonusEnergyMax = 30f;
    private bool isEnabled;

    public override void Enter()
    {
        IsIncreaseMaxEnergy(true);
    }

    public override void Exit()
    {
        IsIncreaseMaxEnergy(false);
    }


    public void IsIncreaseMaxEnergy(bool value)
    {
        if(value == isEnabled) return;
        isEnabled = value;
        if (character == null) return;
        var energyResource = character.TryGetResource(ResourceType.Energy);
        if (energyResource == null) return;

        if (value)
        {
            energyResource.AddMax(BonusEnergyMax, keepPercent: true);
        }
        else
        {
            energyResource.AddMax(-BonusEnergyMax, keepPercent: true);
        }
        energyResource.Regenerate();
    }
}