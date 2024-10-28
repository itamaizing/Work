using System.Collections;
using Mirror;
using UnityEngine;

public class SoulAid : Skill
{
    [SerializeField] private float _speed = 0.0025f;
    [SerializeField] private float _cooldownReduceValue = 5f;
    [SerializeField] private float _radiusMultiplier = 2f;
    [SerializeField] private PriestShield _priestShield;
    [SerializeField] private Restoration _restoration;
    
    private Character _target;
    private GameObject _tempTarget;
    private MoveComponent _tempTargetMove;
    
    private bool _talentTiredSoulDispelActive = false;
    private bool _talentCooldownReduce = false;
    private bool _talentDoubleRange = false;

    private void OnEnable()
    {
        CastEnded += DispelTiredSoul;
        _priestShield.CastEnded += ReduceCooldown;
    }

    private void OnDisable()
    {
        CastEnded -= DispelTiredSoul;
        _priestShield.CastEnded -= ReduceCooldown;
    }

    protected override bool IsCanCast
    {
        get
        {
            var isTargetInRadius = IsTargetInRadius(Radius, _target.transform) || IsTargetHaveRestoration() && IsTargetInRadius(Radius * _radiusMultiplier, _target.transform);
            return isTargetInRadius && TryPayCost() && IsTargetHaveTiredSoul();
        }
    }

    protected override IEnumerator CastJob()
    {
        while (_target != null && _target != Hero && Vector2.Distance(transform.position, _target.transform.position) > 2.1f)
        {
            Vector2 direction = (transform.position - _target.transform.position).normalized;
            Vector2 pullForce = direction * (_speed * Time.fixedTime);

            CmdPull(_target.gameObject, pullForce);
            yield return new WaitForFixedUpdate();
        }
    }

    protected override void ClearData()
    {
        _target = null;
    }

    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget(_talentTiredSoulDispelActive);
            }
            yield return null;
        }
    }

    public void EnableTiredSoulDispel(bool isActive)
    {
        _talentTiredSoulDispelActive = isActive;
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
        return _target != null && _target.CharacterState.CheckForState(States.TiredSoul);
    }

    private bool IsTargetHaveRestoration()
    {
        if (!_talentDoubleRange || _target == null || _restoration.Target == null) return false;

        return _restoration.Target == _target;
    }

    private void DispelTiredSoul()
    {
        if(!IsTargetHaveTiredSoul()) return;
        
        _target.CharacterState.CmdRemoveState(States.TiredSoul);
    }

    private void ReduceCooldown()
    {
        if(!_talentCooldownReduce) 
            return;
        
        DecreaseSetCooldown(_cooldownReduceValue);
    }

    [Command]
    private void CmdPull(GameObject gameObject, Vector2 force)
    {
        if (_tempTarget != gameObject)
        {
            _tempTarget = gameObject;
            _tempTargetMove = gameObject.GetComponent<MoveComponent>();
        }
        _tempTargetMove.TargetRpcAddTransformPosition(force);
    }
}
