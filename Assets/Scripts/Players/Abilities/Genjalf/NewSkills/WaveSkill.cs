using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class WaveSkill : Skill
{
    [SerializeField] private GameObject _waveParticlePrefab;
    [SerializeField] private float _pushRange = 1f;
    [SerializeField] private float _pushDuration = 0.33f;
    [SerializeField] private Transform _previewPivot;
    [SerializeField] private BoxArea _lineVisual;

    private AttributeModifier _modifierLenght = new AttributeModifier(0,ModifierType.Flat);
    private AttributeModifier _modifierWidth = new AttributeModifier(0,ModifierType.Flat);

    private Vector3 _waveStartPoint;
    private Vector3 _originalDirection = Vector3.forward; 
    private Vector3 _waveDirection;

    public override string AdditionalDescription => "";

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => CheckIsCanCast();
    
    private bool CheckIsCanCast()
    {
        if (_waveStartPoint == Vector3.zero)
            return true;

        Vector3 currentPos = transform.position;
        currentPos.y = 0f;

        Vector3 startPos = _waveStartPoint;
        startPos.y = 0f;

        float distance = Vector3.Distance(currentPos, startPos);

        return distance <= AreaInfo.Radius;
    }

    private bool _isBonusSizeEnabled;
    
    private void OnEnable()
    {
        Canceled += OnSkillCancel;
    }

    private void OnDisable()
    {
        Canceled -= OnSkillCancel;
    }
    
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        StopCustomDraw();
       
        if (targetInfo.Points.Count >= 2)
        {
            _waveStartPoint = targetInfo.Points[0];
            Vector3 endPoint = targetInfo.Points[1];
        }
    }

    public void SetSizeModifier(Vector2 bonusSize)
    {
        if (!_isBonusSizeEnabled)
        {
            _modifierWidth.Value = bonusSize.x;
            _modifierLenght.Value = bonusSize.y;
            _modifierLenght.Type = ModifierType.Flat;
            _modifierWidth.Type = ModifierType.Flat;

            _skillAttributes.Attributes[SkillAttributeName.Length].AddModifier(_modifierLenght);
            _skillAttributes.Attributes[SkillAttributeName.Width].AddModifier(_modifierWidth);
            _isBonusSizeEnabled = true;
        }
    }

    public void RemoveSizeModifier()
    {
        _skillAttributes.Attributes[SkillAttributeName.Length].RemoveModifier(_modifierLenght);
        _skillAttributes.Attributes[SkillAttributeName.Width].RemoveModifier(_modifierWidth);
        _isBonusSizeEnabled = false;
    }

    protected override IEnumerator CastJob()
    {
        StartCoroutine(WaveJob());
        yield return null;
    }

    private IEnumerator WaveJob()
    {
        Vector3 heroPosFlat = transform.position;
        heroPosFlat.y = 0f;

        Vector3 startPointFlat = _waveStartPoint;
        startPointFlat.y = 0f;

        Vector3 direction = (startPointFlat - heroPosFlat).normalized;
        if (Vector3.Distance(heroPosFlat, startPointFlat) < 0.3f)
            direction = _originalDirection;

        Vector3 waveCenter = startPointFlat + direction * (AreaInfo.CastLength / 2f);
        waveCenter.y = transform.position.y + 0.5f;

        CmdSpawnWaveEffect(waveCenter, Quaternion.LookRotation(direction));

        var colliders = Physics.OverlapBox(
            waveCenter,
            new Vector3(AreaInfo.CastWidth / 2f, 2f, AreaInfo.CastLength / 2f),
            Quaternion.LookRotation(direction),
            Targeting.Layer
        );

        foreach (var collider in colliders)
        {
            if (!collider.TryGetComponent(out Character enemy) || enemy.IsDead)
                continue;

            Vector3 enemyPosFlat = enemy.transform.position;
            enemyPosFlat.y = 0f;

            Vector3 toEnemy = enemyPosFlat - startPointFlat;

            float distanceAlongWave = Vector3.Dot(toEnemy, direction);
            if (distanceAlongWave < 0 || distanceAlongWave > AreaInfo.CastLength)
                continue;

            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);
            float distanceFromCenter = Mathf.Abs(Vector3.Dot(toEnemy, perpendicular));

            if (distanceFromCenter > AreaInfo.CastWidth / 2f)
                continue;

            float casterRadius = ((CapsuleCollider)_hero.Collider).radius;
            float enemyRadius = ((CapsuleCollider)enemy.Collider).radius;
            float centerDist = Vector3.Distance(startPointFlat, enemyPosFlat);
            float edgeDist = Mathf.Max(centerDist - (casterRadius + enemyRadius), 0f);

            float damageMul = Mathf.Clamp01(1f - edgeDist / AreaInfo.CastLength);

            Damage scaledDamage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage) * damageMul,
                Type = Info.DamageType,
                PhysicAttackType = Info.AttackRangeType,
            };

            CmdApplyDamage(scaledDamage, enemy.gameObject);

            Vector3 pushPoint = enemy.transform.position + direction * _pushRange;
            pushPoint.y = enemy.transform.position.y;

            CmdMoveTarget(enemy.gameObject, pushPoint, _pushDuration);
        }

        yield return new WaitForSeconds(0.6f);

        _skillRender.ResetCursor();
    }
    
    [Command]
    private void CmdSpawnWaveEffect(Vector3 position, Quaternion rotation)
    {
        if (_waveParticlePrefab == null) return;

        var fx = Instantiate(_waveParticlePrefab, position, rotation);
        NetworkServer.Spawn(fx);
        
        StartCoroutine(DestroyAfterDelay(fx, 0.6f));
    }
    
    private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
            NetworkServer.Destroy(obj);
    }

    protected override void ClearData()
    {
        _originalDirection = Vector3.forward;
        _waveStartPoint = Vector3.zero;

        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        StopCustomDraw();

        if (_lineVisual != null)
            _lineVisual.gameObject.SetActive(false);
    }
    
    private void OnSkillCancel()
    {
        StopAllCoroutines();
        Hero.Move?.StopLookAt();
        ClearData();
    }

    public override void StopCustomDraw()
    {
        SkillRender.StopDrawRadius();
        Renderer?.HideSmartIndicator();

        if (_lineVisual != null)
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

        while (IsPreparing)
        {
            Vector3 mousePoint = GetGroundMousePoint();

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

            Vector3 centerPosition = transform.position + direction * Mathf.Min(distance, AreaInfo.Radius);
            pivotTransform.position = centerPosition - direction * (AreaInfo.CastLength / 2f);
            pivotTransform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            _lineVisual.SetSize(AreaInfo.CastWidth, AreaInfo.CastLength, damage);

            yield return null;
        }
        
        _lineVisual.gameObject.SetActive(false);
        SkillRender.StopDrawRadius();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new();

        while (!Input.GetMouseButtonDown(0))
            yield return null;

        Vector3 clickPoint = Targeting.GetMousePoint();
        Vector3 directionToClick = clickPoint - transform.position;
        directionToClick.y = 0;

        if (directionToClick.magnitude > AreaInfo.Radius)
            directionToClick = directionToClick.normalized * AreaInfo.Radius;

        _originalDirection = directionToClick.normalized;

        Vector3 centerPoint = transform.position + directionToClick;
        _waveStartPoint = centerPoint - _originalDirection * (AreaInfo.CastLength / 2f);

        Vector3 waveEndPoint = centerPoint + _originalDirection * (AreaInfo.CastLength / 2f);

        targetInfo.Points.Add(_waveStartPoint);
        targetInfo.Points.Add(waveEndPoint);

        targetInfo.Points.Add(_waveStartPoint);
        targetInfo.Points.Add(waveEndPoint);
   
        StopCustomDraw();
        StopDynamicRender();
        callbackDataSaved(targetInfo);
    }

    
    private Vector3 GetGroundMousePoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Ground")))
            return hit.point;

        return Vector3.zero;
    }


    [Command]
    private void CmdMoveTarget(GameObject target, Vector3 point, float time)
    {
        if (target == null) return;
        var enemyMove = target.GetComponent<MoveComponent>();
        if (enemyMove == null) return;

        enemyMove.RpcDoPush(point, time);
    }
}
