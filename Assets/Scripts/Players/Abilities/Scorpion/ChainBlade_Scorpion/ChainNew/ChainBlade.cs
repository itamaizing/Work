using System.Collections;
using UnityEngine;
using Mirror;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public static class Vector3Extensions
{
    public static bool IsFinite(this Vector3 vector)
    {
        return !(float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z) || float.IsInfinity(vector.x) || float.IsInfinity(vector.y) || float.IsInfinity(vector.z));
    }
}

public class ChainBlade : Skill,IComboParticipatingSkill
{
    [SerializeField] [Range(0, 100)] private float _minDamage = 3f;
    [SerializeField] [Range(0, 100)] private float _maxDamage = 5f;
    [SerializeField] private float _arrowYOffset = 1.5f;

    [SerializeField] private ChainArrow _chainArrowPrefab;
    [SerializeField] private HeroComponent _playerLinks;
    [SerializeField] private LineRenderer _pullLineRenderer;

    private Coroutine _pullCoroutine;
    private ChainArrow _currentChainArrowPrefab;
    private Vector3 _clickPoint = Vector3.positiveInfinity;
    private Animator _animator;
    public event Action<GameObject, Skill> OnDamaged;
    public event Action<Character> OnArrowHit;

    #region Const
    private const float MinDirectionSqrMagnitude = 0.01f;
    private const float TargetLineYOffset = 1.32f;
    private const float ChainArrowCastOffset = 0.5f;
    private const float SearchTargetInRadius = 1f;
    private const float BaseTimeDisappointment = 1f;
    private const float DisappointmentTimeOnFinalHit = 2f;
    #endregion

    private bool _needDestroyArrowAfterSpawn = false;

    private static readonly int chainBladeStart = Animator.StringToHash("ChainStart");
    private static readonly int chainBladeEnd = Animator.StringToHash("ChainEnd");
    private static readonly int chainBladeDestroy = Animator.StringToHash("ChainBladeDestroy");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => chainBladeStart;
    
    private float _pendingFireDamageBonus = 0f;
    private float _pendingScorchedSoulChance = 0f;

    protected override bool IsCanCast => Vector3.Distance(_clickPoint, transform.position) <= AreaInfo.CastLength && Targeting.NoObstacles(_clickPoint, transform.position, _obstacle);
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public float DamageRange => UnityEngine.Random.Range(_minDamage, _maxDamage);

    //private void OnDisable()
    //{
    //    OnSkillCanceled -= HandleSkillCanceled;
    //}

    //private void OnEnable()
    //{
    //    OnSkillCanceled += HandleSkillCanceled;
    //}

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    //private void HandleSkillCanceled()
    //{
    //    if (_hero?.Move != null)
    //    {
    //        Hero.Move.IsMoveBlocked = false;
    //        _clickPoint = Vector3.positiveInfinity;
    //        Hero.Move.StopLookAt();
    //    }

    //    _needDestroyArrowAfterSpawn = false;
    //    ChainBladeCastEnd(false);
    //    CmdDestroyChain();
    //}
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _clickPoint = targetInfo.Points[0];
    }
    
    public void AddFireBonus(float damagePercent, float scorchedChance)
    {
        _pendingFireDamageBonus += damagePercent;
        _pendingScorchedSoulChance += scorchedChance;
    }

    
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), SearchTargetInRadius);

                if (Targeting.GetTempTarget()?.Character != null)
                {
                    if (IsAllyTarget(Targeting.GetTempTarget()?.Character) || Targeting.GetTempTarget()?.Character == Hero) Targeting.ClearTempTarget();

                    else
                    {
                        float distance = Vector3.Distance(_hero.transform.position, targetPoint);

                        if (distance <= AreaInfo.Radius) targetPoint = Targeting.GetTempTarget().Character.transform.position;

                        else
                        {
                            targetPoint = Targeting.GetTempTarget().Character.transform.position;
                        }
                    }
                }

                else targetPoint = Targeting.GetMousePoint();
            }

            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(targetPoint);
        callbackDataSaved(targetInfo);
    }

    public void ChainBladeEnd()
    {
        if (_animator != null)
        {
            _animator.ResetTrigger(chainBladeStart);
            _animator.SetTrigger(chainBladeEnd);
        }
    }

    private void ChainBladeDestroy()
    {
        Hero.Move.StopLookAt();

        if (_animator != null)
        {
            _animator.ResetTrigger(chainBladeEnd);
            _animator.SetTrigger(chainBladeDestroy);
        }
    }


    protected override IEnumerator CastJob()
    {
        CmdSpawnChainArrow(_clickPoint);
        yield return null;
    }

    private IEnumerator PullTargetToPlayer(Character target, float duration)
    {
        Transform targetTransform = target.transform;
        Vector3 start = targetTransform.position;
        Vector3 end = Hero.transform.position + Hero.transform.forward * _arrowYOffset;
        var agent = target.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled) agent.enabled = false;

        float timer = 0f;
        _pullLineRenderer.enabled = true;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            targetTransform.position = Vector3.Lerp(start, end, t);

            _pullLineRenderer.SetPosition(0, transform.position + Vector3.up * _arrowYOffset);

            Vector3 targetPos = targetTransform.position;
            targetPos.y += TargetLineYOffset;
            _pullLineRenderer.SetPosition(1, targetPos);

            yield return null;
        }

        _pullLineRenderer.enabled = false;
        if (agent != null && !agent.enabled) agent.enabled = true;

        ChainBladeCastEnd(true);
    }

    protected override void ClearData()
    {
        _clickPoint = Vector3.positiveInfinity;
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
    }

    public void ChainBladeCast()
    {
        Hero.Move.StopMoveAndAnimationMove();
        AnimStartCastCoroutine();
        Vector3 direction = _clickPoint - Hero.transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude > MinDirectionSqrMagnitude && direction.IsFinite())
            Hero.Move.LookAtPosition(_clickPoint);
    }

    public void ChainBladeMoveBlock()
    {
        Hero.Move.IsMoveBlocked = true;
    }

    public void ChainBladeCastEnd(bool handleArrowHit)
    {
        if (handleArrowHit) Hero.Move.IsMoveBlocked = false;
        AnimCastEnded();
        ChainBladeDestroy();
    }

    [Server]
    private void HandleArrowHit(Character target, float pullDuration,float damage)
    {
        if (_currentChainArrowPrefab != null)
        {
            _currentChainArrowPrefab.OnHitTarget -= HandleArrowHit;
            _currentChainArrowPrefab.Cleanup();
            NetworkServer.Destroy(_currentChainArrowPrefab.gameObject);
            _currentChainArrowPrefab = null;
        }

        if (_pullCoroutine != null) StopCoroutine(_pullCoroutine);
        _pullCoroutine = StartCoroutine(PullTargetToPlayer(target, pullDuration));
        OnDamaged?.Invoke(target.gameObject, this);
        RpcHandleHitClient(target.netId, pullDuration,damage);
        AddBaseDisappointment(target);
    }

    private void AddBaseDisappointment(Character target)
    {
        float pullDistance = Vector3.Distance(gameObject.transform.position, target.transform.position);

        if (pullDistance > 1f)
        {
            var duration = BaseTimeDisappointment + GetDisappointmentDuration(target);
            target.CharacterState.AddState(States.DisappointmentState, duration, 0, _hero.gameObject, nameof(ChainBlade));
        }
    }

    public void OnFinalComboSkill(GameObject target)
    {
        if (target == null) return;
        if (!target.TryGetComponent(out Character character)) return;

        float duration = DisappointmentTimeOnFinalHit + GetDisappointmentDuration(character);

        character.CharacterState.AddState(States.DisappointmentState, duration, 0f, _hero.gameObject,
            nameof(ChainBlade));
    }

    public void OnTargetHasComboPoint(GameObject target, float comboPoints)
    {
        if (target == null) return;
        if (!target.TryGetComponent(out Character character)) return;
        
        float duration = comboPoints + GetDisappointmentDuration(character);

        if (duration > 0)
        {
            character.CharacterState.AddState(States.DisappointmentState, duration, 0, _hero.gameObject, nameof(ChainBlade));
        }
    }

    private float GetDisappointmentDuration(Character character)
    {
        if (character.CharacterState.GetState(States.DisappointmentState) != null)
        {
            return character.CharacterState.GetState(States.DisappointmentState).RemainingDuration;
        }
        return 0;
    }

    [Command]
    private void CmdSpawnChainArrow(Vector3 clickPoint)
    {
        Vector3 direction = (clickPoint - transform.position).normalized;
        Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
        Vector3 targetPoint = transform.position + flatDirection * (AreaInfo.CastLength - ChainArrowCastOffset);
        targetPoint.y = transform.position.y;
        Vector3 spawnPosition = transform.position + Vector3.up * _arrowYOffset;
        var arrow = Instantiate(_chainArrowPrefab, spawnPosition, Quaternion.identity);
        if (_currentChainArrowPrefab != null) Destroy(_currentChainArrowPrefab.gameObject);
        _currentChainArrowPrefab = arrow;

        _currentChainArrowPrefab = arrow;

        if (_needDestroyArrowAfterSpawn)
        {
            RpcResetChain();
            RpcDestroyChain(_currentChainArrowPrefab.gameObject);
            NetworkServer.Destroy(_currentChainArrowPrefab.gameObject);
            _currentChainArrowPrefab = null;
            _needDestroyArrowAfterSpawn = false;
            return;
        }
        arrow.Init(_playerLinks, 0, false, this);

        arrow.OnHitTarget += HandleArrowHit;

        NetworkServer.Spawn(arrow.gameObject);
        //SceneManager.MoveGameObjectToScene(arrow.gameObject, _hero.NetworkSettings.MyRoom);

        arrow.InitArrow(targetPoint, transform, AreaInfo.CastLength, DamageRange);
        RpcInitArrow(arrow.gameObject, targetPoint);
    }

    [ClientRpc]
    private void RpcHandleHitClient(uint targetId, float duration,float damage)
    {
        var obj = NetworkClient.spawned[targetId].gameObject;
        var target = obj.GetComponent<Character>();
        OnArrowHit.Invoke(target);
        if (_pullCoroutine != null) StopCoroutine(_pullCoroutine);
        _pullCoroutine = StartCoroutine(PullTargetToPlayer(target, duration));
        
        float bonus = _pendingFireDamageBonus;
        float scorchedChance = _pendingScorchedSoulChance;
        _pendingFireDamageBonus = 0f;
        _pendingScorchedSoulChance = 0f;

        Damage additionalDamage = new Damage
        {
            Value = damage * bonus,
            Type = Info.DamageType,
            School = Schools.Fire
        };

        if (additionalDamage.Value > 0)
        {
            CmdAdditionalAttack(additionalDamage, target.gameObject, scorchedChance);
        }
    }
    
    [Command]
    private void CmdAdditionalAttack(Damage damage, GameObject target, float scorchedChance)
    {
        if (target == null) return;
        var damageable = target.GetComponent<IDamageable>();
        if (damageable == null) return;

        bool result = damageable.TryTakeDamage(ref damage, this);
        if (result && damageable is Character character)
        {
            if (scorchedChance > 0f && Random.Range(0f, 100f) <= scorchedChance)
                character.CharacterState.AddState(States.ScorchedSoul, 5f, 0f, _hero.gameObject, name);
        }
    }

    [ClientRpc]
    private void RpcDestroyChain(GameObject arrowObj)
    {
        if (arrowObj != null) Destroy(arrowObj);
    }

    [ClientRpc]
    private void RpcResetChain()
    {
        AnimCastEnded();
        ChainBladeDestroy();
        Hero.Move.IsMoveBlocked = false;
    }


    [ClientRpc]
    private void RpcInitArrow(GameObject arrowObj, Vector3 targetPoint)
    {
        if (arrowObj == null) return;

        var arrow = arrowObj.GetComponent<ChainArrow>();
        arrow.Init(_playerLinks, 0, false, this);
        arrow.InitArrow(targetPoint, transform, AreaInfo.CastLength, DamageRange);
        _currentChainArrowPrefab = arrow;
    }
}
