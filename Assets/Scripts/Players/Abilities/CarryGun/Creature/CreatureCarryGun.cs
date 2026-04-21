using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class CreatureCarryGun : NetworkComponent
{
    [SerializeField] private SkillManager _skillManager;

    [SerializeField] private List<Skill> _sills;
    [SerializeField][ReadOnly] private WombSpawn _dadSkill;

    public WombSpawn DadSkill { get => _dadSkill; set => _dadSkill = value; }

    private void Start()
    {
        subscription();

        Invoke("HandleAction", 1f);
    }

    private void OnDisable()
    {
        unsubscribe();
    }

    private void subscription()
    {
        if (_dadSkill == null) return;

        foreach (Skill skill in _sills)
        {
            if (skill is InjectionAdrenaline || skill is ParalyzingTentacles || skill is ThrowingBlow) _dadSkill.OnEffectTentaclesCreatures += SkillActivationInjectionAdrenaline;
        }
    }

    private void unsubscribe()
    {
        if (_dadSkill == null) return;

        foreach (Skill skill in _sills)
        {
            if (skill is InjectionAdrenaline || skill is ParalyzingTentacles || skill is ThrowingBlow) _dadSkill.OnEffectTentaclesCreatures -= SkillActivationInjectionAdrenaline;
        }
    }

    private void HandleAction()
    {
        if (_dadSkill != null) SkillActivationInjectionAdrenaline(_dadSkill.IsEffectTentaclesCreatures);
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