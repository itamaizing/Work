using UnityEngine;

public class MaxEnergyTalent : Talent
{
    [SerializeField] private AddMaxEnergy_Scorpion _addMaxEnergyComponent;
    
    public override void Enter()
    {
        _addMaxEnergyComponent.IsIncreaseMaxEnergy(true);
    }

    public override void Exit()
    {
        _addMaxEnergyComponent.IsIncreaseMaxEnergy(false);
    }
}
