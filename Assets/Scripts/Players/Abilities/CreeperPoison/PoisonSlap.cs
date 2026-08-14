using Mirror;
using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class PoisonSlap : Skill
{
    #region Variables

    private bool _isCanDamageDeal = false;

    [SerializeField] private Character _player;
    [SerializeField] private ColdBlood _coldBlood;

    [Header("Abilities")]
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private LightningMovement _lightningMovement;
    [SerializeField] private SkillManager _skillManager;

    [Header("Talents")]
    [SerializeField] private PoisonDamagingCloudPrefab _poisonDamagingCloudPrefab;
    [SerializeField] private PoisonHealingCloudPrefab _poisonHealingCloudPrefab;

    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;

    private PoisonDamagingCloudPrefab _poisonDamagingCloud;
    private PoisonHealingCloudPrefab _poisonHealingCloud;

    private float _durationPoisonCloud = 6f;

    private int _poisonBoneStack;

    private float _creeperStrikeCastSpeedMultiplier = 1.5f;
    private float _lightningStrikesCastSpeedMultiplier = 2f;
    private float _baseDamage = 30f;
    private float _distancePush = 3.0f;
    private float _durationPush = 1.0f;

    private bool _isUsedPoisonBallCharger = true;

    private readonly AttributeModifier _castSpeedModifier = new AttributeModifier(1, ModifierType.Multiplier);

    #region Talent

    private bool _canSpawnPoisonCloud = false;
    private bool _isColdBloodCrit = false;

    public void ColdBloodStrike(bool value) => _isColdBloodCrit = value;

    public void SetPoisonCloudEnabled(bool value)
    {
        if (value == _canSpawnPoisonCloud) return;
        
        _canSpawnPoisonCloud = value;
    }

    #endregion

    private static readonly int poisonSlapTrigger = Animator.StringToHash("PoisonSlapCastAnimTrigger");

    protected override int AnimTriggerCast => poisonSlapTrigger;
    protected override int AnimTriggerCastDelay => 0;
    public int PoisonBoneStack { get => _poisonBoneStack; set => _poisonBoneStack = value; }
    public bool IsCanDamageDeal { get => _isCanDamageDeal; set => _isCanDamageDeal = value; }

    protected override bool IsCanCast => CheckCanCast();

    public event System.Action OnPoisonSlapEnd;

    #endregion

    #region PrepareAndStartJob

    private void OnDisable()
    {
        OnSkillCanceled -= ClearDataPoisonSlap;
    }

    private void OnEnable()
    {
        OnSkillCanceled += ClearDataPoisonSlap;
    }

    public void PoisonSlapPreparation()
    {
        // Блокируем движение ТОЛЬКО если способность реально исполняется/подготавливается
        if (!IsCasting && !IsPreparing) return;

        if (Hero != null && Hero.Move != null)
        {
            Hero.Move.StopMoveAndAnimationMove();
            Hero.Move.SetCanMove(false);
        }
    }

    public void AnimPoisonSlapCast()
    {
        AnimStartCastCoroutine();
    }

    public void AnimPoisonSlapCastEnded()
    {
        AnimCastEnded();
    }

    public void UsePoisonSlapOfLightningMovement()
    {
        DamageDealOfLightningMovement();
    }

    public void ClearDataPoisonSlap()
    {
        ClearData();
        Renderer?.HideSmartIndicator();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo != null && targetInfo.GetTargets().Count > 0) 
        {
            Targeting.SetTarget(targetInfo.GetTargets()[0]);
        }
        SwitchPayCost();
    }

    protected override void ClearData()
    {
        _castSpeedModifier.Value = 1;
        _isUsedPoisonBallCharger = true;

        if (Hero != null && Hero.Move != null)
        {
            Hero.Move.StopLookAt();
            Hero.Move.SetCanMove(true); // Гарантированная разблокировка движения
        }

        Targeting.ClearTempTarget();
        Targeting.ClearTarget();

        _castDeley = 0;
        base.ClearData();
    }

    protected override IEnumerator CastJob()
    {
        if (_isUsedPoisonBallCharger)
        {
            _poisonBall.PayCostPoisonBall();
        }

        DamageDeal(Targeting.GetTarget()?.Character);

        yield return null;
    }

    private void SwitchPayCost()
    {
        var last = _skillManager?.LastCastedSkill;
        var preview = _skillManager?.PreviewCastedSkill;

        bool isDoubleCreeper = last is CreeperStrike && preview is CreeperStrike;
        bool isLightning = last is LightningStrikes;

        if (isDoubleCreeper)
        {
            CastSpeedFromCreeperStrike();
            _isUsedPoisonBallCharger = false;
        }
        else if (isLightning)
        {
            CastSpeedFromLightningStrikes();
            _isUsedPoisonBallCharger = false;
        }
        else
        {
            _isUsedPoisonBallCharger = true;
        }
    }

    #endregion

    #region CalculationsDistances

    private bool CheckCanCast()
    {
        if (_poisonBall.Charges.RemainingCharges <= 0) return false;
        
        if (Targeting.GetTarget()?.Character == null)
            return false;

        return Vector3.Distance(_player.transform.position, Targeting.GetTarget().Character.transform.position) <= AreaInfo.Radius;
    }

    #endregion

    #region Coroutines

    private void CastSpeedFromCreeperStrike()
    {
        ApplyCastSpeedModifier(_creeperStrikeCastSpeedMultiplier);
    }

    private void CastSpeedFromLightningStrikes()
    {
        ApplyCastSpeedModifier(_lightningStrikesCastSpeedMultiplier);
    }

    private void ApplyCastSpeedModifier(float multiplier)
    {
        _castSpeedModifier.Value = multiplier;
        _castSpeedModifier.Source = this;

        var castSpeedAttribute = Attributes[SkillAttributeName.CastSpeed];

        if (!castSpeedAttribute.Modifiers.Contains(_castSpeedModifier))
            castSpeedAttribute.AddModifier(_castSpeedModifier);
    }

    #endregion

    #region DamageDealAndPushTargetMethods

    private void DamageDeal(Character target)
    {
        if (target != null)
        {
            bool isColdBloodCrit =
                _coldBlood != null && _isColdBloodCrit &&
                (_coldBlood.IsCanCrit || _coldBlood.IsCanCritLightningStrikes);

            if (isColdBloodCrit)
            {
                DealCriticalDamage(target, _baseDamage);

                _coldBlood.IsCanCrit = false;
                _coldBlood.IsCanCritLightningStrikes = false;
            }
            else
            {
                Damage damage = new Damage
                {
                    Value = _baseDamage,
                    Type = DamageType.Physical,
                    PhysicAttackType = AttackRangeType.MeleeAttack,
                };

                CmdApplyDamage(damage, target.gameObject);
            }

            PushTarget(target, _distancePush, _durationPush);
        }

        if (_canSpawnPoisonCloud) CmdApplyPoisonCloud(false, _durationPoisonCloud);

        OnPoisonSlapEnd?.Invoke();
    }

    public void DamageDealOfLightningMovement()
    {
        if (_isUsedPoisonBallCharger)
        {
            _poisonBall.PayCostPoisonBall();
        }

        if (Targeting.GetTarget()?.Character != null)
        {
            Damage damage = new Damage
            {
                Value = _baseDamage,
                Type = DamageType.Physical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
            };

            CmdApplyDamage(damage, Targeting.GetTarget()?.Character.gameObject);

            PushTarget(Targeting.GetTarget()?.Character, _distancePush, _durationPush);
        }
        UseRecharge();
    }

    private void UseRecharge()
    {
        float baseCooldownTime = _cooldownTime;

        _isCanDamageDeal = false;
        TryPayCost(true);

        _cooldownTime = baseCooldownTime;
    }

    private void PushTarget(Character target, float distancePush, float durationPush)
    {
        if (_lightningMovement.IsInMovement)
        {
            CmdPushEnemyInLightningMovement(target, distancePush, durationPush);
        }
        else
        {
            CmdPushEnemy(target, distancePush, durationPush);
        }
    }

    #endregion

    #region CommandMethods

    [Command]
    private void CmdPushEnemy(Character target, float distancePush, float durationPush)
    {
        if (target == null) return;

        Vector3 directionPush = _player.transform.forward;
        directionPush.y = 0f;
        directionPush.Normalize();

        Vector3 pushTarget = target.transform.position + directionPush * distancePush;
        pushTarget.y = target.transform.position.y;

        MoveComponent targetMove = target.GetComponent<MoveComponent>();
        if (targetMove != null)
        {
            StartCoroutine(HandlePushWithFly(targetMove, pushTarget, durationPush));
        }
    }

    [Command]
    private void CmdPushEnemyInLightningMovement(Character target, float distancePush, float durationPush)
    {
        if (target == null) return;

        Vector3 directionPush = (target.transform.position - _player.transform.position).normalized;
        Vector3 perpendicularDirection;

        if (directionPush.x < 0)
            perpendicularDirection = new Vector3(directionPush.y, -directionPush.x, 0).normalized;
        else
            perpendicularDirection = new Vector3(-directionPush.y, directionPush.x, 0).normalized;

        Vector3 pushTarget = target.transform.position + perpendicularDirection * distancePush;
        pushTarget.y = target.transform.position.y;

        MoveComponent targetMove = target.GetComponent<MoveComponent>();
        if (targetMove != null)
        {
            StartCoroutine(HandlePushWithFly(targetMove, pushTarget, durationPush));
        }
    }

    private IEnumerator HandlePushWithFly(MoveComponent targetMove, Vector3 finalPoint, float duration)
    {
        if (targetMove == null) yield break;

        targetMove.SetFlyState(true);

        targetMove.RpcDoPush(finalPoint, duration);

        var agent = targetMove.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled)
            agent.enabled = false;

        if (targetMove.Rigidbody != null)
        {
            targetMove.Rigidbody.DOKill();
            targetMove.Rigidbody.DOMove(finalPoint, duration).SetEase(Ease.Linear);
        }

        yield return new WaitForSeconds(duration);

        if (agent != null)
            agent.enabled = true;

        targetMove.SetFlyState(false);
    }
    [Command]
    private void CmdApplyPoisonCloud(bool isHealingCloud, float duration)
    {
        if (!isHealingCloud)
        {
            if (_poisonDamagingCloud == null && _poisonDamagingCloudPrefab.PoisonDamageCloud == null)
            {
                _poisonDamagingCloud = Instantiate(_poisonDamagingCloudPrefab, transform.position, Quaternion.identity);
                _poisonDamagingCloudPrefab.PoisonDamageCloud = _poisonDamagingCloud;

                _poisonDamagingCloud.InitializationProjectile(_player, duration, this, _creeperPoisonAura.IsFeelingPoisoning);
                _poisonDamagingCloud.AddStack();

                NetworkServer.Spawn(_poisonDamagingCloud.gameObject);
            }
            else
            {
                _poisonDamagingCloudPrefab.PoisonDamageCloud.AddStack();
            }
        }
        else
        {
            if (_poisonHealingCloud == null && _poisonHealingCloudPrefab.PoisonHealingCloud == null)
            {
                _player.CharacterState.AddState(States.HealingPoisonCloud, duration, 0, _player.gameObject, Name);

                _poisonHealingCloud = Instantiate(_poisonHealingCloudPrefab, transform.position, Quaternion.identity);
                _poisonHealingCloudPrefab.PoisonHealingCloud = _poisonHealingCloud;

                _poisonHealingCloud.InitializationProjectile(_player, duration, this, _creeperPoisonAura.IsFeelingPoisoning);
                _poisonHealingCloud.AddStack();

                NetworkServer.Spawn(_poisonHealingCloud.gameObject);
            }
            else
            {
                _player.CharacterState.AddState(States.HealingPoisonCloud, duration, 0, _player.gameObject, Name);
                _poisonHealingCloudPrefab.PoisonHealingCloud.AddStack();
            }
        }

        RpcApply(_poisonDamagingCloudPrefab.PoisonDamageCloud, _poisonHealingCloudPrefab.PoisonHealingCloud, duration, isHealingCloud);
    }

    [ClientRpc]
    private void RpcApply(PoisonDamagingCloudPrefab dmg, PoisonHealingCloudPrefab heal, float duration, bool isHealing)
    {
        if (dmg != null)
        {
            dmg.InitializationProjectile(_player, duration, this, _creeperPoisonAura.IsFeelingPoisoning);
            dmg.AddStack();
        }

        if (heal != null && isHealing)
        {
            heal.InitializationProjectile(_player, duration, this, _creeperPoisonAura.IsFeelingPoisoning);
            heal.AddStack();
        }
    }
    #endregion

    private void DealCriticalDamage(Character target, float baseDamage)
    {
        float multiplier = 2.5f;

        float finalDamage = baseDamage * multiplier;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(finalDamage),
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        CmdApplyDamage(damage, target.gameObject);
    }
}