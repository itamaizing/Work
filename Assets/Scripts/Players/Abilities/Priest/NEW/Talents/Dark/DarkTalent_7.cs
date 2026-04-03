using UnityEngine;

public class DarkTalent_7 : Talent
{
    [SerializeField] private float _baffDuration;
    [SerializeField] private float _baffAdditionalTime;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float _baffChance;

    public override void Enter()
    {
        var flow = character.Abilities.GetSkill<FlowOfLight>();
        var spark = character.Abilities.GetSkill<SparkOfLight>();

        flow?.DestructionFillingTalent(true, _baffDuration, _baffAdditionalTime, _baffChance);
        spark?.DestructionFillingTalent(true, _baffDuration, _baffAdditionalTime, _baffChance);
    }

    public override void Exit()
    {
        var flow = character.Abilities.GetSkill<FlowOfLight>();
        var spark = character.Abilities.GetSkill<SparkOfLight>();

        flow?.DestructionFillingTalent(false, _baffDuration, _baffAdditionalTime, _baffChance);
        spark?.DestructionFillingTalent(false, _baffDuration, _baffAdditionalTime, _baffChance);
    }
}
