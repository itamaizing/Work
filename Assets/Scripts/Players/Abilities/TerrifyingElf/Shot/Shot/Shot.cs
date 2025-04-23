using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Shot : AutoAttackSkill
{
    [SerializeField] private ArrowProjectile _projectile;
    [SerializeField] private HeroComponent _playerLinks;
    [SerializeField] private Transform _spawnPoint;
    private Vector3 _targetPoint = Vector3.positiveInfinity;

    protected override int AnimTriggerAutoAttack => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => true;

    private bool _isDelayActive;

    protected override void CastAction()
    {
        if (_target != null)
        {
            CmdCreateProjectile(_target.transform);
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (float.IsPositiveInfinity(_targetPoint.x))
        {
            if (GetMouseButton && IsCanCast)
            {
                Vector3 clickedPoint = GetMousePoint();

                if (IsPointInRadius(Radius, clickedPoint) && TryGetDamageableAtPoint(clickedPoint, out var damageable))
                {
                    _targetPoint = clickedPoint;
                    yield break;
                }
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (!IsPointInRadius(Radius, _targetPoint))
        {
            ClearData();
            yield break;
        }

        if (IsAutoattackMode)
        {
            while (IsAutoattackMode)
            {
                if (!IsPointInRadius(Radius, _targetPoint))
                {
                    ClearData();
                    yield break;
                }

                if (_isDelayActive)
                {
                    yield return null;
                    continue;
                }

                CmdCreateProjectileAtPosition(_targetPoint);
                _isDelayActive = true;

                yield return new WaitForSeconds(AttackDelay);
                _isDelayActive = false;
            }
        }
        else
        {
            if (!IsPointInRadius(Radius, _targetPoint))
            {
                ClearData();
                yield break;
            }

            CmdCreateProjectileAtPosition(_targetPoint);
            yield return new WaitForSeconds(CastDeley);
        }
    }

    private bool TryGetDamageableAtPoint(Vector3 point, out IDamageable damageable)
    {
        damageable = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _targetsLayers))
        {
            if (hit.collider.TryGetComponent(out damageable))
            {
                return true;
            }
        }

        return false;
    }

    [Command]
    protected void CmdCreateProjectile(Transform target)
    {
        if (target == null) return;

        Vector3 spawnPosition = _spawnPoint != null ? _spawnPoint.position : transform.position;
        Vector3 direction = (target.position - spawnPosition).normalized;

        ArrowProjectile projectile = Instantiate(_projectile, spawnPosition, Quaternion.LookRotation(direction));
        projectile.Init(_playerLinks, 0, false, this);
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(projectile.gameObject);
        projectile.StartFly(direction);
        RpcInit(projectile.gameObject);
    }

    [Command]
    protected void CmdCreateProjectileAtPosition(Vector3 position)
    {
        Vector3 spawnPosition = _spawnPoint != null ? _spawnPoint.position : transform.position;
        Vector3 direction = (position - spawnPosition).normalized;

        ArrowProjectile projectile = Instantiate(_projectile, spawnPosition, Quaternion.LookRotation(direction));
        projectile.Init(_playerLinks, 0, false, this);
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(projectile.gameObject);
        projectile.StartFly(direction);
        RpcInit(projectile.gameObject);
    }

    [ClientRpc]
    protected void RpcInit(GameObject gameObject)
    {
        if (gameObject == null) return;

        ArrowProjectile projectile = gameObject.GetComponent<ArrowProjectile>();
        if (projectile != null)
        {
            projectile.Init(_playerLinks, 0, false, this);
        }
    }

    protected override void ClearData()
    {
        base.ClearData();
        _targetPoint = Vector3.positiveInfinity;
        _isDelayActive = false;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        throw new NotImplementedException();
    }
}