using UnityEngine;

public class CounterRageTalent : Talent
{
    [SerializeField] private CounterRage_Scorpion _counterRage;
    public override void Enter()
    {
        _counterRage.EnableCounterRage(true,character);
    }

    public override void Exit()
    {
        _counterRage.EnableCounterRage(false,character);
    }
}