using Mirror;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class CheliceraStrike : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private BasePsionicEnergy _basePsionicEnergy;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private JumpWithChelicera _jumpWithChelicera;
    [SerializeField] private ClawStrike _clawStrike;
    [SerializeField] private CooldownEnergy _cooldownEnergy;
    [SerializeField] private float _animSpeed = 1.4f;
    [SerializeField] private float _chanceCritDamageEvolutionTwo = 0.05f;
    [SerializeField] private float _chanceCritDamageEvolutionFour = 0.15f;
    [SerializeField] private float _chanceApplyBleeding = 0.15f;
    [SerializeField] private float _durationBleeding = 3.0f;
    [SerializeField] private float _chanceApplyBleedingIncrease = 0.4f;
    [SerializeField] private float _chanceCritDamageIncrease = 0.3f;
    [SerializeField] private float _cooldownEnergyCost = 2;

    [Header("Damage")]
    [SerializeField] private float _minDamage = 11f;
    [SerializeField] private float _maxDamage = 16f;

    #region Constants

    private const float CriticalDamageMultiplierDefault = 1.6f;
    private const float ChanceCritDamageMinMultiplier = 1.8f;
    private const float ChanceCritDamageMaxMultiplier = 2.7f;

    private const float MagicDamagePerPsiMainTarget = 0.3f;
    private const float MagicDamagePerPsiNearby = 0.5f;

    private const float RadiusLow = 1.5f;
    private const float RadiusMid = 2.0f;
    private const float RadiusHigh = 2.5f;

    private const float AttackingPsiThresholdLow = 10f;
    private const float AttackingPsiThresholdMid = 20f;
    private const float AttackingPsiThresholdHigh = 30f;

    private const float TargetSearchRadius = 0.5f;

    #endregion

    private Damage _dealDamage;
    private Animator _animator;
    private float _totalChanceApplyBleeding;
    private float _totalchanceCritDamage;
    private float _criticalDamage;
    private float _baseDamage;
    private float _additionalDamageFromSkill;
    private float _spentAttackingPsiEnergy;
    private bool _isClawStrike_Right = true;

    private static readonly int RightClawStrikeTrigger = Animator.StringToHash("CheliceraStrikeTrigger_Right");
    private static readonly int LeftClawStrikeTrigger = Animator.StringToHash("CheliceraStrikeTrigger_Left");

    protected override int AnimTriggerCast => _isClawStrike_Right ? RightClawStrikeTrigger : LeftClawStrikeTrigger;
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast => CheckIsCanCast() && _cooldownEnergy.CurrentValue >= _cooldownEnergyCost;
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public float ChanceCritDamageEvolutionFour { get => _chanceCritDamageEvolutionFour; set => _chanceCritDamageEvolutionFour = value; }

    public event Action OnCheliceraStrikeEnd;

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }
    private void OnEnable()
    {
        _baseDamage = UnityEngine.Random.Range(_minDamage, _maxDamage);
        Damage = _baseDamage;
        OnSkillCanceled += HandleSkillCanceled;
    }

    #region Talens
    private bool isCheliceraStrikeChanceDamageCrit = false;
    private bool isEvolutionTalentTwo = false;
    private bool isPsionicsTalentTwo = false;
    private bool _isChanceApplyBleedingIncrease = false;
    private bool _isChanceCritDamageIncrease = false;

    public void CheliceraStrikeChanceDamageCrit(bool value) => isCheliceraStrikeChanceDamageCrit = value;
    public void EvolutionTalentTwo(bool value) => isEvolutionTalentTwo = value;

    public void PsionicsTalentTwo(bool value, string text)
    {
        isPsionicsTalentTwo = value;
        AbilityInfoHero.FinalDescription = value ? AbilityInfoHero.Description + $" {text}" : AbilityInfoHero.Description;
    }

    public void ChanceApplyBleedingIncrease(bool value) => _isChanceApplyBleedingIncrease = value;
    public void ChanceCritDamageIncrease(bool value) => _isChanceCritDamageIncrease = value;
    #endregion

    private bool CheckIsCanCast()
    {
        return GetTarget() != null && Vector3.Distance(GetTarget().Transform.position, transform.position) <= Radius && NoObstacles(GetTarget().Transform.position, transform.position, _obstacle);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) SetTarget(targetInfo.GetTargets()[0]);
    }


    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                FindTarget(TargetSearchRadius, GetMousePoint());

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

        targetInfo.Points.Add(GetTarget().Transform.position);
        targetInfo.AddTarget(GetTarget());
        callbackDataSaved.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (GetTarget() == null) yield break;

        _baseDamage = UnityEngine.Random.Range(_minDamage, _maxDamage);
        Damage = _baseDamage;

        IDamageable damageable = GetTarget() as IDamageable;

        if (_jumpWithChelicera.IsJumpDone)
        {
            _cooldownEnergy.CastCooldownEnergySkill(_jumpWithChelicera.CooldownJump, _jumpWithChelicera);
        }

        else _cooldownEnergy.CastCooldownEnergySkill(_cooldownEnergyCost, this);

        DamageDealChelicera(damageable);
        _jumpWithChelicera.IsJumpDone = false;
        _isClawStrike_Right = !_isClawStrike_Right;

        yield return null;
    }

    private void HandleSkillCanceled()
    {
        CheliceraStrikeEnded();
        _isPlayCastAnim = false;
        ClearTarget();
        ClearTempTarget();
        AnimCastEnded();
    }

    public void DamageDealChelicera(IDamageable target)
    {
        if (target == null) return;
        Character targetCharacter = target as Character;

        if (_jumpWithChelicera.IsJumpDone)
        {
            float bonusDamage = _baseDamage * _additionalDamageFromSkill;
            Damage = _baseDamage + bonusDamage;
        }
        else Damage = _baseDamage;

        if (isEvolutionTalentTwo)
        {
            float chanceBleedingValue = UnityEngine.Random.Range(0f, 1f);
            float chanceCritValue = UnityEngine.Random.Range(0f, 1f);

            _totalChanceApplyBleeding = _chanceApplyBleeding;
            _totalchanceCritDamage = _chanceCritDamageEvolutionTwo;

            if (_isChanceApplyBleedingIncrease && CheckStateForBleeding(targetCharacter)) _totalChanceApplyBleeding += _chanceApplyBleedingIncrease;
            if (_isChanceCritDamageIncrease && CheckStateForBleeding(targetCharacter)) _totalchanceCritDamage += _chanceCritDamageIncrease;

            if (chanceCritValue <= _totalchanceCritDamage) _criticalDamage = CriticalDamageDeal(Damage, CriticalDamageMultiplierDefault);

            if (chanceBleedingValue <= _totalChanceApplyBleeding && targetCharacter != null) CmdAddState(targetCharacter);
        }

        if (isCheliceraStrikeChanceDamageCrit)
        {
            float chanceCritValue = UnityEngine.Random.Range(0f, 1f);
            float chanceCritDamageValue = UnityEngine.Random.Range(ChanceCritDamageMinMultiplier, ChanceCritDamageMaxMultiplier);

            _totalchanceCritDamage = _chanceCritDamageEvolutionFour;

            if (_isChanceCritDamageIncrease && CheckStateForBleeding(targetCharacter)) _totalchanceCritDamage += _chanceCritDamageIncrease;

            if (chanceCritValue <= _chanceCritDamageEvolutionFour) _criticalDamage = CriticalDamageDeal(Damage, chanceCritDamageValue);
        }

        _dealDamage = new Damage()
        {
            Value = Damage + _criticalDamage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        if (_attackingPsionicEnergy.IsAttackingPsiEnergy && targetCharacter != null) DamageDealWithAttackingPsionicEnergy(targetCharacter);

        CmdApplyDamage(_dealDamage, target.gameObject);

        _criticalDamage = 0f;
        _dealDamage.Value = 0f;
        Damage = _baseDamage;
    }

    private float CriticalDamageDeal(float criticalDamage, float multiplierCrit)
    {
        return criticalDamage * multiplierCrit;
    }

    private bool CheckStateForBleeding(Character character)
    {
        States[] blockingStates = { States.Stun, States.Stupefaction, States.TentacleGrip };
        return character != null && blockingStates.Any(state => character.CharacterState.CheckForState(state));
    }

    private void DamageDealWithAttackingPsionicEnergy(Character targetCharacter)
    {
        float attackingPsi = _spentAttackingPsiEnergy;

        if (!isPsionicsTalentTwo && attackingPsi <= 0) return;

        float radius = attackingPsi >= AttackingPsiThresholdHigh ? RadiusHigh : attackingPsi >= AttackingPsiThresholdMid ? RadiusMid : RadiusLow;

        if (attackingPsi >= AttackingPsiThresholdLow)
        {
            Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, radius, _targetsLayers);
            foreach (var enemyCollider in nearbyEnemies)
                if (enemyCollider.TryGetComponent<Character>(out var enemy) && enemy != targetCharacter)
                {
                    CmdDispel(enemy);
                    ApplyDamage(attackingPsi, MagicDamagePerPsiNearby, enemy);
                }

            TotalMagicDamageEnemy(targetCharacter, attackingPsi, MagicDamagePerPsiMainTarget);
            CmdDispel(targetCharacter);
        }
    }

    private void ApplyDamage(float attackingPsi, float magicDamagePerPsiNearby, Character enemy)
    {
        if (enemy != _player)
        {
            TotalMagicDamageEnemy(enemy, attackingPsi, magicDamagePerPsiNearby);
        }
    }

    private void TotalMagicDamageEnemy(Character enemy, float attackingPsi, float magicDamage)
    {
        float totalMagicDamageEnemy = attackingPsi * magicDamage;

        var magicDamageNearby = new Damage
        {
            Value = totalMagicDamageEnemy,
            Type = DamageType.Magical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        CmdApplyDamage(magicDamageNearby, enemy.gameObject);
    }

    public void CheliceraStrikePreparingForAnim()
    {
        _player.Move.SetCanMove(false);
        _hero.Move.StopMoveAndAnimationMove();
        if (_attackingPsionicEnergy.IsAttackingPsiEnergy && _attackingPsionicEnergy.CurrentValue > 0f) TrySpendAttackingPsi();
        else _spentAttackingPsiEnergy = 0;
    }

    public void SetAdditionalDamage(float value)
    {
        _additionalDamageFromSkill = value;
    }

    public void CheliceraStrikeCast()
    {
        AnimStartCastCoroutine();
    }

    public void CheliceraStrikeEnded()
    {
        OnCheliceraStrikeEnd?.Invoke();
        _player.Move.StopLookAt();
        _player.Move.SetCanMove(true);
        AnimCastEnded();
    }

    public void ClearDataCheliceraStrike()
    {
        ClearData();
        StopAutoDraw();
    }

    public void TrySpendAttackingPsi()
    {
        _spentAttackingPsiEnergy = _attackingPsionicEnergy.CurrentValue;
        CmdUseAttackingEnergy(_attackingPsionicEnergy.CurrentValue);
    }

    [Command]
    private void CmdUseAttackingEnergy(float value)
    {
        _attackingPsionicEnergy.CurrentValue -= value;
    }

    [Command]
    private void CmdAddState(Character character)
    {
        character.CharacterState.AddState(States.Bleeding, _durationBleeding, 0, _player.gameObject, null);
    }

    [Command]
    private void CmdDispel(Character targetCharacter)
    {
        targetCharacter.CharacterState.DispelStates(StateType.Magic, targetCharacter.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, true);
    }
    protected override void ClearData()
    {
        ClearTempTarget();
        ClearTarget();
        AnimCastEnded();
    }
}