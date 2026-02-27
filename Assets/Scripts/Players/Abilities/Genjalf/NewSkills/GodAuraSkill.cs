using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GodAuraSkill : MonoBehaviour
{

}

public class GodAura : AuraState
{
    public override States State => States.GodAura;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects { get; }
    public override float Distance => 8;
    public override float EffectRate { get; }
    public override LayerMask LayerMask => LayerMask.GetMask("Allies");

    public override void EffectOnEnter(Character character)
    {
        if (characterState.Character != character)
        {
            CmdAddState(character.gameObject);
        }
    }

    public override void EffectOnExit(Character character)
    {
        if (character.CharacterState.CheckForState(States.GodAuraBuff))
        {
            CmdRemoveState(character.gameObject);
        }
    }

    public override void EffectOnStay(List<Character> characters)
    {

    }

    public override void ExitState()
    {
        base.ExitState();
        foreach (var character in _charactersInRadius)
        {
            CmdRemoveState(character.gameObject);
        }
    }
    
    [Command]
    private void CmdAddState(GameObject target)
    {
        target.GetComponent<Character>().CharacterState.AddState(States.GodAuraBuff,-1,0,target,nameof(HotAuraBuff));
    }

    [Command]
    private void CmdRemoveState(GameObject target)
    {
        target.GetComponent<Character>().CharacterState.RemoveState(States.GodAuraBuff);
    }
}

public class GodAuraBuff : AbstractCharacterState
{
    private List<StatusEffect> _effects = new List<StatusEffect>();
    private float _percentage = 0.1f;
    private Character _character;

    public override States State => States.GodAuraBuff;

    public override StateType Type => StateType.Magic;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _character = character.Character;
        foreach (var skill in character.Character.Abilities.Abilities)
        {
            skill.Buff.Cooldown.IncreasePercentage(1 - _percentage);
        }
    }

    public override void ExitState()
    {
        _character.CharacterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }

    public override void UpdateState()
    {
    }
}
