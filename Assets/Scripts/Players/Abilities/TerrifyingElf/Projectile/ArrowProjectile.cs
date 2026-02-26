using Mirror;
using System.Collections;
using System.Linq;
using UnityEngine;

public class ArrowProjectile : Projectiles
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private float _arrowYOffset = 1.5f;
    [SerializeField] private bool _arrowDark;
    [SerializeField] private float _duration;
    [SerializeField] private DamageType _damageTypePhysics;
    [SerializeField] private GameObject _arrow;
    [SerializeField] private SphereCollider _sphereCollider;

    private Transform _followTarget;
    private Vector3 _startPosition;
    private bool _isFollowingTarget = false;

    #region Constants
    private const float InAstralDamageMultiplier = 1.5f;
    private const int ResistMagicDamageMaxValue = 100;
    #endregion

    private float _magDamage;
    private float _damage;

    public bool ArrowDark { get => _arrowDark; set => _arrowDark = value; }

    private void OnEnable()
    {
        _arrow.SetActive(false);
        _sphereCollider.enabled = false;
        Destroy(gameObject, _lifeTime);
    }

    private bool IsEnemy(GameObject target)
    {
        if (_dad == null) return IsEnemyByLayer(target);
        if (!_dad.TryGetComponent(out UserNetworkSettings ownerSettings) || !target.TryGetComponent(out UserNetworkSettings targetSettings)) return IsEnemyByLayer(target);
        if (!IsTeamAssigned(ownerSettings) || !IsTeamAssigned(targetSettings)) return IsEnemyByLayer(target);

        return ownerSettings.TeamIndex != targetSettings.TeamIndex;
    }

    private bool IsTeamAssigned(UserNetworkSettings settings)
    {
        return settings.TeamIndex != 0;
    }

    private bool IsEnemyByLayer(GameObject target)
    {
        return ((1 << target.layer) & _skill.Targeting.Layer.value) != 0;
    }

    private void Update()
    {
        ArrowStart();
    }

    private void ArrowStart()
    {
        if (_startPosition != Vector3.zero)
        {
            float distanceTravelled = Vector3.Distance(_startPosition, transform.position);
            if (distanceTravelled > _skill.AreaInfo.CastLength)
            {
                Destroy(gameObject);
            }
        }
    }

    public void StartFly(Vector3 direction)
    {
        if (direction == Vector3.zero || float.IsNaN(direction.x) || float.IsNaN(direction.y) || float.IsNaN(direction.z)) return;
        if (_rb != null) _rb.linearVelocity = direction * _speed;
        _startPosition = transform.position;
        _arrow.SetActive(true);
        _sphereCollider.enabled = true;
        RpcArrowTrue();
    }
    public void StartFly(Transform target)
    {
        _startPosition = transform.position;
        _followTarget = target;
        _isFollowingTarget = true;
        _sphereCollider.enabled = true;
        RpcArrowTrue();
        StartCoroutine(FollowTargetCoroutine());
    }

    public void Init(HeroComponent dad, float energy, bool lastHit, Skill skill, float damage)
    {
        base.Init(dad, energy, lastHit, skill);
        _damage = damage;
        _magDamage = energy;
    }

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == _dad?.gameObject) return;
        if (!other.TryGetComponent<IDamageable>(out _)) return;

        if (_arrowDark && other.GetComponentInParent<ReconnaissanceFireAura>() != null)
        {
            Destroy(gameObject);
            return;
        }

        if (!IsEnemy(other.gameObject)) return;

        if (other.TryGetComponent<ObjectHealth>(out ObjectHealth objectHealth) &&
            objectHealth.ResistMagicDamage >= ResistMagicDamageMaxValue && _arrowDark)
            return;

        ApplyEnemy(other);
        Destroy(gameObject);
    }

    #region ApplyEnemy
    private void ApplyEnemy(Collider collider)
    {
        bool inAstral = _dad != null && _dad.CharacterState.CheckForState(States.Astral);

        if (_arrowDark)
        {
            if (!inAstral)
            {
                ApplyDamage(_damage, _damageTypePhysics, collider.gameObject);
                if (TryApplyDamage(_damageTypePhysics, _skill.Info.AttackRangeType, collider.gameObject)) return;
            }

            float totalMagDamage = _magDamage;
            if (inAstral) totalMagDamage *= InAstralDamageMultiplier;

            Debug.Log($"totalMagDamage: {totalMagDamage}");

            ApplyDamage(totalMagDamage, _skill.Info.DamageType, collider.gameObject);

            if (collider.TryGetComponent<Character>(out Character character)) character.CharacterState.AddState(States.InnerDarkness, _duration, 0, _skill.Hero.gameObject, _skill.name);
        }

        else ApplyDamage(_damage, _damageTypePhysics, collider.gameObject);
    }
    #endregion

    private void ApplyDamage(float value, DamageType type, GameObject target)
    {
        var damage = new Damage { Value = value, Type = type };
        _skill.ApplyDamage(damage, target);
    }

    private bool TryApplyDamage(DamageType damageType, AttackRangeType attackRangeType, GameObject target)
    {
        if (target.TryGetComponent<Health>(out Health health)) return health.TryEvade(damageType, attackRangeType);

        return false;
    }

    private IEnumerator FollowTargetCoroutine()
    {
        while (_isFollowingTarget && _followTarget != null)
        {
            Vector3 targetPos = _followTarget.position + Vector3.up * _arrowYOffset;
            Vector3 dir = (targetPos - transform.position).normalized;
            if (_rb != null)
                _rb.linearVelocity = dir * _speed;

            yield return null;
        }
    }

    [ClientRpc]
    private void RpcArrowTrue()
    {
        _arrow.SetActive(true);
    }
}