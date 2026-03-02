using UnityEngine;
using Mirror;

public class SpellMoveGetomirTo : SpellMoveCreatureTo
{
    [SerializeField] private float _minDamage = 12f;
    [SerializeField] private float _maxDamage = 18f;
    [SerializeField] private float _aoeRadius = 1.5f;

    protected override string AutoAttackTrigger => "AutoAttackScrader";

    protected override void DealDamage(Character target)
    {
        if (!isServer) return;
        if (target == null) return;

        float randomDamage = Random.Range(_minDamage, _maxDamage);
        float baseDamage = Buff.Damage.GetBuffedValue(randomDamage);

        Vector3 center = target.transform.position;

        Collider[] hits = Physics.OverlapSphere(center, _aoeRadius, TargetsLayers);

        foreach (var hit in hits)
        {
            Character character = hit.GetComponent<Character>();
            if (character == null) continue;

            if (!IsValidTarget(character)) continue;

            float finalDamage = character == target ? baseDamage : baseDamage * 0.5f;            

            Damage damage = new Damage
            {
                Value = finalDamage,
                Type = DamageType,
                PhysicAttackType = AttackRangeType
            };

            CmdApplyDamage(damage, character.gameObject);
        }
    }

    private bool IsValidTarget(Character character)
    {
        if (character == Hero) return false;
        if (character.IsDead) return false;
        return true;
    }
}