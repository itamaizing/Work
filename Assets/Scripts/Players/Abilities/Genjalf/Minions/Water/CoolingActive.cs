using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class CoolingActive : Skill
{
    [SerializeField] private ParticleSystem _effectObject;
    [SerializeField] private CoolingAura _coolingAura;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => CheckCanCast();
    
    private float _clickRadius = 0.5f;
    private float _auraDuration = 5f;
    
    private GameObject _shield;
    
    private Vector3 _clickPoint = Vector3.zero;

    private bool CheckCanCast()
    {
        return true;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        while (_clickPoint == Vector3.zero)
        {
            if (GetMouseButton)
            {
                _clickPoint = Targeting.GetMousePoint();
            }
            yield return null;
        }
        targetInfo.AddTarget(_hero);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        _coolingAura.ActivateAura(true,_auraDuration);
        CmdStartEffect();

        yield return null;
    }
    
    [Command]
    private void CmdStartEffect()
    {
        StartEffectJob();
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

        float duration = _auraDuration;
        float elapsed = 0f;

        Transform effectTransform = _effectObject.transform;
        
        _effectObject.gameObject.SetActive(true);
        
        var shape = _effectObject.shape;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            shape.radius = Mathf.Lerp(1f, duration, elapsed / duration);

            yield return null;
        }

        _effectObject.gameObject.SetActive(false);
        effectTransform.localScale = Vector3.one;
    }
    
    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Hero.Move.StopLookAt();
        _clickPoint = Vector3.zero;
    }
}
