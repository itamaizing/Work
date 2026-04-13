using System;
using System.Collections;
using UnityEngine;

public class GettingDispelPassive : Skill, IPassiveSkill
{
    [SerializeField] private ShadowSkill _shadowSkill;

    private bool _gettingDispelEnabled = false;

    public bool GettingDispelEnabled => _gettingDispelEnabled;

    public void EnableGettingDispel(bool value)
    {
        _gettingDispelEnabled = value;

        if (_gettingDispelEnabled)
        {
            Hero.CharacterState.OnStateDispelled += OnDispelled;
        }
        else
        {
            Hero.CharacterState.OnStateDispelled -= OnDispelled;
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo){ }
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) { return null; }

    protected override IEnumerator CastJob() { return null; }

    protected override void ClearData() { }

    private void OnDispelled(States states, int stackCount)
    {
        if (states == States.Destruction ||
            states == States.DestructionStacking ||
            states == States.SpiritHealth)
        {
            if (_shadowSkill.IsSkillActive)
            {
                _shadowSkill.AddChargers(stackCount);
            }
        }
    }
}
