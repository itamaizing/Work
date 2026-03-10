using System.Collections.Generic;
using UnityEngine;

public class CreatureCarryGun : MonoBehaviour
{
    [SerializeField] private Tentacles _dadSkill;
    [SerializeField] private SkillManager _skillManager;

    [SerializeField] private List<Skill> _sills;

    private void Start()
    {       
        foreach (Skill skill in _sills)
        {
            if (skill as InjectionAdrenaline) _dadSkill.OnInjectionAdrenaline += SkillActivationInjectionAdrenaline;
        }
    }

    private void OnDisable()
    {
        foreach (Skill skill in _sills)
        {
            if (skill as InjectionAdrenaline) _dadSkill.OnInjectionAdrenaline -= SkillActivationInjectionAdrenaline;
        }
    }

    private void SkillActivationInjectionAdrenaline(bool value)
    {
        foreach (Skill skill in _sills)
        {
            if (skill as InjectionAdrenaline)
            {
                if (value) _skillManager.ActivateSkill(skill);
                else _skillManager.DeactivateSkill(skill);
            }
         }
    }
}