using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Shot : Skill
{
    [SerializeField] private ArrowProjectile projectile;
    [SerializeField] private HeroComponent playerLinks;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Ghost ghostSkill;
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private float minDamage;
    [SerializeField] private float maxDamage;

    private const string _startAnimTrigger = "ShotCastDelayTrigger";
    private const string _endAnimTrigger = "ShotCastDelayEndAnimTrigger"; // remove two animations later, the remainder of the auto-attack

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private AudioSource _audioSource;
    private int _consecutiveShots;
    private Character _lastTarget;

    protected override int AnimTriggerCastDelay => Animator.StringToHash(_startAnimTrigger);
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast =>
        Vector3.Distance(_targetPoint, transform.position) <= Radius &&
        NoObstacles(_targetPoint, transform.position, _obstacle);

    private void OnDestroy()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public void ShotAnimationMove()
    {
        if (_hero == null || _hero.Move == null) return;

        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;

        Vector3 direction = _targetPoint - _hero.transform.position;
        bool badDirection = float.IsInfinity(_targetPoint.x) || direction.sqrMagnitude < 0.0001f;

        Damage = UnityEngine.Random.Range(minDamage, maxDamage + 1);

        if (badDirection)
        {
            _hero.Move.StopLookAt();
            return;
        }

        else Hero.Move.LookAtPosition(_targetPoint);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Hero.Animator.speed = Hero.Animator.speed / CastDeley;
        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        while (float.IsPositiveInfinity(_targetPoint.x))
        {
            if (GetMouseButton)
            {
                Vector3 clickedPoint = GetMousePoint();

                if (NoObstacles(clickedPoint, transform.position, _obstacle) && TryGetDamageableAtPoint(clickedPoint, out var damageable))
                {
                    if (_lastTarget == null) _lastTarget = (damageable as Component)?.GetComponent<Character>();
                    if (multiMagic != null) multiMagic.LastTarget = _lastTarget;
                    _targetPoint = clickedPoint;
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
            Hero.Move.CanMove = true;
            Hero.Move.StopLookAt();
            ClearData();
            yield break;
        }

        CmdCreateProjectileAtPosition(_targetPoint, Damage);

        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;
        if (multiMagic != null)
        {
            foreach (var character in multiMagic.PopPendingTargets())
            {
                TryPayCost();
                CmdCreateProjectileAtPosition(character.transform.position, Damage);
            }
        }

        ProcessGhostCooldownReduction();
        WorkAnimator(_startAnimTrigger, _endAnimTrigger);
        HandleSkillCanceled();
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
            _lastTarget = null;
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
    public void CmdCreateProjectileAtPosition(Vector3 position, float damage)
    {
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 direction = (position - spawnPosition).normalized;

        if (direction == Vector3.zero) return;

        ArrowProjectile proj = Instantiate(projectile, spawnPosition, Quaternion.LookRotation(direction));
        proj.Init(playerLinks, 0, false, this, damage);
        SceneManager.MoveGameObjectToScene(proj.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(proj.gameObject);
        proj.StartFly(direction);
        RpcInit(proj.gameObject, damage);
        RpcPlayShotSound();
    }

    [Command] private void CmdCrossFade(string newAnim) => _hero.Animator.CrossFade(newAnim, 0.1f);

    [ClientRpc]
    protected void RpcInit(GameObject gameObject, float damage)
    {
        if (gameObject == null) return;

        ArrowProjectile proj = gameObject.GetComponent<ArrowProjectile>();
        if (proj != null) proj.Init(playerLinks, 0, false, this, damage);
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

    public override void LoadTargetData(TargetInfo targetInfo) => _targetPoint = targetInfo.Points[0];
}

