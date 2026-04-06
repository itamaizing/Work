using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class SoulAid : Skill
{
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _cooldownReduceValue = 5f;
    [SerializeField] private float _defaultRadius = 4f;
    [SerializeField] private float _largeRadius = 8f;
    [SerializeField] private PriestShield _priestShield;
    [SerializeField] private Restoration _restoration;
    
    //private Character _target;
    private GameObject _tempTarget;
    private MoveComponent _tempTargetMove;
    
    private bool IsAllyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Allies");
    
    #region TiredSoul Talent
    private SoulTiredDispelBooster _tiredSoulBooster;
    public SoulTiredDispelBooster TiredSoulBooster => _tiredSoulBooster;
    #endregion
    private bool _talentCooldownReduce = false;
    private bool _talentDoubleRange = false;
    
    private float _clickRadius = 0.5f;

    private void OnEnable()
    {
        _priestShield.CastEnded += ReduceCooldown;
        
        _tiredSoulBooster = new SoulTiredDispelBooster(this);
    }

    private void OnDisable()
    {
        _priestShield.CastEnded -= ReduceCooldown;
    }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast
    {
        get 
        {
            var target = Targeting.GetTarget()?.Character;
            if (target == null) return false;

            bool inRange = Targeting.IsTargetInRadius(_talentDoubleRange ? _largeRadius : _defaultRadius, target.transform);
            bool hasTiredSoul = target.CharacterState.CheckForState(States.TiredSoul);

            if (_tiredSoulBooster.Enabled) { return inRange && _tiredSoulBooster.CanCastOnTarget(target); }
            
            return inRange && hasTiredSoul;
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character == null || !IsCanCast) yield break;

        var target = Targeting.GetTarget()?.Character.gameObject;
        CmdDispelTiredSoul(target,_tiredSoulBooster.Enabled);
        CmdStartPull(target);
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        //_target = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            AreaInfo.Radius = _talentDoubleRange ? _largeRadius : _defaultRadius;
            
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();
                
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);
                
                if (Targeting.GetTempTarget().Character is Character character)
                {
                    if (Targeting.GetTempTarget().Character != null && !IsAllyTarget(character))
                    {
                        Targeting.ClearTempTarget();
                    }
                    else
                    {
                        Targeting.GetTempTarget().Character.SelectedCircle.IsActive = true;
                        _hero.Move.LookAtTransform(Targeting.GetTempTarget().Character.transform);
                    }
                }
            }
            yield return null;
        }
        TargetInfo targetInfo = new TargetInfo();
        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    public void EnableCooldownReduce(bool isActive)
    {
        _talentCooldownReduce = isActive;
    }

    public void EnableDoubleRange(bool isActive)
    {
        _talentDoubleRange = isActive;
    }

    private bool IsTargetHaveTiredSoul()
    {
        return Targeting.GetTarget()?.Character != null && Targeting.GetTarget().Character.CharacterState.CheckForState(States.TiredSoul);
    }

    private bool IsTargetHaveRestoration()
    {
        if (!_talentDoubleRange || Targeting.GetTarget()?.Character == null || _restoration.Target == null) return false;
        
        return _restoration.Target == Targeting.GetTarget()?.Character;
    }

    private void ReduceCooldown()
    {
        if(!_talentCooldownReduce) 
            return;
        
        DecreaseSetCooldown(_cooldownReduceValue);
        Cooldown.Modify(-_cooldownReduceValue);
    }

    [Command]
    private void CmdStartPull(GameObject targetObj)
    {
        StartCoroutine(ServerPullCoroutine(targetObj));
    }
    
    private IEnumerator ServerPullCoroutine(GameObject targetObj)
    {
        var targetTransform = targetObj.transform;
        var targetMove = targetObj.GetComponent<MoveComponent>();
        if (targetMove == null) yield break;

        bool originalCanMove = targetMove.CanMove;
        targetMove.SetCanMove(false);

        while (Vector2.Distance(transform.position, targetTransform.position) > 0.01f)
        {
            Vector3 direction = (transform.position - targetTransform.position).normalized;
            Vector3 pullForce = direction * (2 * Time.fixedDeltaTime);

            if (targetMove.Rigidbody != null)
            {
                targetMove.Rigidbody.MovePosition(targetTransform.position + pullForce);
            }
            else
            {
                targetTransform.position += pullForce;
            }

            RpcApplyPullForce(targetObj, pullForce);

            yield return new WaitForFixedUpdate();
        }

        targetMove.SetCanMove(originalCanMove);
    }

    [ClientRpc]
    private void RpcApplyPullForce(GameObject targetObj, Vector3 force)
    {
        var targetTransform = targetObj.transform;
        var targetMove = targetObj.GetComponent<MoveComponent>();
        if (targetMove == null) return;

        if (targetMove.Rigidbody != null)
        {
            targetMove.Rigidbody.MovePosition(targetTransform.position + force);
        }
        else
        {
            targetTransform.position += force;
        }
    }

    [Command]
    private void CmdDispelTiredSoul(GameObject target, bool isEnabled)
    {
        _tiredSoulBooster.TryRemoveTiredSoul(target, isEnabled);
    }
}
