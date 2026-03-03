using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ColdShield : MoveSkill
{
    [SerializeField] private Shield _shieldPref;
    [Range(0, 1)] [SerializeField] private float _physResistPercent = 0.1f;
    [SerializeField] private float _baffDuration = 39;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => CheckCanCast();

    private float _initialPhysResist;
    private float _clickRadius = 0.5f;
    
    private GameObject _shield;


    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
    
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

        Character character = targetShield.GetComponent<Character>();
        
        _initialPhysResist = character.Health.DefPhysDamage;
        
        var shield = Instantiate(_shieldPref, targetShield.transform.position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(shield.gameObject, ((MinionComponent)_hero).CharacterParent.NetworkSettings.MyRoom);
        NetworkServer.Spawn(shield.gameObject);
        _shield = shield.gameObject;
        ClientRpcShieldFollow(_shield,targetShield.transform);
        StartCoroutine(ShieldJob(_shield.gameObject,targetShield));
        character.Health.SetPhysicDef(_initialPhysResist + (_initialPhysResist * _physResistPercent));
        character.CharacterState.AddState(States.CoolingDamaged, _baffDuration, 0, Hero.gameObject, name);
    }

    private IEnumerator ShieldJob(GameObject shield,GameObject target)
    {
        yield return new WaitForSeconds(_baffDuration);
        target.GetComponent<Character>().Health.SetPhysicDef(_initialPhysResist);
        
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
