using Mirror;
using System.Collections;
using UnityEngine;

public class CheliceraStrike : AutoAttackSkill
{
    [SerializeField] private Character _player;
    [SerializeField] private BasePsionicEnergy _basePsionicEnergy;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private JumpWithChelicera _jumpWithChelicera;
    [SerializeField] private float animSpeed = 1.4f;
    [SerializeField] private bool isEvolutionTalentTwo = false;
    [SerializeField] private bool isPsionicsTalentTwo = false;

    private Damage _dealDamage;
    private Animator _animator;
    private Character target;
    private float _baseDamage;
    private float _criticalDamage;
    private float _additionalDamageFromSkill;
    private float _spentAttackingPsiEnergy;

    private float _chanceCritDamage = 0.05f;
    private float _chanceApplyBleeding = 0.15f;
    private float _durationBleeding = 3.0f;
    private bool _isClawStrike_Right = true;

    private static readonly int RightClawStrikeTrigger = Animator.StringToHash("CheliceraStrikeTrigger_Right");
    private static readonly int LeftClawStrikeTrigger = Animator.StringToHash("CheliceraStrikeTrigger_Left");

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerAutoAttack => _isClawStrike_Right ? RightClawStrikeTrigger : LeftClawStrikeTrigger;

    public event System.Action OnCheliceraStrikeEnd;

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    protected override void CastAction()
    {
        if (_target == null) return;
        if (!IsTargetInRange()) return;

        DamageDealChelicera(_target.gameObject);

        _isClawStrike_Right = !_isClawStrike_Right;
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

        _baseDamage = Random.Range(11f, 13f);

        if (_jumpWithChelicera.IsJumpDone)
        {
            float bonusDamage = _baseDamage * _additionalDamageFromSkill;
            _baseDamage += bonusDamage;
        }

        if (isEvolutionTalentTwo)
        {
            float chanceCritValue = Random.Range(0f, 1f);
            float chanceBleedingValue = Random.Range(0f, 1f);

            if (chanceBleedingValue <= _chanceApplyBleeding) CmdAddState(targetCharacter);
            if (chanceCritValue <= _chanceCritDamage) _criticalDamage = CriticalDamageDeal(targetCharacter, _baseDamage);
        }

        _dealDamage = new Damage()
        {
            Value = _baseDamage + _criticalDamage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        if (_attackingPsionicEnergy.IsAttackingPsiEnergy && targetCharacter != null) DamageDealWithAttackingPsionicEnergy(targetCharacter);

        CmdApplyDamage(_dealDamage, target);

        _criticalDamage = 0f;
        _dealDamage.Value = 0f;
    }

    private float CriticalDamageDeal(Character target, float criticalDamage)
    {
        criticalDamage = CalculationCriticalDamage(criticalDamage);

        return criticalDamage;
    }

    private float CalculationCriticalDamage(float baseDamage)
    {
        float criticalDamage = baseDamage;
        float multiplierCrit = 1.6f;

        criticalDamage *= multiplierCrit;

        return criticalDamage;
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

    public void CheliceraStrikeCast()
    {
        AnimCastAction();
    }

    public void CheliceraStrikeEnded()
    {
        OnCheliceraStrikeEnd?.Invoke();
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

    #region Talens

    public void EvolutionTalentTwo(bool value)
    {
        isEvolutionTalentTwo = value;   
    }

    public void PsionicsTalentTwo(bool value)
    {
        isPsionicsTalentTwo = value;
    }

    #endregion

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
        character.CharacterState.AddState(States.Bleeding, _durationBleeding, 0, _player.gameObject, null);
    }

    [Command]
    private void CmdDispel(Character targetCharacter)
    {
        targetCharacter.CharacterState.DispelStates(StateType.Magic, targetCharacter.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, true);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        throw new System.NotImplementedException();
    }
}
