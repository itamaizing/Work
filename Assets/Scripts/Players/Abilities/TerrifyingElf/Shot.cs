using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Shot : AutoAttackSkill
{
    [SerializeField] private ArrowProjectile _projectile;
    [SerializeField] private HeroComponent _playerLinks;
    private bool isAttacking = false;

    protected override int AnimTriggerAutoAttack => 0;
    protected override int AnimTriggerCastDelay => 0;

    protected override void CastAction()
    {
        if (_target != null)
        {
            CmdCreateProjectile(_target.transform);
        }
    }

    private Character GetNearestTargetInRadius()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, Radius, _targetsLayers);
        Character nearestTarget = null;
        float shortestDistance = Radius;

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out Character character) && character != Hero)
            {
                float distance = Vector3.Distance(transform.position, character.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestTarget = character;
                }
            }
        }
        return nearestTarget;
    }

    protected override IEnumerator PrepareJob()
    {
        while (!IsCasting)
        {
            if (IsAutoattackMode)
            {
                if (GetMouseButton && !isAttacking)
                {
                    _target = GetNearestTargetInRadius();

                    if (_target != null && IsTargetInRadius(Radius, _target.transform))
                    {
                        isAttacking = true;
                        StartCoroutine(AutoAttackRoutine());
                    }
                }
            }
            else if (!IsAutoattackMode && !isAttacking && GetMouseButton && TryPayCost())
            {
                isAttacking = true;
                Vector3 clickPosition = GetMousePoint();
                CmdCreateProjectileAtPosition(clickPosition);

                yield return new WaitForSeconds(CastDeley);
                isAttacking = false;
            }

            yield return null;
        }
    }

    [Command]
    private void CmdCreateProjectile(Transform target)
    {
        if (target == null) return;

        ArrowProjectile projectile = Instantiate(_projectile, transform.position, Quaternion.identity);
        projectile.Init(_playerLinks, 0, false, this);
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(projectile.gameObject);
        projectile.StartFly(target.position);
        RpcInit(projectile.gameObject);
    }

    [Command]
    private void CmdCreateProjectileAtPosition(Vector3 position)
    {
        ArrowProjectile projectile = Instantiate(_projectile, transform.position, Quaternion.identity);
        projectile.Init(_playerLinks, 0, false, this);
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(projectile.gameObject);
        projectile.StartFly(position);
        RpcInit(projectile.gameObject);
    }

    [ClientRpc]
    private void RpcInit(GameObject gameObject)
    {
        ArrowProjectile projectile = gameObject.GetComponent<ArrowProjectile>();
        projectile.Init(_playerLinks, 0, false, this);
    }

    private IEnumerator AutoAttackRoutine()
    {
        while (IsAutoattackMode && _target != null && IsCanCast)
        {
            if (TryPayCost())
            {
                CmdCreateProjectile(_target.transform);
            }

            yield return new WaitForSeconds(AttackDelay);
        }
        isAttacking = false;
    }
}
