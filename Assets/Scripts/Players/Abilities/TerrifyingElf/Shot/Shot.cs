using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Shot : AutoAttackSkill
{
    [SerializeField] private ArrowProjectile _projectile;
    [SerializeField] private HeroComponent _playerLinks;
    private bool isAttacking = false;

    protected override void CastAction()
    {
        if (_target != null)
        {
            CmdCreateProjectile(_target.transform);
        }
    }

    private void SelectTargetOrPosition(out Vector2 clickPosition, out Character target)
    {
        clickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(clickPosition, Vector2.zero, 100f, _targetsLayers);

        if (hit.collider != null && hit.collider.TryGetComponent(out Character hitTarget))
        {
            target = hitTarget;
        }
        else
        {
            target = null;
        }
    }

    protected override IEnumerator PrepareJob()
    {
        while (!IsCasting)
        {
            if (IsAutoattackMode)
            {
                if (GetMouseButton && !isAttacking)
                {
                    SelectTargetOrPosition(out _, out _target);

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

                Vector2 clickPosition;
                SelectTargetOrPosition(out clickPosition, out _);
                CmdCreateProjectileAtPosition(clickPosition);

                yield return new WaitForSeconds(CastDeley);
                isAttacking = false;
            }

            yield return null;
        }
    }

    [Command]
    protected void CmdCreateProjectile(Transform target)
    {
        if (target == null)
        {
            return;
        }

        ArrowProjectile projectile = Instantiate(_projectile, transform.position, Quaternion.identity);
        projectile.Init(_playerLinks, 0, false, this);
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(projectile.gameObject);
        projectile.StartFly(target.position);
        RpcInit(projectile.gameObject);
    }

    [Command]
    protected void CmdCreateProjectileAtPosition(Vector2 position)
    {
        ArrowProjectile projectile = Instantiate(_projectile, transform.position, Quaternion.identity);
        projectile.Init(_playerLinks, 0, false, this);
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(projectile.gameObject);
        projectile.StartFly(position);
        RpcInit(projectile.gameObject);
    }

    [ClientRpc]
    protected void RpcInit(GameObject gameObject)
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

            yield return new WaitForSeconds(AttackSpeed);
        }
        isAttacking = false;
    }
}