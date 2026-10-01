using UnityEngine;

public class MultiTargetComboTalent : Talent
{
    [SerializeField] private PassiveCombo_Scorpion passiveCombo_Scorpion;
    public override void Enter()
    {
        passiveCombo_Scorpion.SetMultiTargetComboTalent(true);
    }

    public override void Exit()
    {
        passiveCombo_Scorpion.SetMultiTargetComboTalent(false);
    }
}
