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
        return Vector3.Distance(GetTargetCharacter().transform.position, transform.position) <= Radius && GetTargetCharacter() != null;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        SetTarget((Character)targetInfo.GetTargets()[0]);
        
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
        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = GetMousePoint();
        
                FindTarget(_clickRadius, clickPoint, canTargetHimself: true);
                if (GetTempTargetCharacter() is Character character)
                {
                    if (GetTempTargetCharacter() != null && !IsAllyTarget(character))
                    {
                        ClearTempTarget();
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
        targetInfo.AddTarget(GetTempTargetCharacter());
        ClearTempTarget();
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (GetTargetCharacter() != null)
        {
            CmdAddShield(GetTargetCharacter().gameObject);
        }

        yield return null;
    }
    
    [Command]
    private void CmdAddShield(GameObject targetShield)
    {
        if(targetShield == null) return;
        
        var shield = Instantiate(_shieldPref, targetShield.transform.position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(shield.gameObject, ((MinionComponent)_hero).CharacterParent.NetworkSettings.MyRoom);
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
        ClearTarget();
        Hero.Move.StopLookAt();
    }
}
