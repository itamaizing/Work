using Mirror;
using System;
using System.Collections;
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
    //[SerializeField] private RestorationOfGlands _restorationOfGlands;
    //[SerializeField] private LightningFastPoisonSlap _lightningFastPoisonSlap;
    //[SerializeField] private LightweightSlap _lightweightSlap;
    //[SerializeField] private PoisonSlapTalent _poisonSlapTalent;

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
    private float _radiusTargetSearch = 0.5f;

    private readonly AttributeModifier _castSpeedModifier = new AttributeModifier(1, ModifierType.Multiplier);

    #region Talent

    private bool _canSpawnPoisonCloud = false;
    private bool _isColdBloodCrit = false;

    public void ColdBloodStrike(bool value) => _isColdBloodCrit = value;

    public void SetPoisonCloudEnabled(bool value)
    {
        if(value == _canSpawnPoisonCloud) return;
        
        _canSpawnPoisonCloud = value;
    }

    #endregion

    private static readonly int poisonSlapTrigger = Animator.StringToHash("PoisonSlapCastAnimTrigger");

    protected override int AnimTriggerCast => poisonSlapTrigger;
    protected override int AnimTriggerCastDelay => 0;
    public int PoisonBoneStack { get => _poisonBoneStack; set => _poisonBoneStack = value; }
    public bool IsCanDamageDeal { get => _isCanDamageDeal; set => _isCanDamageDeal = value; }

    protected override bool IsCanCast => CheckCanCast();
    private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public event System.Action OnPoisonSlapEnd;

    #endregion

    #region PrepareAndStartJob

    private void OnDisable()
    {
        OnSkillCanceled -= ClearData;
    }

    private void OnEnable()
    {
        OnSkillCanceled += ClearData;
    }

    public void PoisonSlapPreparation()
    {
        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.SetCanMove(false);
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
        Renderer.HideSmartIndicator();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget(targetInfo.GetTargets()[0]);
        SwitchPayCost();
    }

    protected override void ClearData()
    {
        _castSpeedModifier.Value = 1;

        _isUsedPoisonBallCharger = true;
        Hero.Move.StopLookAt();
        Hero.Move.SetCanMove(true);

        Targeting.ClearTarget();
        Targeting.ClearTempTarget();

        _castDeley = 0;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), _radiusTargetSearch);

                if (Targeting.GetTempTarget()?.Character != null)
                {
                    if (IsAllyTarget(Targeting.GetTempTarget()?.Character) || Targeting.GetTempTarget()?.Character == Hero)
                        Targeting.ClearTempTarget();
                }
            }

            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
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

            //if (target.CharacterState.CheckForState(States.PoisonBone) && _restorationOfGlands && _poisonBoneStack > 0)
            //{
            //    float baseChanceOfRestorationOfGlands = 0.1f;
            //    float chanceOfRestorationOfGlands = baseChanceOfRestorationOfGlands * _poisonBoneStack;

            //    if (Random.Range(0f, 1f) <= chanceOfRestorationOfGlands)
            //    {
            //        _restorationOfGlands.ReductionCooldown();
            //    }
            //}

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

            //if (Targeting.GetTarget().Character.CharacterState.CheckForState(States.PoisonBone) && _restorationOfGlands && _poisonBoneStack > 0)
            //{
            //    float baseChanceOfRestorationOfGlands = 0.1f;
            //    float chanceOfRestorationOfGlands = baseChanceOfRestorationOfGlands * _poisonBoneStack;

            //    if (Random.Range(0f, 1f) <= chanceOfRestorationOfGlands)
            //    {
            //        _restorationOfGlands.ReductionCooldown();
            //    }
            //}

            PushTarget(Targeting.GetTarget()?.Character, _distancePush, _durationPush);
        }
        UseRecharge();
    }

    private void UseRecharge()
    {
        float baseCooldownTime = _cooldownTime;

        //if (_lightweightSlap.Data.IsOpen)
        //{
        //    _cooldownTime /= 2;
        //}

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
        MoveComponent targetMoveComponent = target.GetComponent<MoveComponent>();
        
        Vector3 directionPush = _player.transform.forward;
        directionPush.y = 0f;
        directionPush.Normalize();

        Vector3 pushTarget = target.transform.position + directionPush * distancePush;

        if (targetMoveComponent.connectionToClient != null)
            targetMoveComponent.TargetRpcDoPush(pushTarget, durationPush);
        else
            targetMoveComponent.RpcDoPush(pushTarget, durationPush);
    }

    [Command]
    private void CmdPushEnemyInLightningMovement(Character target, float distancePush, float durationPush)
    {
        MoveComponent targetMoveComponent = target.GetComponent<MoveComponent>();

        Vector3 directionPush = (target.transform.position - _player.transform.position).normalized;
        Vector3 perpendicularDirection;

        if (directionPush.x < 0)
            perpendicularDirection = new Vector3(directionPush.y, -directionPush.x, 0).normalized;
        else
            perpendicularDirection = new Vector3(-directionPush.y, directionPush.x, 0).normalized;

        Vector3 pushTarget = target.transform.position + perpendicularDirection * distancePush;

        if (targetMoveComponent.connectionToClient != null) targetMoveComponent.TargetRpcDoPush(pushTarget, durationPush);
        else targetMoveComponent.RpcDoPush(pushTarget, durationPush);
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