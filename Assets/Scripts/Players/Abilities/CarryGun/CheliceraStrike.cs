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
    [SerializeField] private ClawStrike clawStrike;
    [SerializeField] private CooldownEnergy cooldownEnergy;
    [SerializeField] private float animSpeed = 1.4f;
    [SerializeField] private float chanceCritDamageEvolutionTwo = 0.05f;
    [SerializeField] private float chanceCritDamageEvolutionFour = 0.15f;
    [SerializeField] private float chanceApplyBleeding = 0.15f;
    [SerializeField] private float durationBleeding = 3.0f;
    [SerializeField] private float chanceApplyBleedingIncrease = 0.4f;
    [SerializeField] private float chanceCritDamageIncrease = 0.3f;
    [SerializeField] private float cooldownEnergyCost = 2;

    private Damage _dealDamage;
    private Animator _animator;
    private Character _target;
    private Character _runtimeTarget;
    private float _totalChanceApplyBleeding;
    private float _totalchanceCritDamage;
    private float _criticalDamage;
    private float _baseDamage;
    private float _additionalDamageFromSkill;
    private float _spentAttackingPsiEnergy;
    private bool _isClawStrike_Right = true;
    private Coroutine _castDelayResetCoroutine;

    private static readonly int RightClawStrikeTrigger = Animator.StringToHash("CheliceraStrikeTrigger_Right");
    private static readonly int LeftClawStrikeTrigger = Animator.StringToHash("CheliceraStrikeTrigger_Left");

    protected override int AnimTriggerCast => _isClawStrike_Right ? RightClawStrikeTrigger : LeftClawStrikeTrigger;
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast => _target != null && CheckIsCanCast() && cooldownEnergy.CurrentValue >= cooldownEnergyCost;

    public float ChanceCritDamageEvolutionFour { get => chanceCritDamageEvolutionFour; set => chanceCritDamageEvolutionFour = value; }

    public event System.Action OnCheliceraStrikeEnd;

    private void Start() => _animator = GetComponent<Animator>();

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }
    private void OnEnable()
    {
        _baseDamage = UnityEngine.Random.Range(11f, 13f);
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
    public void CheliceraStrikeSpeed(bool value) => Hero.Animator.speed = value ? 1.4f : 1f;
    public void EvolutionTalentTwo(bool value) => isEvolutionTalentTwo = value;
    public void PsionicsTalentTwo(bool value) => isPsionicsTalentTwo = value;
    public void ChanceApplyBleedingIncrease(bool value) => _isChanceApplyBleedingIncrease = value;
    public void ChanceCritDamageIncrease(bool value) => _isChanceCritDamageIncrease = value;
    #endregion

    private bool CheckIsCanCast()
    {
        return _target != null &&
            Vector3.Distance(_target.transform.position, transform.position) <= Radius && 
            NoObstacles(_target.transform.position, transform.position, _obstacle);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();

                if (_target != null)
                {
                    _runtimeTarget = _target;
                    _target.SelectedCircle.IsActive = true;
                    _isCanCancle = false;
                }

                break;
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(_runtimeTarget);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null) yield return null;
        if (!IsTargetInRange()) yield return null;

        _baseDamage = UnityEngine.Random.Range(11f, 13f);
        Damage = _baseDamage;

        if (_jumpWithChelicera.IsJumpDone)
        {
            cooldownEnergy.CastCooldownEnergySkill(_jumpWithChelicera.ChargeCooldown, _jumpWithChelicera);
            _jumpWithChelicera.IsJumpDone = false;
        }

        else cooldownEnergy.CastCooldownEnergySkill(cooldownEnergyCost, this);

        DamageDealChelicera(_target.gameObject);
        _isClawStrike_Right = !_isClawStrike_Right;

        _hero.Move.StopLookAt();
        Hero.Move.CanMove = true;

        yield return null;
    }

    private void HandleSkillCanceled()
    {
        _target = null;
        Hero.Move.CanMove = true;
        Hero.Move.StopLookAt();
    }

    private bool IsTargetInRange()
    {
        return Vector3.Distance(_player.transform.position, _target.transform.position) <= Radius;
    }

    public void SetTarget(Character target)
    {
        _target = target;
    }

    public void DamageDealChelicera(GameObject target)
    {
        if (target == null) return;

        Character targetCharacter = target.GetComponent<Character>();

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

            _totalChanceApplyBleeding = chanceApplyBleeding;
            _totalchanceCritDamage = chanceCritDamageEvolutionTwo;

            if (_isChanceApplyBleedingIncrease && CheckStateForBleeding()) _totalChanceApplyBleeding += chanceApplyBleedingIncrease;
            if (_isChanceCritDamageIncrease && CheckStateForBleeding()) _totalchanceCritDamage += chanceCritDamageIncrease;

            if (chanceCritValue <= _totalchanceCritDamage) _criticalDamage = CriticalDamageDeal(targetCharacter, Damage, 1.6f);

            if (chanceBleedingValue <= _totalChanceApplyBleeding) CmdAddState(targetCharacter);
        }

        if (isCheliceraStrikeChanceDamageCrit)
        {
            float chanceCritValue = UnityEngine.Random.Range(0f, 1f);
            float chanceCritDamageValue = UnityEngine.Random.Range(1.8f, 2.7f);

            _totalchanceCritDamage = chanceCritDamageEvolutionFour;

            if (_isChanceCritDamageIncrease && CheckStateForBleeding()) _totalchanceCritDamage += chanceCritDamageIncrease;

            if (chanceCritValue <= chanceCritDamageEvolutionFour) _criticalDamage = CriticalDamageDeal(targetCharacter, Damage, chanceCritDamageValue);
        }

        _dealDamage = new Damage()
        {
            Value = Damage + _criticalDamage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        if (_attackingPsionicEnergy.IsAttackingPsiEnergy && targetCharacter != null) DamageDealWithAttackingPsionicEnergy(targetCharacter);

        CmdApplyDamage(_dealDamage, target);

        _criticalDamage = 0f;
        _dealDamage.Value = 0f;
        Damage = _baseDamage;
    }

    private float CriticalDamageDeal(Character target, float criticalDamage, float multiplierCrit)
    {
        criticalDamage = CalculationCriticalDamage(criticalDamage, multiplierCrit);

        return criticalDamage;
    }

    private float CalculationCriticalDamage(float baseDamage, float multiplierCrit)
    {
        float criticalDamage = baseDamage;
        criticalDamage *= multiplierCrit;

        return criticalDamage;
    }

    private bool CheckStateForBleeding()
    {
        States[] blockingStates = { States.Stun, States.Stupefaction, States.TentacleGrip };
        if (blockingStates.Any(state => _target.CharacterState.CheckForState(state))) return true;
        else return false;
    }

    private void DamageDealWithAttackingPsionicEnergy(Character targetCharacter)
    {
        float attackingPsi = _spentAttackingPsiEnergy;

        float magicDamagePerPsiMainTarget = 0.3f;
        float magicDamagePerPsiNearby = 0.5f;

        if (!isPsionicsTalentTwo && attackingPsi <= 0) return;

        else if (attackingPsi >= 10)
        {
            Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, 1.5f, _targetsLayers);
            foreach (var enemyCollider in nearbyEnemies)
                if (enemyCollider.TryGetComponent<Character>(out var enemy) && enemy != targetCharacter)
                {
                    CmdDispel(enemy);
                    ApplyDamage(attackingPsi, magicDamagePerPsiNearby, enemy);
                }

            TotalMagicDamageEnemy(targetCharacter, attackingPsi, magicDamagePerPsiMainTarget);
            CmdDispel(targetCharacter);
        }

        else if (attackingPsi >= 20)
        {
            Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, 2f, _targetsLayers);
            foreach (var enemyCollider in nearbyEnemies)
                if (enemyCollider.TryGetComponent<Character>(out var enemy) && enemy != targetCharacter)
                {
                    CmdDispel(enemy);
                    ApplyDamage(attackingPsi, magicDamagePerPsiNearby, enemy);
                }

            TotalMagicDamageEnemy(targetCharacter, attackingPsi, magicDamagePerPsiMainTarget);
            CmdDispel(targetCharacter);
        }

        else if (attackingPsi >= 30)
        {
            Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, 2.5f, _targetsLayers);
            foreach (var enemyCollider in nearbyEnemies)
                if (enemyCollider.TryGetComponent<Character>(out var enemy) && enemy != targetCharacter)
                {
                    CmdDispel(enemy);
                    ApplyDamage(attackingPsi, magicDamagePerPsiNearby, enemy);
                }

            TotalMagicDamageEnemy(targetCharacter, attackingPsi, magicDamagePerPsiMainTarget);
            CmdDispel(targetCharacter);
        }
    }

    private void ApplyDamage(float attackingPsi, float magicDamagePerPsiNearby, Character enemy)
    {
        if (enemy != _player) TotalMagicDamageEnemy(enemy, attackingPsi, magicDamagePerPsiNearby);

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

    public void CheliceraStrikeSpeedAnim()
    {
        if (Hero.Move.CanMove == true) Hero.Move.CanMove = false;
        _player.Animator.SetFloat("CheliceraStrikeSpeed", 1f / animSpeed);
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
        _isCanCancle = true;
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

    #region CommandMethods

    [Command]
    private void CmdUseAttackingEnergy(float value)
    {
        _attackingPsionicEnergy.CurrentValue -= value;
    }

    #endregion

    [Command]
    private void CmdAddState(Character character)
    {
        character.CharacterState.AddState(States.Bleeding, durationBleeding, 0, _player.gameObject, null);
    }

    [Command]
    private void CmdDispel(Character targetCharacter)
    {
        targetCharacter.CharacterState.DispelStates(StateType.Magic, targetCharacter.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, true);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = (Character)targetInfo.Targets[0];
        _hero.Move.LookAtTransform(_target.transform);
        _isCanCancle = false;
    }

    protected override void ClearData()
    {
        _target = null;
    }
}
