using UnityEngine;

public class FireComboTalent : Talent
{
    [SerializeField] private FireComboHandler _handler;
    [SerializeField] private ConsumeCombo_Scorpion consume;

    public override void Enter()
    {
        _handler.SetEnabled(true);
        consume.SetFireComboTalentEnabled(true);
    }

    public override void Exit()
    {
        _handler.SetEnabled(false);
        consume.SetFireComboTalentEnabled(false);
    }
}
