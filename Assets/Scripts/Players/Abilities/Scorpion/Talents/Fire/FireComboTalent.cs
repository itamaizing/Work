using UnityEngine;

public class FireComboTalent : Talent
{
    [SerializeField] private FireComboHandler _handler;

    public override void Enter() => _handler.SetEnabled(true);
    public override void Exit()  => _handler.SetEnabled(false);
}
