using System.Collections;
using UnityEngine;

public class IncreaseLengthTalent : Skill
{
    #region Skill

    protected override IEnumerator CastJob()
    {
        throw new System.NotImplementedException();
    }

    protected override int AnimTriggerCastDelay { get; }
    protected override int AnimTriggerCast { get; }

    #endregion

    private AttributeModifier _modifier = new(Multiplier, ModifierType.Flat);
    private const float Multiplier = 1.5f;

    private bool visualsApplied = false;

    public bool IsHuntressTalent_1 => _isHuntressTalent_1;
    
    private bool _isHuntressTalent_1;

    public void EnableHuntressTalent_1(bool value)
    {
        if(_isHuntressTalent_1 == value) return;
        _isHuntressTalent_1 = value;
        
        if(_isHuntressTalent_1)
            OnEnter();
        else
            OnExit();
            
    }
    
    private void OnEnter()
    {
        _hero.Abilities.GetSkill<Ghost>().MovingToGhostWithZeroMana(true);
        _hero.VisionComponent.VisionRange += 3;

        foreach (Skill skill in _hero.Abilities.Abilities)
        {
            if (skill == null) continue;

            skill.Attributes[SkillAttributeName.Length].AddModifier(_modifier);
            skill.Attributes[SkillAttributeName.Radius].AddModifier(_modifier);
        }

        if (!visualsApplied && _hero.Abilities.Abilities.Count > 0)
        {
            if (_hero.Abilities.Abilities[0] != null &&
                _hero.Abilities.Abilities[0].TryGetComponent(out SkillRenderer renderer))
            {
                renderer.MultiplyCastVisuals(Multiplier);
                visualsApplied = true;
            }
        }
    }

    private void OnExit()
    {
        _hero.Abilities.GetSkill<Ghost>().MovingToGhostWithZeroMana(false);
        _hero.VisionComponent.VisionRange -= 3;

        foreach (Skill skill in _hero.Abilities.Abilities)
        {
            if (skill == null) continue;

            skill.Attributes[SkillAttributeName.Length].RemoveModifier(_modifier);
            skill.Attributes[SkillAttributeName.Radius].RemoveModifier(_modifier);
        }

        if (visualsApplied && _hero.Abilities.Abilities.Count > 0)
        {
            if (_hero.Abilities.Abilities[0] != null &&
                _hero.Abilities.Abilities[0].TryGetComponent(out SkillRenderer renderer))
            {
                renderer.DivideCastVisuals(Multiplier);
                visualsApplied = false;
            }
        }
    }
}
