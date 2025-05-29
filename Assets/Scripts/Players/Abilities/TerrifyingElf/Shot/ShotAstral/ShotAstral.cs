using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShotAstral : Skill
{
    [SerializeField] private ArrowAstralProjectile _projectile;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private HeroComponent _playerLinks;

    private const string _startAnimTrigger = "ShotCastDelayStartAnimTrigger";
    private const string _endAnimTrigger = "ShotCastDelayEndAnimTrigger";

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private Character _target;

    protected override bool IsCanCast => CheckCanCast();

    protected override int AnimTriggerCastDelay => Animator.StringToHash(_startAnimTrigger);

    protected override int AnimTriggerCast => 0;

    private void OnDestroy()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    private bool CheckCanCast()
    {
        if (_target == null)
            return Vector3.Distance(_targetPoint, transform.position) <= Radius &&
                   NoObstacles(_targetPoint, transform.position, _obstacle);

        return Vector3.Distance(_targetPoint, transform.position) <= Radius &&
               NoObstacles(_targetPoint, transform.position, _obstacle) ||
               Vector3.Distance(_target.transform.position, transform.position) <= Radius &&
               NoObstacles(_target.transform.position, transform.position, _obstacle);
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null) CmdCreateProjectile(_target.transform);
        else CmdCreateProjectileAtPosition(_targetPoint);

        HandleSkillCanceled();
        ClearData();
        yield return null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        OnSkillCanceled += HandleSkillCanceled;
        Hero.Animator.speed = Hero.Animator.speed / CastDeley;

        while (float.IsPositiveInfinity(_targetPoint.x) && _target == null)
        {
            if (GetMouseButton)
            {
                _targetPoint = GetMousePoint();
                _target = GetRaycastTarget(false);

                if (_target != null && _target == _playerLinks)
                {
                    _playerLinks.CharacterState.CmdAddState(States.Astral, _projectile.Duration, 0, gameObject, "ShotAstral");
                    ClearData();
                    yield break;
                }

                if (IsPointInRadius(Radius, _targetPoint) &&
                    NoObstacles(_targetPoint, transform.position, _obstacle))
                {
                    if (_target != null && _target != _playerLinks)
                    {
                        _targetPoint = _target.transform.position;
                        Hero.Move.LookAtTransform(_target.transform);
                        Hero.Move.CanMove = false;
                    }

                    else
                    {
                        Hero.Move.LookAtPosition(_targetPoint);
                        Hero.Move.CanMove = false;
                    }
                }
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_targetPoint);
        callbackDataSaved(targetInfo);
    }

    private void HandleSkillCanceled()
    {
        if (_hero != null && _hero.Move != null)
        {
            Hero.Move.CanMove = true;
            Hero.Animator.speed = 1;
            Hero.Move.StopLookAt();
        }

        WorkAnimator(_startAnimTrigger, _endAnimTrigger);
    }

    private void WorkAnimator(string oldAnim, string newAnim)
    {
        _hero.Animator.ResetTrigger(Animator.StringToHash(oldAnim));
        _hero.NetworkAnimator.ResetTrigger(Animator.StringToHash(oldAnim));

        _hero.Animator.CrossFade(newAnim, 0.1f);
        CmdCrossFade(newAnim);
    }

    [Command]
    protected void CmdCreateProjectile(Transform target)
    {
        if (target == null) return;

        Vector3 spawnPosition = _spawnPoint != null ? _spawnPoint.position : transform.position;
        Vector3 direction = (target.position - spawnPosition).normalized;

        ArrowAstralProjectile projectile = Instantiate(_projectile, spawnPosition, Quaternion.LookRotation(direction));
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

        ArrowAstralProjectile projectile = Instantiate(_projectile, spawnPosition, Quaternion.LookRotation(direction));
        projectile.Init(_playerLinks, 0, false, this);
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(projectile.gameObject);
        projectile.StartFly(direction);
        RpcInit(projectile.gameObject);
    }

    [Command]
    private void CmdCrossFade(string newAnim)
    {
        _hero.Animator.CrossFade(newAnim, 0.1f);
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
        _target = null;
        _targetPoint = Vector3.positiveInfinity;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _targetPoint = targetInfo.Points[0];
    }
}
