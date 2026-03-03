using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class HotAura : MonoBehaviour
{
    private void Start()
    {
        var character = GetComponent<Character>();
        character.CharacterState.CmdAddState(States.HotBloodAura, 0, 0, character.gameObject, name);
    }

    private void OnDestroy()
    {
        var character = GetComponent<Character>();
        character.CharacterState.CmdRemoveState(States.HotBloodAura);
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
            CmdRemoveState(character.gameObject);
        }
    }

    public override void EffectOnStay(List<Character> characters)
    {
    }
    
    [Command]
    private void CmdAddState(GameObject target)
    {
        target.GetComponent<Character>().CharacterState.AddState(States.HotBloodBuff,-1,0,target,nameof(HotAuraBuff));
    }
    
    [Command]
    private void CmdRemoveState(GameObject target)
    {
        target.GetComponent<Character>().CharacterState.RemoveState(States.HotBloodBuff);
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
