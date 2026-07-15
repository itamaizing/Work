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

    private bool _isReflected;
    private bool _isElvenSkillCrit;

    #region Constants
    private const float InAstralDamageMultiplier = 1.5f;
    private const int ResistMagicDamageMaxValue = 100;
    #endregion

    private float _magDamage;
    private float _damage;
    
    private float _maxTravelDistance = -1f;

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

        if (target == _dad.gameObject) return false;

        UserNetworkSettings ownerSettings = null;
        if (_dad.TryGetComponent(out UserNetworkSettings foundOwnerSettings))
        {
            ownerSettings = foundOwnerSettings;
        }
        else if (_dad.NetworkSettings != null)
        {
            ownerSettings = _dad.NetworkSettings;
        }

        int ownerTeam = (ownerSettings != null) ? ownerSettings.TeamIndex : 0;

        if (target.TryGetComponent<Object>(out var obj))
        {
            if (ownerTeam != 0)
            {
                return ownerTeam != obj.IndexTeam;
            }

            if (obj.IndexTeam != 0)
            {
                return true;
            }
        }

        UserNetworkSettings targetSettings = null;
        if (target.TryGetComponent(out UserNetworkSettings foundTargetSettings))
        {
            targetSettings = foundTargetSettings;
        }
        else if (target.TryGetComponent<Character>(out var targetChar) && targetChar.NetworkSettings != null)
        {
            targetSettings = targetChar.NetworkSettings;
        }

        if (ownerSettings != null && targetSettings != null)
        {
            bool isOwnerAssigned = IsTeamAssigned(ownerSettings);
            bool isTargetAssigned = IsTeamAssigned(targetSettings);

            if (isOwnerAssigned && isTargetAssigned)
            {
                return ownerSettings.TeamIndex != targetSettings.TeamIndex;
            }

            if (isOwnerAssigned && !isTargetAssigned)
            {
                return true;
            }
        }

        return IsEnemyByLayer(target);
    }

    private bool IsTeamAssigned(UserNetworkSettings settings)
    {
        return settings.TeamIndex != 0;
    }

    private bool IsEnemyByLayer(GameObject target)
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        return target.layer == enemyLayer;
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
            float limit = _maxTravelDistance > 0f ? _maxTravelDistance : _skill.AreaInfo.CastLength;
            if (distanceTravelled > limit)
            {
                Destroy(gameObject);
            }
        }
    }

    private void Reflect(Character reflector)
    {
        _isReflected = true;

        CancelInvoke();
        Destroy(gameObject, _lifeTime);

        Character oldOwner = _dad;
        _dad = reflector;

        if (oldOwner == null) return;

        _isFollowingTarget = false;
        StopAllCoroutines();

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        Vector3 targetPos = oldOwner.transform.position + Vector3.up * _arrowYOffset;
        Vector3 dir = (targetPos - transform.position).normalized;

        if (_rb != null) _rb.linearVelocity = dir * _speed;

        _startPosition = transform.position;
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

    public void Init(HeroComponent dad, float energy, bool lastHit, Skill skill, float damage, bool ElvenSkillCrit, float maxTravelDistance)
    {
        base.Init(dad, energy, lastHit, skill);
        _damage = damage;
        _magDamage = energy;
        _isElvenSkillCrit = ElvenSkillCrit;
        _maxTravelDistance = maxTravelDistance;
    }

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (_dad == null) return;

        if (other.gameObject == _dad.gameObject) return;

        if (!other.TryGetComponent<IDamageable>(out _)) return;

        if (!IsEnemy(other.gameObject)) return;

        other.TryGetComponent<Character>(out var target);

        if (target != null && target.CharacterState.CheckForState(States.ReflectiveScales) && _arrowDark)
        {
            if (_isReflected) return;

            target.CharacterState.RemoveState(States.ReflectiveScales);
            Reflect(target);
            return;
        }

        if (_arrowDark && other.GetComponentInParent<ReconnaissanceFireAura>() != null)
        {
            Destroy(gameObject);
            return;
        }

        if (other.TryGetComponent<ObjectHealth>(out ObjectHealth objectHealth) &&
            objectHealth.ResistMagicDamage >= ResistMagicDamageMaxValue && _arrowDark)
        {
            return;
        }

        ApplyEnemy(other);
        Destroy(gameObject);
    }

    #region ApplyEnemy
    private void ApplyEnemy(Collider collider)
    {
        bool inAstral = _dad != null && _dad.CharacterState.CheckForState(States.Astral);
        Character character = collider.GetComponent<Character>();

        if (_arrowDark)
        {
            if (!inAstral)
            {
                if (character != null)
                {
                    float modifiedDamage = ApplyElvenCritModifier(_damage, character);
                    ApplyDamage(modifiedDamage, _damageTypePhysics, collider.gameObject);
                }

                else ApplyDamage(_damage, _damageTypePhysics, collider.gameObject);
                if (TryApplyDamage(_damageTypePhysics, _skill.Info.AttackRangeType, collider.gameObject)) return;
            }

            float totalMagDamage = _magDamage;
            if (inAstral) totalMagDamage *= InAstralDamageMultiplier;

            Debug.Log($"totalMagDamage: {totalMagDamage}");

            ApplyDamage(totalMagDamage, _skill.Info.DamageType, collider.gameObject);

            if (character != null) character.CharacterState.AddState(States.InnerDarkness, _duration, 0, _skill.Hero.gameObject, _skill.name);
        }

        else
        {
            if (character != null)
            {
                float modifiedDamage = ApplyElvenCritModifier(_damage, character);
                ApplyDamage(modifiedDamage, _damageTypePhysics, collider.gameObject);
            }

            else ApplyDamage(_damage, _damageTypePhysics, collider.gameObject);
        }
    }
    #endregion

    private void ApplyDamage(float value, DamageType type, GameObject target)
    {
        var damage = new Damage { Value = value, Type = type };
        _skill.ApplyDamage(damage, target);
    }

    private float ApplyElvenCritModifier(float damage, Character target)
    {
        if (!_isElvenSkillCrit) return damage;
        if (_dad == null || target == null) return damage;
        if (!_dad.CharacterState.CheckForState(States.ElvenSkill)) return damage;

        float hpPercent = target.Health.CurrentValue / target.Health.MaxValue;

        if (hpPercent <= 0.7f) return damage;

        damage *= 1.3f;

        if (UnityEngine.Random.Range(0f, 100f) <= 30f)
        {
            float critMultiplier = UnityEngine.Random.Range(2.4f, 3.2f);
            damage *= critMultiplier;
        }

        return damage;
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
            if (_rb != null) _rb.linearVelocity = dir * _speed;

            yield return null;
        }
    }

    [ClientRpc]
    private void RpcArrowTrue()
    {
        _arrow.SetActive(true);
    }
}
