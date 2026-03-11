using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class CreatureCarryGun : NetworkComponent
{
    [SerializeField] private SkillManager _skillManager;

    [SerializeField] private List<Skill> _sills;
    [SerializeField][ReadOnly] private Tentacles _dadSkill;

    public Tentacles DadSkill { get => _dadSkill; set => _dadSkill = value; }

    private void Start()
    {
        if (_dadSkill == null) return;

        foreach (Skill skill in _sills)
        {
            if (skill is InjectionAdrenaline || skill is ParalyzingTentacles || skill is ThrowingBlow) _dadSkill.OnEffectTentaclesCreatures += SkillActivationInjectionAdrenaline;
        }
    }

    private void OnDisable()
    {
        if (_dadSkill == null) return;

        foreach (Skill skill in _sills)
        {
            if (skill is InjectionAdrenaline || skill is ParalyzingTentacles || skill is ThrowingBlow) _dadSkill.OnEffectTentaclesCreatures -= SkillActivationInjectionAdrenaline;
        }
    }

    private void SkillActivationInjectionAdrenaline(bool value)
    {
        foreach (Skill skill in _sills)
        {
            if (skill is InjectionAdrenaline || skill is ParalyzingTentacles || skill is ThrowingBlow)
            {
                if (value) _skillManager.ActivateSkill(skill);
                else _skillManager.DeactivateSkill(skill);
            }
         }
    }
}