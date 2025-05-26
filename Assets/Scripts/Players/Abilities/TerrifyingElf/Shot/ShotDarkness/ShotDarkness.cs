using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShotDarkness : Skill
{
    [SerializeField] private ArrowProjectile projectile;
    [SerializeField] private HeroComponent playerLinks;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Ghost ghostSkill;
    [SerializeField] private AudioClip audioClip;

    private const string _startAnimTrigger = "ShotCastDelayStartAnimTrigger";
    private const string _endAnimTrigger = "ShotCastDelayEndAnimTrigger"; // убрать в дальнейшем две анимации, остаток от автоатаки

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private AudioSource _audioSource;
    private int _consecutiveShots;

    protected override int AnimTriggerCastDelay => Animator.StringToHash(_startAnimTrigger);
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast =>
        Vector3.Distance(_targetPoint, transform.position) <= Radius &&
        NoObstacles(_targetPoint, transform.position, _obstacle);

    private void OnDestroy()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        OnSkillCanceled += HandleSkillCanceled;
        Hero.Animator.speed = Hero.Animator.speed / CastDeley;

        while (float.IsPositiveInfinity(_targetPoint.x))
        {
            if (GetMouseButton)
            {
                Vector3 clickedPoint = GetMousePoint();

                if (IsPointInRadius(Radius, clickedPoint) &&
                    NoObstacles(clickedPoint, transform.position, _obstacle) &&
                    TryGetDamageableAtPoint(clickedPoint, out var damageable))
                {
                    _targetPoint = clickedPoint;

                    if (damageable is Component component) Hero.Move.LookAtTransform(component.transform);
                    else Hero.Move.LookAtPosition(_targetPoint);

                    Hero.Move.CanMove = false;
                }
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (!IsCanCast)
        {
            ClearData();
            Hero.Move.CanMove = true;
            yield break;
        }

        CmdCreateProjectileAtPosition(_targetPoint);
        ProcessGhostCooldownReduction();

        WorkAnimator(_startAnimTrigger, _endAnimTrigger);
        //HandleSkillCanceled();
        ClearData();
    }

    private void ProcessGhostCooldownReduction()
    {
        if (!ghostSkill || !ghostSkill.CooldownGhostShotActive) return;

        _consecutiveShots++;
        if (_consecutiveShots >= 3)
        {
            ghostSkill.ReductionCooldownCharges(1);
            _consecutiveShots = 0;
        }
    }

    private void HandleSkillCanceled()
    {
        if (_hero?.Move != null)
        {
            Hero.Move.CanMove = true;
            Hero.Animator.speed = 1;
            Hero.Move.StopLookAt();
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

    private void WorkAnimator(string oldAnim, string newAnim)
    {
        _hero.Animator.ResetTrigger(Animator.StringToHash(oldAnim));
        _hero.NetworkAnimator.ResetTrigger(Animator.StringToHash(oldAnim));
        _hero.Animator.CrossFade(newAnim, 0.1f);
        CmdCrossFade(newAnim);
    }

    [Command]
    protected void CmdCreateProjectileAtPosition(Vector3 position)
    {
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 direction = (position - spawnPosition).normalized;

        if (direction == Vector3.zero) return;

        ArrowProjectile proj = Instantiate(projectile, spawnPosition, Quaternion.LookRotation(direction));
        proj.Init(playerLinks, 0, false, this);
        SceneManager.MoveGameObjectToScene(proj.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(proj.gameObject);
        proj.StartFly(direction);
        RpcInit(proj.gameObject);
        RpcPlayShotSound();
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

        ArrowProjectile proj = gameObject.GetComponent<ArrowProjectile>();
        if (proj != null) proj.Init(playerLinks, 0, false, this);
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && audioClip != null)
            _audioSource.PlayOneShot(audioClip);
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        _consecutiveShots = 0;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _targetPoint = targetInfo.Points[0];
    }
}
