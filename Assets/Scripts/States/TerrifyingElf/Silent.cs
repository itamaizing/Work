using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Silent : AbstractCharacterState
{
    private float _baseDuration;
    private Silence _silence;
    private bool _isSilenceAddAllCharacterWithDeabaffElf;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Silent;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Entering Silent State");
        characterState = character;
        base.personWhoMadeBuff = personWhoMadeBuff;
        _baseDuration = durationToExit;

        duration = _baseDuration; 

        Debug.Log($"duration: {duration}");

        BlockMagicAbilities();
    }

    public override void UpdateState()
    {
        if (duration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Silent State");
        characterState.StateIcons.RemoveItemByState(State);
        characterState.RemoveState(this);

        UnblockMagicAbilities();
    }

    private void BlockMagicAbilities()
    {
        if (characterState.Character.Abilities == null) return;

        foreach (var skill in characterState.Character.Abilities.Abilities)
        {
            if (skill.Info.AbilityForm == AbilityForm.Magic)
            {
                skill.Disactive = true;
                Debug.Log($"Blocking magic skill: {skill.Name}");
            }
        }
    }

    private void UnblockMagicAbilities()
    {
        if (characterState.Character.Abilities == null) return;

        foreach (var skill in characterState.Character.Abilities.Abilities)
        {
            if (skill.Info.AbilityForm == AbilityForm.Magic)
            {
                skill.Disactive = false;
                Debug.Log($"Unblocking magic skill: {skill.Name}");
            }
        }
    }

    [Command] 
    private void CmdStateSilent(Character target, float dur, GameObject caster) => ClientRpcStateSilent(target, dur, caster);
    
    [ClientRpc] 
    private void ClientRpcStateSilent(Character target, float dur, GameObject caster) 
    { 
        target.CharacterState.AddStateLogic(States.Silent, dur, 0f, Schools.None, caster, null); 
    }
}