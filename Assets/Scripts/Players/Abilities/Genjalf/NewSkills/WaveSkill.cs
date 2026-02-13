using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class WaveSkill : Skill
{
    [SerializeField] private ParticleSystem _particle;
    [SerializeField] private float _pushRange = 1f;
    [SerializeField] private float _pushDuration = 0.33f;
    [SerializeField] private AbilityLineRenderer _linePrefab;

    private Vector3 _waveStartPoint;
    private Vector3 _waveDirection;
    private Transform _dynamicTransform;
    private GameObject _dynamicPivot;
    private BoxArea _lineVisual;

    public override string AdditionalDescription => "";

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count >= 2)
        {
            _waveStartPoint = targetInfo.Points[0];
            Vector3 endPoint = targetInfo.Points[1];
            _waveDirection = (endPoint - _waveStartPoint).normalized;
        }
    }

    protected override IEnumerator CastJob()
    {
        CmdSetActiveParticle(true);

        Vector3 waveCenter = _waveStartPoint + _waveDirection * (CastLength / 2f);

        var colliders = Physics.OverlapBox(
            waveCenter,
            new Vector3(CastWidth / 2f, 2f, CastLength / 2f),
            Quaternion.LookRotation(_waveDirection),
            TargetsLayers
        );

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out Character enemy))
            {
                Vector3 toEnemy = enemy.transform.position - _waveStartPoint;
                toEnemy.y = 0;

                float distanceAlongWave = Vector3.Dot(toEnemy, _waveDirection);
                if (distanceAlongWave < 0 || distanceAlongWave > CastLength)
                    continue;

                Vector3 perpendicular = Vector3.Cross(_waveDirection, Vector3.up);
                float distanceFromCenter = Mathf.Abs(Vector3.Dot(toEnemy, perpendicular));
                if (distanceFromCenter > CastWidth / 2f)
                    continue;

                float casterRadius = ((CapsuleCollider)_hero.Collider).radius;
                float enemyRadius = ((CapsuleCollider)enemy.Collider).radius;

                float centerDist = Vector3.Distance(_waveStartPoint, enemy.transform.position);
                float edgeDist = Mathf.Max(centerDist - (casterRadius + enemyRadius), 0f);

                float damageMul = Mathf.Clamp01(1f - edgeDist / CastLength);

                Damage scaledDamage = new Damage
                {
                    Value = Buff.Damage.GetBuffedValue(Damage) * damageMul,
                    Type = DamageType,
                    PhysicAttackType = AttackRangeType,
                };

                CmdApplyDamage(scaledDamage, enemy.gameObject);

                float distToPush = _pushRange;
                Vector3 pointForPush = enemy.transform.position + _waveDirection * distToPush;

                CmdMoveTarget(enemy.gameObject, pointForPush, _pushDuration);
            }
        }

        yield return new WaitForSeconds(0.2f);
        CmdSetActiveParticle(false);
    }

    protected override void ClearData()
    {
        _waveStartPoint = Vector3.zero;
        _waveDirection = Vector3.zero;
        ClearTarget();
        ClearTempTarget();

        if (_lineVisual != null)
        {
            Destroy(_lineVisual.gameObject);
            _lineVisual = null;
        }

        if (_dynamicPivot != null)
        {
            Destroy(_dynamicPivot);
            _dynamicPivot = null;
            _dynamicTransform = null;
        }
    }

    protected override void StartAutoDraw()
    {
        SkillRender.DrawRadius(Radius);
    }

    protected override void StopAutoDraw()
    {
        SkillRender.StopDrawRadius();

        if (_lineVisual != null)
        {
            Destroy(_lineVisual.gameObject);
            _lineVisual = null;
        }

        if (_dynamicPivot != null)
        {
            Destroy(_dynamicPivot);
            _dynamicPivot = null;
            _dynamicTransform = null;
        }
    }

    protected override IEnumerator DynamicRendererJob(float time = 0.2f)
    {
        if (SkillRender == null || _linePrefab == null) yield break;

        var pivot = new GameObject("WavePreviewPivot").transform;
        pivot.SetParent(transform, false);

        _lineVisual = Instantiate(_linePrefab.Start, pivot);

        _lineVisual.SetColor(new Color(1f, 1f, 0.4f, 0.7f));

        Damage damage = new Damage();

        while (true)
        {
            Vector3 mouse = GetMousePoint();
            if (mouse == Vector3.zero) 
            {
                yield return null;
                continue;
            }

            Vector3 toMouse = mouse - transform.position;
            toMouse.y = 0;

            float dist = toMouse.magnitude;
            Vector3 dir = toMouse.normalized;

            Vector3 startPos = transform.position + dir * Mathf.Min(dist, Radius);
            pivot.position = startPos;

            pivot.rotation = Quaternion.LookRotation(dir, Vector3.up);

            _lineVisual.SetSize(CastWidth, CastLength, damage);

            yield return null;
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new();

        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        Vector3 clickPoint = GetMousePoint();

        Vector3 directionToClick = clickPoint - transform.position;
        directionToClick.y = 0;

        if (directionToClick.magnitude > Radius)
        {
            directionToClick = directionToClick.normalized * Radius;
        }

        _waveStartPoint = transform.position + directionToClick;
        _waveDirection = directionToClick.normalized;

        Vector3 waveEndPoint = _waveStartPoint + _waveDirection * CastLength;

        targetInfo.Points.Add(_waveStartPoint);
        targetInfo.Points.Add(waveEndPoint);
        callbackDataSaved(targetInfo);
    }

    [Command]
    private void CmdMoveTarget(GameObject target, Vector3 point, float time)
    {
        var enemyMove = target.GetComponent<MoveComponent>();
        enemyMove.RpcDoPush(point, time);
    }

    [Command]
    private void CmdSetActiveParticle(bool status)
    {
        ClientRpcSetActiveParticle(status);
    }

    [ClientRpc]
    private void ClientRpcSetActiveParticle(bool status)
    {
        _particle.gameObject.SetActive(status);
    }
}
