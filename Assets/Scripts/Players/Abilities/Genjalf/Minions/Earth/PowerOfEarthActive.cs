using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class PowerOfEarthActive : Skill
{
    [SerializeField] private GameObject _effectObject;
    
    [SerializeField]private float _auraDuration = 6f;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("PowerOfEarth");
    protected override bool IsCanCast => CheckCanCast();

    private float _clickRadius = 0.5f;

    public void AnimCastPowerEarth()
    {
            
        AnimStartCastCoroutine();
    }

    public void AnimPowerEarthEnd()
    {
        AnimCastEnded();
    }   
    
    private bool CheckCanCast()
    {
        return true;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        
        CmdAddAura();

        yield return null;
    }
    
    [Command]
    private void CmdAddAura()
    {
        _hero.CharacterState.AddState(States.PowerOfEarth, _auraDuration, 0, Hero.gameObject, name);
        StartEffectJob();
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
    }
    
    [ClientRpc]
    private void StartEffectJob()
    {
        StartCoroutine(EffectJob());
    }
    
    private IEnumerator EffectJob()
    {
        if (_effectObject == null)
            yield break;

        _effectObject.gameObject.SetActive(true);

        yield return new WaitForSeconds(_auraDuration);

        _effectObject.gameObject.SetActive(false);
    }
}
