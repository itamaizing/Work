using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Silent : AbstractCharacterState
{
    private float _baseDuration;
    private float _duration;
    private Silence _silence;
    private bool _isSilenceAddAllCharacterWithDeabaffElf;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Silent;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Entering Silent State");
        characterState = character;
        base.personWhoMadeBuff = personWhoMadeBuff;
        _baseDuration = durationToExit;
        _duration = _baseDuration;

        Debug.Log($"_duration: {_duration}");

        if (base.personWhoMadeBuff.TryGetComponent<Silence>(out var silence))
        {
            _isSilenceAddAllCharacterWithDeabaffElf = silence.IsSilenceAddAllCharacterWithDeabaffElf;
            _silence = silence;
        }

        if (_silence != null && _isSilenceAddAllCharacterWithDeabaffElf)
        {
            HashSet<States> targetDebuffsFromCaster = new();

            foreach (var state in characterState.CurrentStates) 
                if (state.BaffDebaff == BaffDebaff.Debaff && state.PersonWhoMadeBuff == base.personWhoMadeBuff) targetDebuffsFromCaster.Add(state.State);

            if (targetDebuffsFromCaster.Count == 0) return;

            foreach (var target in GameObject.FindObjectsOfType<Character>())
            {
                if (target == characterState.Character)
                    continue;

                var state = target.CharacterState;
                if (state == null) continue;

                foreach (var targetState in state.CurrentStates)
                {
                    if (targetState.BaffDebaff == BaffDebaff.Debaff && targetState.PersonWhoMadeBuff == base.personWhoMadeBuff && targetDebuffsFromCaster.Contains(targetState.State))
                    {
                        CmdStateSilent(target);
                        break;
                    }
                }
            }

        }

        BlockMagicAbilities();
    }

    public override void UpdateState()
    {
    }

    protected override void ExitState()
    {
        Debug.Log("Exiting Silent State");
        characterState.StateIcons.RemoveItemByState(State);
        characterState.RemoveStateFromList(this);

        UnblockMagicAbilities();
    }

   /* public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            _duration = _baseDuration;
            Debug.Log($"Stacking Silent. Current stacks: {_currentStacks}, New duration: {_duration}s");
            return true;
        }
        else
        {
            _duration = _baseDuration;
            Debug.Log($"Max stacks reached. Refreshing Silent duration: {_duration}s");
            return false;
        }
    }*/

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

    [Command] private void CmdStateSilent(Character target) => ClientRpcStateSilent(target);
    [ClientRpc] private void ClientRpcStateSilent(Character target) { target.CharacterState.AddStateLogic(States.Silent, _duration, 0f, Schools.None, characterState.gameObject, null); }
}
