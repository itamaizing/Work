using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class HotAura : MonoBehaviour
{
    private void Start()
    {
        var chatacter = GetComponent<Character>();
        chatacter.CharacterState.CmdAddState(States.HotBloodAura, 0, 0, chatacter.gameObject, name);
    }
}

public class HotBloodAura : AuraState
{
    private float _percentage = 0.1f;
    public override States State { get; }
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects { get; }
    public override float Distance => 6;
    public override float EffectRate { get; }
    public override LayerMask LayerMask => LayerMask.GetMask("Allies");

    public override void EffectOnEnter(Character character)
    {
        CmdAddState(character.gameObject);
    }

    public override void EffectOnExit(Character character)
    {
        if (character.CharacterState.CheckForState(States.HotBloodBuff))
        {
            character.CharacterState.CmdRemoveState(States.HotBloodBuff);
        }
    }

    public override void EffectOnStay(List<Character> characters)
    {
        foreach (var character in characters)
        {
            if (!character.CharacterState.CheckForState(States.HotBloodBuff))
            {
                CmdAddState(character.gameObject);
            }
        }
    }
    
    [Command]
    private void CmdAddState(GameObject target)
    {
        target.GetComponent<Character>().CharacterState.AddState(States.HotBloodBuff,-1,0,target,nameof(HotAuraBuff));
    }
}

public class HotAuraBuff : AbstractCharacterState
{
    private List<StatusEffect> _effects = new List<StatusEffect>();
    private float _percentage = 0.1f;
    private Character _character;

    public override States State => States.HotBloodBuff;

    public override StateType Type => StateType.Magic;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _character = character.Character;
        foreach (var skill in character.Character.Abilities.Abilities)
        {
            skill.Buff.CastSpeed.IncreasePercentage(1 - _percentage);
            skill.Buff.AttackSpeed.IncreasePercentage(1 - _percentage);
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
