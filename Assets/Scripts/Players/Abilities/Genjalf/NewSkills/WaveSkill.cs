using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class WaveSkill : Skill
{
    [SerializeField] private ParticleSystem _particle;
    [SerializeField] private float _pushRange = 1f;
    [SerializeField] private float _pushDuration = 0.33f;
    [SerializeField] private Transform _previewPivot;
    [SerializeField] private BoxArea _lineVisual;
    
    private float _bonusLength = 0;
    private float _bonusWidth = 0f;
    private float _initialLenght = 2.5f;
    private float _initialWidth = 1;

    private Vector3 _waveStartPoint;
    private Vector3 _waveDirection;

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

    public void SetBonusSize(Vector2 bonusSize)
    {
        _bonusLength = bonusSize.x; 
        _bonusWidth = bonusSize.y;

        CastLength = _initialWidth + _bonusWidth ;
        CastWidth = _initialLenght + _bonusLength;
    }

    protected override IEnumerator CastJob()
    {
        CmdSetActiveParticle(true);

        Vector3 waveCenter = _waveStartPoint + _waveDirection * (AreaInfo.CastLength / 2f);

        var colliders = Physics.OverlapBox(
            waveCenter,
            new Vector3(AreaInfo.CastWidth / 2f, 2f, AreaInfo.CastLength / 2f),
            Quaternion.LookRotation(_waveDirection),
            Targeting.Layer
        );

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out Character enemy))
            {
                Vector3 toEnemy = enemy.transform.position - _waveStartPoint;
                toEnemy.y = 0;

                float distanceAlongWave = Vector3.Dot(toEnemy, _waveDirection);
                if (distanceAlongWave < 0 || distanceAlongWave > AreaInfo.CastLength)
                    continue;

                Vector3 perpendicular = Vector3.Cross(_waveDirection, Vector3.up);
                float distanceFromCenter = Mathf.Abs(Vector3.Dot(toEnemy, perpendicular));
                if (distanceFromCenter > AreaInfo.CastWidth / 2f)
                    continue;

                float casterRadius = ((CapsuleCollider)_hero.Collider).radius;
                float enemyRadius = ((CapsuleCollider)enemy.Collider).radius;

                float centerDist = Vector3.Distance(_waveStartPoint, enemy.transform.position);
                float edgeDist = Mathf.Max(centerDist - (casterRadius + enemyRadius), 0f);

                float damageMul = Mathf.Clamp01(1f - edgeDist / AreaInfo.CastLength);

                Damage scaledDamage = new Damage
                {
                    Value = Buff.Damage.GetBuffedValue(Damage) * damageMul,
                    Type = Info.DamageType,
                    PhysicAttackType = Info.AttackRangeType,
                };

                CmdApplyDamage(scaledDamage, enemy.gameObject);

                float distToPush = _pushRange;
                Vector3 pointForPush = enemy.transform.position + _waveDirection * distToPush;

                CmdMoveTarget(enemy.gameObject, pointForPush, _pushDuration);
            }
        }

        yield return new WaitForSeconds(0.6f);
        CmdSetActiveParticle(false);
        _skillRender.ResetCursor();
    }

    protected override void ClearData()
    {
        _waveStartPoint = Vector3.zero;
        _waveDirection = Vector3.zero;
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
    }

    public override void StartCustomDraw()
    {
        SkillRender.DrawRadius(AreaInfo.Radius);
    }

    public override void StopCustomDraw()
    {
        SkillRender.StopDrawRadius();
        _lineVisual.gameObject.SetActive(false);
    }

    public override IEnumerator CustomDrawJob(float time = 0.2f)
    {
        if (_lineVisual == null)
        {
            yield break;
        }

        Transform pivotTransform = _previewPivot;
        
        _lineVisual.gameObject.SetActive(true);

        Damage damage = new Damage
        {
            Value = Damage,
            Type = Info.DamageType,
        };

        while (true)
        {
            Vector3 mousePoint = Targeting.GetMousePoint();

            if (mousePoint == Vector3.zero)
            {
                yield return null;
                continue;
            }

            Vector3 directionToMouse = mousePoint - transform.position;
            directionToMouse.y = 0;

            if (directionToMouse.magnitude < 0.1f)
            {
                yield return null;
                continue;
            }

            float distance = directionToMouse.magnitude;
            Vector3 direction = directionToMouse.normalized;

            Vector3 startPosition = transform.position + direction * Mathf.Min(distance, AreaInfo.Radius);

            pivotTransform.position = startPosition;
            pivotTransform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            _lineVisual.SetSize(AreaInfo.CastWidth, AreaInfo.CastLength, damage);

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

        Vector3 clickPoint = Targeting.GetMousePoint();

        Vector3 directionToClick = clickPoint - transform.position;
        directionToClick.y = 0;

        if (directionToClick.magnitude > AreaInfo.Radius)
        {
            directionToClick = directionToClick.normalized * AreaInfo.Radius;
        }

        _waveStartPoint = transform.position + directionToClick;
        _waveDirection = directionToClick.normalized;

        Vector3 waveEndPoint = _waveStartPoint + _waveDirection * AreaInfo.CastLength;

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
        RpcSetActiveParticle(status);
    }

    [ClientRpc]
    private void RpcSetActiveParticle(bool status)
    {
        if (_particle == null) return;

        _particle.gameObject.SetActive(status);
    }
}
