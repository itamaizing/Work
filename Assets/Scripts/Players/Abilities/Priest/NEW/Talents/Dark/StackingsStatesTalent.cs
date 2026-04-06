using UnityEngine;

public class StackingsStatesTalent : Talent
{
    [SerializeField] private Restoration _restoration;
    public override void Enter()
    {
        _restoration.SetStackingRestorationTalent(true);
        _restoration.SetStackingDestructionTalent(true);
    }

    public override void Exit()
    {
        _restoration.SetStackingRestorationTalent(false);
        _restoration.SetStackingDestructionTalent(false);
    }
}
