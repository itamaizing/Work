using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChainLightning : Skill
{
    [SerializeField] private ParticleSystem _particlePref;
    [SerializeField] private int _maxJumps = 5;
    [SerializeField] private float _damageReductionPerJump = 0.20f;
    [SerializeField] private float _jumpDelay = 0.1f;
    [SerializeField] private float _jumpDistance = 3f;

    //private Character _target;

    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;
    
    private float _clickRadius = 0.5f;
    
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    private bool CheckCanCast()
    {
        return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius;
    }

    public void AnimCastLight()
    {
        AnimStartCastCoroutine();
    }

    public void AnimLightEnd()
    {
        AnimCastEnded();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator CastJob()
    {
        Character current = Targeting.GetTarget()?.Character;
        if (current == null) yield break;

        StartCoroutine(RunChainJumps(current));

        yield return null;
    }
    
    private IEnumerator RunChainJumps(Character current)
    {
        List<Character> hitTargets = new List<Character>();
        float currentDamageMult = 1f;
    
        for (int i = 0; i <= _maxJumps; i++)
        {
            if (current == null) break;
            if (hitTargets.Contains(current)) break;
    
            hitTargets.Add(current);
    
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage) * currentDamageMult,
                Type = Info.DamageType,
                School = Schools.Air
            };
    
            CmdCreateParticle(current.Position);
            CmdApplyDamage(damage, current.gameObject);
    
            var discharge = current.CharacterState.GetState(States.Discharge);
            if (discharge != null)
            {
                foreach (var target in FindNearestTarget(current, hitTargets))
                    CmdApplyDischarge(target.gameObject, discharge.RemainingDuration);
            }
    
            current = FindNextTarget(current, hitTargets);
            currentDamageMult *= (1f - _damageReductionPerJump);
    
            if (i < _maxJumps && current != null)
                yield return new WaitForSeconds(_jumpDelay);
        }
    }
    
    private Character FindNextTarget(Character current, List<Character> alreadyHit)
    {
        if (current == null) return null;

        Collider[] nearby = Physics.OverlapSphere(current.transform.position, _jumpDistance, Targeting.Layer);

        Character target = null;
        float bestDist = float.MaxValue;

        foreach (var col in nearby)
        {
            if (!col.TryGetComponent<Character>(out var enemy)) continue;
            if (enemy == current || enemy == Hero || alreadyHit.Contains(enemy)) continue;
            if (!IsEnemyTarget(enemy)) continue;

            float dist = Vector3.Distance(current.transform.position, enemy.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                target = enemy;
            }
        }

        return target;
    }
    
    
    private List<Character> FindNearestTarget(Character current, List<Character> alreadyHit)
    {
        if (current == null) return null;

        Collider[] nearby = Physics.OverlapSphere(current.transform.position, _jumpDistance, Targeting.Layer);
        List<Character> targets = new();

        foreach (var col in nearby)
        {
            if (!col.TryGetComponent<Character>(out var enemy)) continue;
            if (enemy == current || enemy == Hero) continue;
            if (alreadyHit.Contains(enemy)) continue;
            if (!IsEnemyTarget(enemy)) continue;

            targets.Add(enemy);
        }

        return targets;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: false);

                var temp = Targeting.GetTempTarget()?.Character;
                if (temp != null && !IsEnemyTarget(temp))
                    Targeting.ClearTempTarget();
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
        callbackDataSaved(targetInfo);
        Targeting.ClearTempTarget();
    }

    [Command]
    private void CmdApplyDischarge(GameObject target, float duration)
    {
        target.GetComponent<CharacterState>()?.AddState(States.Discharge, duration, 0f, _hero.gameObject, Name);
    }
    
    private void CreateParticle(Vector3 position)
    {
        GameObject item = Instantiate(_particlePref.gameObject, position, Quaternion.identity);
    }

    [Command]
    protected void CmdCreateParticle(Vector3 position)
    {
        RpcCreateParticle(position);
    }

    [ClientRpc]
    private void RpcCreateParticle(Vector3 position)
    {
        CreateParticle(position);
    }
}
