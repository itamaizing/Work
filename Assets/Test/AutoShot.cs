using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AutoShot : AutoAttackSkill
{
    [SerializeField] private ArrowProjectile projectile;
    [SerializeField] private Transform spawnPoint;

    private bool _isDelayActive = false;
    private Coroutine _autoAttackCoroutine;

    protected override int AnimTriggerAutoAttack => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => true;

    private void Start()
    {
        if (isOwned) _autoAttackCoroutine = StartCoroutine(AutoAttackRoutine());
    }

    private void OnDestroy()
    {
        if (_autoAttackCoroutine != null)
        {
            StopCoroutine(_autoAttackCoroutine);
        }
    }

    private IEnumerator AutoAttackRoutine()
    {
        while (true)
        {
            if (IsAutoattackMode && !_isDelayActive)
            {
                if (TryGetClosestTarget(out var target))
                {
                    CmdCreateProjectileAtTarget(target);
                    _isDelayActive = true;

                    yield return new WaitForSeconds(AttackDelay);
                    _isDelayActive = false;
                }
            }
            yield return null;
        }
    }

    private bool TryGetClosestTarget(out Character target)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, Radius, TargetsLayers);
        target = null;
        float minDistance = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out Character character))
            {
                float distance = Vector3.Distance(transform.position, character.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = character;
                }
            }
        }
        return target != null;
    }

    [Command]
    private void CmdCreateProjectileAtTarget(Character target)
    {
        if (target == null) return;

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 direction = (target.transform.position - spawnPosition).normalized;

        ArrowProjectile arrow = Instantiate(projectile, spawnPosition, Quaternion.LookRotation(direction));
        NetworkServer.Spawn(arrow.gameObject);
        arrow.StartFly(direction);
    }

    protected override void CastAction()
    {
        throw new System.NotImplementedException();
    }
}
