using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public class ProtectiiveCocoonAuraDamage : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private float damageValue = 2f;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private LayerMask enemyLayer;

    private WaitForSeconds _wait;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _wait = new WaitForSeconds(tickInterval);
        StartCoroutine(DamageRoutine());
    }

    [Server]
    private IEnumerator DamageRoutine()
    {
        while (true)
        {
            yield return _wait;

            Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayer);

            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<Character>(out Character character))
                    continue;

                if (!character.TryGetComponent<IDamageable>(out IDamageable damageable))
                    continue;

                Damage damage = new Damage
                {
                    Value = damageValue,
                    Type = DamageType.Physical
                };

                damageable.TryTakeDamage(ref damage, null);
            }
        }
    }
}
