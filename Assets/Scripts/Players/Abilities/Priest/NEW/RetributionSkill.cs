using System;
using System.Collections;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class RetributionSkill : Skill, IPassiveSkill
{
    [SerializeField] private float _buffDuration = 6f;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private float _retributionChance = 20f;

    private bool _enabled;

    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) { yield break; }
    protected override IEnumerator CastJob() { yield break; }
    protected override void ClearData() { }

    public void OnActive(bool value)
    {
        if(_enabled == value) return;

        _enabled = value;
        
        if(_enabled)
            Hero.Health.DamageTaken += OnDamageGet;
        else
            Hero.Health.DamageTaken -= OnDamageGet;
    }

    private void OnDamageGet(Damage damage, Skill skill)
    {
        if(damage.Form != AbilityForm.Physical) return;

        if (Random.value * 100f >= _retributionChance) 
            return;

        CmdAddState();
    }

    [Command]
    private void CmdAddState()
    {
        _hero.CharacterState.AddState(States.Retribution,_buffDuration,0,_hero.gameObject,nameof(RetributionSkill));
    }
}
