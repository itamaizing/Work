using System.Collections.Generic;
using UnityEngine;

public class CreatureCarryGun : MonoBehaviour
{
    [SerializeField] private Tentacles _dadSkill;
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private Skill _skill;

    private void Start()
    {
        _dadSkill.OnInjectionAdrenaline += SwitchStateSkill;
    }

    private void OnDisable()
    {
        _dadSkill.OnInjectionAdrenaline -= SwitchStateSkill;
    }

    private void SwitchStateSkill(bool value)
    {
        if (value) _skillManager.ActivateSkill(_skill);
        else _skillManager.DeactivateSkill(_skill);
    }
}