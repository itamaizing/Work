using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class Shot : Skill
{
    [SerializeField] private ArrowProjectile _projectile;
    [SerializeField] private HeroComponent _playerLinks;
    [SerializeField] private Ghost _ghostSkill;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private TerrifyingElfAura _terrifyingElfAura;
    [SerializeField] private float _minDamage = 6f;
    [SerializeField] private float _maxDamage = 10f;
    [SerializeField] private float _arrowYOffsetUp = 1.5f;
    [SerializeField] private float _arrowYOffsetDown = 0.5f;
    [SerializeField] private LayerMask _groundLayerMask;

    private const string _startAnimTrigger = "ShotCastDelayTrigger";

    #region Constants

    private const float HealthThresholdPercent = 0.8f;
    private const float ExtraDamageMultiplier = 0.3f;
    private const float CritChance = 0.20f;
    private const float CritMultiplier = 3.2f;
    private const float RayCastDistance = 1000f;

    private const int GhostShotsForCooldownReduction = 3;
    private const int GhostCooldownReductionValue = 1;

    private const float RandomRangeInclusiveOffset = 1f;
    private const float RadiusTargetCheck = 0.3f;

    #endregion

    private AudioSource _audioSource;
    private int _consecutiveShots;

    private Vector3 _targetPoint = Vector3.positiveInfinity;

    private bool _isHealthAboveThreshold;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash(_startAnimTrigger);

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

        if (Targeting.GetTarget() != null && Targeting.GetTarget()?.Character is Character targetCurrent)
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
        _hero.Move.SetCanMove(false);
    }

    protected override IEnumerator CastJob()
    {
        var targetData = Targeting.GetTarget();

        if (targetData == null) yield break;

        ShotAnimationMove();
        ProcessGhostCooldownReduction();
        HandleThirdShotRowOnCast();

        if (targetData.Type ==  TargetType.Object) CmdCreateProjectileAtTarget(targetData.Object, Damage);
        else CmdCreateProjectileAtPosition(targetData.Poisition, Damage);
        yield return null;
    }

    private void ProcessGhostCooldownReduction()
    {
        if (!_ghostSkill || !_ghostSkill.CooldownGhostShotActive) return;

        _consecutiveShots++;
        if (_consecutiveShots >= GhostShotsForCooldownReduction)
        {
            //_ghostSkill.ReductionCooldownCharges(GhostCooldownReductionValue);
            _ghostSkill.Charges.ModifyDuration(-GhostCooldownReductionValue, tickAll: true);
            _consecutiveShots = 0;
        }
    }

    private void HandleSkillCanceled()
    {
        if (_hero?.Move != null)
        {
            Hero.Move.SetCanMove(true);
            Targeting.ClearTarget();
            Targeting.ClearTempTarget();
            _targetPoint = Vector3.positiveInfinity;
            Hero.Move.StopLookAt();
            AnimCastEnded();
        }
    }

    private void HandleThirdShotRowOnCast()
    {
        if (_terrifyingElfAura == null) return;
        if (!_terrifyingElfAura.IsThirdShotRowActive) return;

        var targetData = Targeting.GetTarget();

        if (targetData == null || targetData.Character == null) return;

        _terrifyingElfAura.ProcessShot(targetData.Character);
    }

    [Command]
    public void CmdCreateProjectileAtTarget(GameObject targetObject, float damage)
    {
        if (targetObject == null) return;

        Transform target = targetObject.transform;

        Vector3 direction = (target.transform.position - transform.position).normalized;

        if (direction == Vector3.zero) return;

        ArrowProjectile proj = Instantiate(_projectile, transform.position + Vector3.up * _arrowYOffsetUp, Quaternion.LookRotation(direction));
        proj.Init(_playerLinks, 0, false, this, damage, _terrifyingElfAura.IsElvenSkillPhysDamageHealthChance);
        //SceneManager.MoveGameObjectToScene(proj.gameObject, _hero.NetworkSettings.MyRoom);
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

        ArrowProjectile proj = Instantiate(_projectile, transform.position + Vector3.up * _arrowYOffsetDown, Quaternion.LookRotation(direction));
        proj.Init(_playerLinks, 0, false, this, damage, _terrifyingElfAura.IsElvenSkillPhysDamageHealthChance);
        //SceneManager.MoveGameObjectToScene(proj.gameObject, _hero.NetworkSettings.MyRoom);
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
        if (proj != null) proj.Init(_playerLinks, 0, false, this, damage, _terrifyingElfAura.IsElvenSkillPhysDamageHealthChance);
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && _audioClip != null) _audioSource.PlayOneShot(_audioClip);
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        AnimCastEnded();
        _consecutiveShots = 0;
    }
}
