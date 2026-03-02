using UnityEngine;
using Mirror;

public class SpellMoveGetomirTo : SpellMoveCreatureTo
{
    [SerializeField] private float _minDamage = 12f;
    [SerializeField] private float _maxDamage = 18f;
    [SerializeField] private float _aoeRadius = 1.5f;
    [SerializeField] private LayerMask _characterLayer;

    protected override string AutoAttackTrigger => "AutoAttackScrader";

    protected override void DealDamage(Character target)
    {
        float randomDamage = Random.Range(_minDamage, _maxDamage);
        float finalDamage = Buff.Damage.GetBuffedValue(randomDamage);

        Vector3 center = target.transform.position;

        Collider[] hits = Physics.OverlapSphere(center, _aoeRadius, _characterLayer);

        foreach (var hit in hits)
        {
            Character character = hit.GetComponent<Character>();
            if (character == null) continue;

            if (!IsValidTarget(character)) continue;

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
        return character != Hero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _aoeRadius);
    }
}