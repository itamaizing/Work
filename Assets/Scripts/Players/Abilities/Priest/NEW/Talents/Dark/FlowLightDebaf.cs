using UnityEngine;

public class FlowLightDebaf : Talent
{
    [SerializeField] private FlowOfLight _flow;
    public override void Enter()
    {
        _flow.SetSlowTalent(true);
    }

    public override void Exit()
    {
        _flow.SetSlowTalent(false);
    }
}
