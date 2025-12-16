using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Shot : Skill
{
    [SerializeField] private ArrowProjectile _projectile;
    [SerializeField] private HeroComponent _playerLinks;
    [SerializeField] private Ghost _ghostSkill;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private TerrifyingElfAura _terrifyingElfAura;
    [SerializeField] private float _minDamage = 6f;
    [SerializeField] private float _maxDamage = 10f;
    [SerializeField] private float _arrowYOffset = 1.5f;

    private const string _startAnimTrigger = "ShotCastDelayTrigger";

    #region Constants

    private const float HealthThresholdPercent = 0.8f;
    private const float ExtraDamageMultiplier = 0.3f;
    private const float CritChance = 0.20f;
    private const float CritMultiplier = 3.2f;

    private const int GhostShotsForCooldownReduction = 3;
    private const int GhostCooldownReductionValue = 1;

    private const float RandomRangeInclusiveOffset = 1f;

    #endregion

    private AudioSource _audioSource;
    private int _consecutiveShots;

    private Vector3 _targetPoint = Vector3.positiveInfinity;

    private bool _isHealthAboveThreshold;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash(_startAnimTrigger);
    protected override bool IsCanCast { get => CheckCanCast(); }
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == TargetsLayers;

    private bool CheckCanCast()
    {
        if (GetTarget() != null)
            return Vector3.Distance(GetTarget().Transform.position, transform.position) <= CastLength;

        if (_targetPoint != Vector3.positiveInfinity)
            return Vector3.Distance(_targetPoint, transform.position) <= CastLength;

        return false;
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void ShotAnimationMove()
    {
        if (_hero == null || _hero.Move == null) return;

        _isHealthAboveThreshold = false;

        if (GetTarget() != null && GetTarget() is Character targetCurrent)
        {
            var health = targetCurrent.Health;
            _isHealthAboveThreshold = health.CurrentValue >= health.MaxValue * HealthThresholdPercent;
        }

        if (!_terrifyingElfAura) Damage = UnityEngine.Random.Range(_minDamage, _maxDamage + RandomRangeInclusiveOffset);
        else
        {
            if (!_isHealthAboveThreshold) Damage = UnityEngine.Random.Range(_minDamage, _maxDamage + RandomRangeInclusiveOffset);

            else
            {
                var elvenSkill = _playerLinks.CharacterState.GetState(States.ElvenSkill) as ElvenSkill;

                if (elvenSkill == null) Damage = UnityEngine.Random.Range(_minDamage, _maxDamage + RandomRangeInclusiveOffset);

                else
                {
                    float baseDamage = UnityEngine.Random.Range(_minDamage, _maxDamage + RandomRangeInclusiveOffset);
                    float extraDamage = UnityEngine.Random.Range(_minDamage, _maxDamage + RandomRangeInclusiveOffset) * ExtraDamageMultiplier;
                    float total = baseDamage + extraDamage;

                    bool isCrit = UnityEngine.Random.value < CritChance;
                    if (isCrit) total *= CritMultiplier;

                    Damage = total;
                }
            }
        }
    }

    public void ShotCastStart()
    {
        AnimStartCastCoroutine();
    }

    public void ShotCastEnd()
    {
       AnimCastEnded();
    }

    public void ShotPreparation()
    {
        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) SetTarget(targetInfo.GetTargets()[0]);
        _targetPoint = targetInfo.Points[0];
    }
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            if (GetMouseButton)
            {
                FindTarget();
                targetPoint = GetMousePoint();

                if (GetTempTarget() != null && GetTempTarget() is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) ClearTempTarget();

                    else
                    {
                        if (GetTempTarget() is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }

        SetTarget(GetTempTarget());

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(GetTarget());
        targetInfo.Points.Add(targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (GetTarget() == null && _targetPoint == Vector3.positiveInfinity) yield return null;
        if (GetTarget() != null && !IsTargetInRange()) yield return null;

        ShotAnimationMove();
        ProcessGhostCooldownReduction();

        if (GetTarget() != null && GetTarget() is IDamageable damageable) CmdCreateProjectileAtTarget(damageable.gameObject, Damage);
        else CmdCreateProjectileAtPosition(_targetPoint, Damage);

        yield return null;
    }
    private bool IsTargetInRange() { return GetTarget() != null && Vector3.Distance(transform.position, GetTarget().Transform.position) <= CastLength; }
    private void ProcessGhostCooldownReduction()
    {
        if (!_ghostSkill || !_ghostSkill.CooldownGhostShotActive) return;

        _consecutiveShots++;
        if (_consecutiveShots >= GhostShotsForCooldownReduction)
        {
            _ghostSkill.ReductionCooldownCharges(GhostCooldownReductionValue);
            _consecutiveShots = 0;
        }
    }

    private void HandleSkillCanceled()
    {
        if (_hero?.Move != null)
        {
            Hero.Move.CanMove = true;
            ClearTarget();
            _targetPoint = Vector3.positiveInfinity;
            Hero.Move.StopLookAt();
        }
    }

    [Command]
    public void CmdCreateProjectileAtTarget(GameObject targetObject, float damage)
    {
        if (targetObject == null) return;

        Transform target = targetObject.transform;

        Vector3 direction = (target.transform.position - transform.position + Vector3.up * _arrowYOffset).normalized;

        if (direction == Vector3.zero) return;

        ArrowProjectile proj = Instantiate(_projectile, transform.position + Vector3.up * _arrowYOffset, Quaternion.LookRotation(direction));
        proj.Init(_playerLinks, 0, false, this, damage);
        SceneManager.MoveGameObjectToScene(proj.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(proj.gameObject);
        proj.StartFly(target);
        RpcInit(proj.gameObject, damage);
        RpcPlayShotSound();
    }

    [Command]
    public void CmdCreateProjectileAtPosition(Vector3 position, float damage)
    {

        Vector3 flatTargetPoint = new Vector3(position.x, position.y, position.z);
        Vector3 direction = (flatTargetPoint - transform.position).normalized;

        if (direction == Vector3.zero) return;

        ArrowProjectile proj = Instantiate(_projectile, transform.position + Vector3.up * _arrowYOffset, Quaternion.LookRotation(direction));
        proj.Init(_playerLinks, 0, false, this, damage);
        SceneManager.MoveGameObjectToScene(proj.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(proj.gameObject);
        proj.StartFly(direction);
        RpcInit(proj.gameObject, damage);
        RpcPlayShotSound();
    }

    [ClientRpc]
    protected void RpcInit(GameObject gameObject, float damage)
    {
        if (gameObject == null) return;

        ArrowProjectile proj = gameObject.GetComponent<ArrowProjectile>();
        if (proj != null) proj.Init(_playerLinks, 0, false, this, damage);
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && _audioClip != null) _audioSource.PlayOneShot(_audioClip);
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        ClearTarget();
        _consecutiveShots = 0;
    }
}

