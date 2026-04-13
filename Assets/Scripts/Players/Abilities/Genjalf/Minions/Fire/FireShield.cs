using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FireShield : MoveSkill
{
    [SerializeField] private Shield _shieldPref;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("FireShield");
    protected override bool IsCanCast => CheckCanCast();

    private float _clickRadius = 0.5f;
    
    private GameObject _shield;

    [SerializeField] private float _baffDuration = 9;

    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
    
    public void AnimCastFireShield()
    {
        AnimStartCastCoroutine();
    }

    public void AnimFireShieldEnd()
    {
        AnimCastEnded();
    }

    private bool CheckCanCast()
    {
        return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius && Targeting.GetTarget()?.Character != null;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
        
        if (!IsCanCast)
        {
            MoveTo();
        }
    }
    
    private void OnEnable()
    {
        Canceled += CancelMove;
    }

    private void OnDisable()
    {
        Canceled -= CancelMove;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        while (Targeting.GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();
        
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);
                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (Targeting.GetTempTarget()?.Character != null && !IsAllyTarget(character))
                    {
                        Targeting.ClearTempTarget();
                    }
                    else
                    {
                        if (character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }
        targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
        Targeting.ClearTempTarget();
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character != null)
        {
            CmdAddShield(Targeting.GetTarget()?.Character.gameObject);
        }

        yield return null;
    }
    
    [Command]
    private void CmdAddShield(GameObject targetShield)
    {
        if(targetShield == null) return;
        
        var shield = Instantiate(_shieldPref, targetShield.transform.position, Quaternion.identity);
        NetworkServer.Spawn(shield.gameObject);
        _shield = shield.gameObject;
        ClientRpcShieldFollow(_shield,targetShield.transform);
        StartCoroutine(ShieldJob(_shield.gameObject));
        targetShield.GetComponent<Character>().CharacterState.AddState(States.Burn, _baffDuration, 0, Hero.gameObject, name);
    }

    private IEnumerator ShieldJob(GameObject shield)
    {
        yield return new WaitForSeconds(_baffDuration);
        
        if (shield != null)
        {
            NetworkServer.Destroy(shield.gameObject);
        }
    }
    
    [ClientRpc]
    private void ClientRpcShieldFollow(GameObject shield, Transform target)
    {
        shield.GetComponent<Shield>().FollowTo(target);
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Hero.Move.StopLookAt();
    }
}
