using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class InnerDarkness : RefreshingStateStacking
{
    private const float TimeDecreasePerStack = 2f;
    private float _durationRemaining;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.InnerDarkness;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;
    public override float RemainingDuration => _durationRemaining;

    public InnerDarkness()
    {
        SetMaxStacks(6);
    }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        
        _durationRemaining = durationToExit;
        var terrifyingElfAura = personWhoMadeBuff.GetComponent<TerrifyingElfAura>();

        if (personWhoMadeBuff != null && terrifyingElfAura.IsReductionRecharge)
        {
            SkillManager caster = personWhoMadeBuff.Abilities;
            foreach (Skill skill in caster.Abilities)
            {
                bool isDark = skill.Info.School == Schools.Dark;
                bool isSpellish = skill.Info.AbilityForm == AbilityForm.Magic || skill.Info.AbilityForm == AbilityForm.Both;

                if (isDark && isSpellish && skill.Cooldown.IsActive)
                {
                    float duration = skill.Cooldown.RemainingTime * 0.5f;
                    skill.Cooldown.Modify(-duration);
                }
            }
        }
    }

    public override void UpdateState()
    {
        if (_durationRemaining <= 0) ExitState();
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
        currentStacksCount = 0;
    }

    public override bool Stack(float time)
    {
        Debug.Log($"CurrentStacksCount: {currentStacksCount}");

        if(currentStacksCount < MaxStacksCount)
        {
            AddNewStack(time);
            return true;
        }

        else if (currentStacksCount == MaxStacksCount)
        {
            UpdateDurationForMaxStacks(time);
            return false;
        }

        return false;
    }

    private void AddNewStack(float time)
    {
        currentStacksCount++;

        if (currentStacksCount == MaxStacksCount) CmdStateFear();

        _durationRemaining = time - (currentStacksCount - 1) * TimeDecreasePerStack;
    }

    private void UpdateDurationForMaxStacks(float time)
    {
        _durationRemaining = time - (currentStacksCount - 1) * TimeDecreasePerStack;
        CmdStateFear();
        Debug.Log("обновление при максимальном стаке");
    }
    
    

    [Command] private void CmdStateFear() => ClientRpcStateFear();
    [ClientRpc] private void ClientRpcStateFear() { characterState.AddStateLogic(States.Fear, Random.Range(0.7f, 1.4f), 0f, Schools.None, sourceCaster.gameObject, null); }
}
