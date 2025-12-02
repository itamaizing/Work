using Mirror;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShotDarkness : Skill
{
    [SerializeField] private ArrowProjectile projectile;
    [SerializeField] private HeroComponent playerLinks;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Ghost ghostSkill;
    [SerializeField] private MultiMagicSpell multiMagicSpell;
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private float minDamage;
    [SerializeField] private float maxDamage;

    private const string _startAnimTrigger = "ShotDarkCastDelayTrigger";

    private AudioSource _audioSource;
    private int _consecutiveShots;
    private float _magicDamage;

    private IDamageable _target;
    private Vector3 _targetPoint = Vector3.positiveInfinity;

    private bool _isHealthAboveThreshold;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash(_startAnimTrigger);
    protected override bool IsCanCast { get => CheckCanCast(); }

    private bool CheckCanCast()
    {
        if (_target == null) return Vector3.Distance(_targetPoint, transform.position) <= CastLength;
        return Vector3.Distance(_targetPoint, transform.position) <= CastLength || Vector3.Distance(_target.transform.position, transform.position) <= CastLength;
    }

    private void OnDestroy() => OnSkillCanceled -= HandleSkillCanceled;
    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;
    private void Start() => _audioSource = GetComponent<AudioSource>();

    private void ShotDarknessAnimationMove()
    {
        if (_hero == null || _hero.Move == null) return;

        _isHealthAboveThreshold = false;

        if (_target != null && _target is Character targetCurrent)
        {
            var health = targetCurrent.Health;
            _isHealthAboveThreshold = health.CurrentValue >= health.MaxValue * 0.8f;
        }

        if (!terrifyingElfAura) Damage = UnityEngine.Random.Range(minDamage, maxDamage + 1);
        else
        {
            if (!_isHealthAboveThreshold) Damage = UnityEngine.Random.Range(minDamage, maxDamage + 1);

            else
            {
                var elvenSkill = playerLinks.CharacterState.GetState(States.ElvenSkill) as ElvenSkill;

                if (elvenSkill == null) Damage = UnityEngine.Random.Range(minDamage, maxDamage + 1);

                else
                {
                    float baseDamage = UnityEngine.Random.Range(minDamage, maxDamage + 1);
                    float extraDamage = UnityEngine.Random.Range(minDamage, maxDamage + 1) * 0.3f;
                    float total = baseDamage + extraDamage;

                    bool isCrit = UnityEngine.Random.value < 0.20f;
                    if (isCrit) total *= 3.2f;

                    Damage = total;
                }
            }
        }
    }
    public void ShotDarkCastStart()
    {
        AnimStartCastCoroutine();
    }

    public void ShotDarkCastEnd()
    {
        AnimCastEnded();
    }
    public void ShotDarkPreparation()
    {
        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;
    }
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = targetInfo.Targets[0] as IDamageable;
        if (_target is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
        _targetPoint = targetInfo.Points[0];
    }
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        ITargetable target = null;
        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            if (GetMouseButton)
            {
                if (GetRaycastTarget() is ITargetable targetable) target = targetable;
                targetPoint = GetMousePoint();
            }
            yield return null;
        }
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(target);
        targetInfo.Points.Add(targetPoint);
        callbackDataSaved(targetInfo);
    }


    protected override IEnumerator CastJob()
    {
        if (_target == null && _targetPoint == Vector3.positiveInfinity) yield return null;
        if (_target != null && !IsTargetInRange()) yield return null;

        _magicDamage = CalculateAndSpendBonusMagicDamage();
        ShotDarknessAnimationMove();
        ProcessGhostCooldownReduction();

        if (_target != null) CmdCreateProjectileAtTarget(_target.transform, Damage, _magicDamage);
        else CmdCreateProjectileAtPosition(new Vector3(_targetPoint.x, _targetPoint.y, _targetPoint.z), Damage, _magicDamage);

        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        if (multiMagic != null)
        {
            foreach (var character in multiMagic.PopPendingTargets())
            {
                TryPayCost();
                CmdUseMana(_magicDamage);
                CmdCreateProjectileAtPosition(character.transform.position, Damage, _magicDamage);
            }

            float reduce = multiMagicSpell.RemainingCooldownTime * 0.1f;
            multiMagicSpell.DecreaseSetCooldown(reduce);
        }

        else CmdUseMana(_magicDamage);
    }

    private void ProcessGhostCooldownReduction()
    {
        if (!ghostSkill || !ghostSkill.CooldownGhostShotActive) return;

        _consecutiveShots++;
        if (_consecutiveShots >= 3)
        {
            ghostSkill.ReductionCooldownCharges(1);
            _consecutiveShots = 0;
        }
    }

    private void HandleSkillCanceled()
    {
        if (_hero?.Move != null)
        {
            Hero.Move.CanMove = true;
            _target = null;
            _targetPoint = Vector3.positiveInfinity;
            Hero.Move.StopLookAt();
        }
    }

    private bool IsTargetInRange() { return _target != null && Vector3.Distance(transform.position, _target.transform.position) <= CastLength; }
    private void UseMana(float amount)
    {
        float mana = amount;
        foreach (var resource in playerLinks.Resources.Where(resource => resource.Type == ResourceType.Mana))
        {
            if (mana <= 0) break;
            float spend = Math.Min(resource.CurrentValue, mana);
            resource.CurrentValue -= spend;
            mana -= spend;
        }
    }
    private float CalculateAndSpendBonusMagicDamage(float maxBonusMana = 6f)
    {
        float availableMana = playerLinks.Resources
            .Where(r => r.Type == ResourceType.Mana)
            .Sum(r => r.CurrentValue);

        float bonusManaToUse = Mathf.Min(availableMana, maxBonusMana);

        float manaSpent = 0f;
        float manaToSpend = bonusManaToUse;

        foreach (var resource in playerLinks.Resources.Where(r => r.Type == ResourceType.Mana))
        {
            if (manaToSpend <= 0) break;

            float spend = Mathf.Min(resource.CurrentValue, manaToSpend);
            manaSpent += spend;
        }

        _magicDamage = manaSpent;

        return manaSpent;
    }

    [Command]
    protected void CmdCreateProjectileAtTarget(Transform target, float damage, float magDamage)
    {
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 direction = (target.transform.position - spawnPosition).normalized;

        if (direction == Vector3.zero) return;

        ArrowProjectile proj = Instantiate(projectile, spawnPosition, Quaternion.LookRotation(direction));
        proj.Init(playerLinks, magDamage, false, this, damage);
        SceneManager.MoveGameObjectToScene(proj.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(proj.gameObject);
        proj.StartFly(target);
        RpcInit(proj.gameObject, magDamage, damage);
        RpcPlayShotSound();
    }

    [Command]
    public void CmdCreateProjectileAtPosition(Vector3 position, float damage, float magDamage)
    {
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;

        Vector3 flatTargetPoint = new Vector3(position.x, spawnPosition.y, position.z);
        Vector3 direction = (flatTargetPoint - spawnPosition).normalized;

        if (direction == Vector3.zero) return;

        ArrowProjectile proj = Instantiate(projectile, spawnPosition, Quaternion.LookRotation(direction));
        proj.Init(playerLinks, magDamage, false, this, damage);
        SceneManager.MoveGameObjectToScene(proj.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(proj.gameObject);
        proj.StartFly(direction);
        RpcInit(proj.gameObject, magDamage, damage);
        RpcPlayShotSound();
    }
    [Command] private void CmdUseMana(float amount) => UseMana(amount);

    [ClientRpc]
    protected void RpcInit(GameObject gameObject, float magicDamage, float damage)
    {
        if (gameObject == null) return;

        ArrowProjectile proj = gameObject.GetComponent<ArrowProjectile>();
        if (proj != null) proj.Init(playerLinks, magicDamage, false, this, damage);
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && audioClip != null)
            _audioSource.PlayOneShot(audioClip);
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        _target = null;
        _consecutiveShots = 0;
    }
}
