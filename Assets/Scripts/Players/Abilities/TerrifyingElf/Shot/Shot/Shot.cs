using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Shot : AutoAttackSkill
{
    [SerializeField] private ArrowProjectile projectile;
    [SerializeField] private HeroComponent playerLinks;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Ghost ghostSkill;
    [SerializeField] private AudioClip audioClip;

    private const string _startAnimTrigger = "ShotCastDelayStartAnimTrigger";
    private const string _middleAnimTrigger = "ShotCastDelayMiddleAnimTrigger";
    private const string _endAnimTrigger = "ShotCastDelayEndAnimTrigger";

    private bool FirstShot;
    private AudioSource _audioSource;
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private bool _isDelayActive;
    private int _consecutiveShots = 0;

    protected override int AnimTriggerAutoAttack => 0;
    protected override int AnimTriggerCastDelay => Animator.StringToHash(_startAnimTrigger);
    protected override bool IsCanCast => true;

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

    protected override void CastAction()
    {
        if (_target != null && !_disactive) return;
    }

    protected override IEnumerator PrepareJob()
    {
        Hero.Animator.speed = Hero.Animator.speed / AttackDelay;

        while (float.IsPositiveInfinity(_targetPoint.x) && !Disactive)
        {
            if (GetMouseButton && IsCanCast)
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
            Hero.Move.CanMove = true;
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

                AnimatorStateInfo stateInfo = _hero.Animator.GetCurrentAnimatorStateInfo(0);
                float animationLength = stateInfo.length;

                if (!FirstShot)
                {
                    _hero.Animator.SetTrigger(Animator.StringToHash(_startAnimTrigger));
                    _hero.NetworkAnimator.SetTrigger(Animator.StringToHash(_startAnimTrigger));

                    yield return new WaitForSeconds(animationLength / 2.25f);
                }

                else
                {  
                    _hero.Animator.Play(_middleAnimTrigger, 0, 0);
                    CmdAnimatorPlay(_middleAnimTrigger);

                    yield return new WaitForSeconds(animationLength);
                }


                if (!IsPointInRadius(Radius, _targetPoint))
                {
                    ClearData();
                    yield break;
                }

                CmdCreateProjectileAtPosition(_targetPoint);
                _isDelayActive = true;

                ProcessGhostCooldownReduction();

                FirstShot = true;
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

            _hero.Animator.SetTrigger(Animator.StringToHash(_startAnimTrigger));
            _hero.NetworkAnimator.SetTrigger(Animator.StringToHash(_startAnimTrigger));

            AnimatorStateInfo stateInfo = _hero.Animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;

            yield return new WaitForSeconds(animationLength / 2.25f);

            if (!IsPointInRadius(Radius, _targetPoint))
            {
                ClearData();
                yield break;
            }

            CmdCreateProjectileAtPosition(_targetPoint);
            ProcessGhostCooldownReduction();

            HandleSkillCanceled();
            ClearData();
            yield break;
        }
    }

    private void ProcessGhostCooldownReduction()
    {
        if (!ghostSkill.CooldownGhostShotActive) return;

        _consecutiveShots++;

        if (_consecutiveShots >= 3)
        {
            ghostSkill.ReductionCooldownCharges(1);
            _consecutiveShots = 0;
        }
    }

    private void HandleSkillCanceled()
    {
        if (_hero != null && _hero.Move != null)
        {
            Hero.Move.CanMove = true;
            Hero.Animator.speed = 1;
            Hero.Move.StopLookAt();
        }

        FirstShot = false;
        _consecutiveShots = 0;

        if (IsAutoattackMode) WorkAnimator(_middleAnimTrigger, _endAnimTrigger);
        else WorkAnimator(_startAnimTrigger, _endAnimTrigger);
    }

    private void WorkAnimator(string oldAnim, string newAnim)
    {
        _hero.Animator.ResetTrigger(Animator.StringToHash(oldAnim));
        _hero.NetworkAnimator.ResetTrigger(Animator.StringToHash(oldAnim));

        _hero.Animator.CrossFade(newAnim, 0.1f);
        CmdCrossFade(newAnim);
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

    //[Command]
    //protected void CmdCreateProjectile(Transform target)
    //{
    //    if (target == null) return;

    //    Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
    //    Vector3 direction = (target.position - spawnPosition).normalized;

    //    ArrowProjectile projectile = Instantiate(this.projectile, spawnPosition, Quaternion.LookRotation(direction));
    //    projectile.Init(playerLinks, 0, false, this);
    //    SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
    //    NetworkServer.Spawn(projectile.gameObject);
    //    projectile.StartFly(direction);
    //    RpcInit(projectile.gameObject);
    //}

    [Command]
    protected void CmdCreateProjectileAtPosition(Vector3 position)
    {
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 direction = (position - spawnPosition).normalized;

        ArrowProjectile projectile = Instantiate(this.projectile, spawnPosition, Quaternion.LookRotation(direction));
        projectile.Init(playerLinks, 0, false, this);
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(projectile.gameObject);
        projectile.StartFly(direction);
        RpcInit(projectile.gameObject);
        RpcPlayShotSound();
    }

    [Command]
    private void CmdCrossFade(string newAnim)
    {
        _hero.Animator.CrossFade(newAnim, 0.1f);
    }

    [Command]
    private void CmdAnimatorPlay(string newAnim)
    {
        _hero.Animator.Play(newAnim, 0, 0);
    }


    [ClientRpc]
    protected void RpcInit(GameObject gameObject)
    {
        if (gameObject == null) return;

        ArrowProjectile projectile = gameObject.GetComponent<ArrowProjectile>();
        if (projectile != null)
        {
            projectile.Init(playerLinks, 0, false, this);
        }
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && audioClip != null) _audioSource.PlayOneShot(audioClip);
    }

    protected override void ClearData()
    {
        base.ClearData();
        _targetPoint = Vector3.positiveInfinity;
        _isDelayActive = false;

        _consecutiveShots = 0;
    }
}
