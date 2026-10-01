using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightningStrike : Skill
{
    [SerializeField] private ParticleSystem _particlePref;
    
    [SerializeField] private LineRenderer _beamPrefab;
    [SerializeField, Range(0, 100)] private int _debuffChance = 15;
    [SerializeField, Range(0.05f, 0.5f)] private float _chainDelay = 0.15f;
    [SerializeField] private float _beamHeightOffset = 0.8f;
    [SerializeField,Range(0, 1)] private float _damageReductionPercent = 0.8f;
    [SerializeField] private int _maxChainTargets = 3;

    protected override bool IsCanCast { get => CheckCanCast(); }

    private bool _isChaining = false;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private float _clickRadius = 0.5f;
   
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    public void EnableChain(bool value)
    {
        _isChaining = value;
    }

    private bool CheckCanCast()
    {
        return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius && Targeting.GetTarget()?.Character != null;
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
        var target = Targeting.GetTarget()?.Character;
       
        if (target != null)
        {
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage),
                Type = Info.DamageType
            };

            CmdApplyDamage(damage, target.gameObject);
            CmdCreateParticle(target.Position);

            if (target.CharacterState.GetState(States.Discharge) != null && _isChaining)
            {
                var chainTargets = new List<GameObject>();
                var chainDamageValues = new List<float>();
                var fromPositions = new List<Vector3>();

                Vector3 prevPos = target.Position;
                float currentDamageValue = damage.Value;
                var visited = new HashSet<Character> { target };

                for (int i = 0; i < _maxChainTargets; i++)
                {
                    Character next = FindNextChainTarget(prevPos, visited, 3f);
                    if (next == null) break;

                    visited.Add(next);
                    currentDamageValue *= _damageReductionPercent;

                    chainTargets.Add(next.gameObject);
                    chainDamageValues.Add(currentDamageValue);
                    fromPositions.Add(prevPos);

                    prevPos = next.Position;
                }

                if (chainTargets.Count > 0)
                    CmdApplyChainLightning(chainTargets.ToArray(), chainDamageValues.ToArray(), damage.Type, fromPositions.ToArray());
            }

            if (Random.Range(1, 100) <= _debuffChance)
            {
                CmdAddState(target);
            }
            Targeting.ClearTarget();
        }
        yield return null;
    }
   
    [Command] private void CmdAddState(Character target) => target.CharacterState.AddState(States.Discharge, 2, 0, Hero.gameObject, name);

    private Character FindNextChainTarget(Vector3 fromPosition, HashSet<Character> visited, float maxDistance)
    {
        Collider[] hits = Physics.OverlapSphere(fromPosition, maxDistance);

        Character best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var ch)) continue;
            if (ch.IsDead) continue;
            if (visited.Contains(ch)) continue;
            if (!IsEnemyTarget(ch)) continue;

            float dist = Vector3.Distance(fromPosition, ch.Position);
            if (dist < bestDist && dist > 0.1f)
            {
                bestDist = dist;
                best = ch;
            }
        }
        return best;
    }

    [Command]
    private void CmdApplyChainLightning(GameObject[] targets, float[] damageValues, DamageType damageType, Vector3[] fromPositions)
    {
        StartCoroutine(ApplyChainOnServer(targets, damageValues, damageType, fromPositions));
    }

    private IEnumerator ApplyChainOnServer(GameObject[] targets, float[] damageValues, DamageType damageType, Vector3[] fromPositions)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;

            yield return new WaitForSeconds(_chainDelay);

            if (!targets[i].TryGetComponent<Character>(out var ch))
                continue;

            Damage chainDamage = new Damage
            {
                Value = damageValues[i],
                Type = damageType
            };

            ApplyDamage(chainDamage, targets[i]);
            RpcCreateLightningBeam(fromPositions[i], ch.Position);
        }
    }

    [ClientRpc]
    private void RpcCreateLightningBeam(Vector3 start, Vector3 end)
    {
        CreateLightningBeam(start, end);
    }

    private void CreateLightningBeam(Vector3 start, Vector3 end)
    {
        if (Vector3.Distance(start, end) < 0.1f) return;

        LineRenderer lr = Instantiate(_beamPrefab);
        lr.gameObject.SetActive(true);

        start += Vector3.up * _beamHeightOffset;
        end += Vector3.up * _beamHeightOffset;
        
        Vector3 dir = (end - start).normalized;
        float length = Vector3.Distance(start, end);

        Vector3[] pos = new Vector3[7];
        pos[0] = start;
        pos[6] = end;

        for (int j = 1; j < 6; j++)
        {
            float t = j / 6f;
            Vector3 basePoint = Vector3.Lerp(start, end, t);

            Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;
            if (perp.sqrMagnitude < 0.01f)
                perp = Vector3.Cross(dir, Vector3.forward).normalized;

            float offset = Mathf.Sin(t * Mathf.PI * 4f) * (length * 0.13f);
            pos[j] = basePoint + perp * offset + Random.insideUnitSphere * (length * 0.04f);
        }

        lr.SetPositions(pos);

        Destroy(lr.gameObject, 0.35f);
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();
                
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);
                
                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (character != null && !IsEnemyTarget(character))
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
        callbackDataSaved(targetInfo);
        Targeting.ClearTempTarget();
    }

    private void CreateParticle(Vector3 position)
    {
        if (_particlePref != null)
        {
            Destroy(Instantiate(_particlePref.gameObject, position, Quaternion.identity),1f);
        }
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
