using System.Collections;
using UnityEngine;
using Mirror;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

public static class Vector3Extensions
{
    public static bool IsFinite(this Vector3 vector)
    {
        return !(float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z) || float.IsInfinity(vector.x) || float.IsInfinity(vector.y) || float.IsInfinity(vector.z));
    }
}

public class ChainBlade : Skill
{
    [SerializeField] [Range(0, 100)] private float _minDamage = 3f;
    [SerializeField] [Range(0, 100)] private float _maxDamage = 5f;
    [SerializeField] private float _arrowYOffset = 1.5f;
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;

    [SerializeField] private ChainArrow chainArrowPrefab;
    [SerializeField] private HeroComponent playerLinks;
    [SerializeField] private LineRenderer pullLineRenderer;

    private Coroutine _pullCoroutine;
    private ChainArrow _chainArrowPrefab;
    private Vector3 _clickPoint = Vector3.positiveInfinity;
    private Animator _animator;

    private bool _needDestroyArrowAfterSpawn = false;

    private static readonly int chainBladeStart = Animator.StringToHash("ChainStart");
    private static readonly int chainBladeEnd = Animator.StringToHash("ChainEnd");
    private static readonly int chainBladeDestroy = Animator.StringToHash("ChainBladeDestroy");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => chainBladeStart;

    protected override bool IsCanCast => Vector3.Distance(_clickPoint, transform.position) <= CastLength && NoObstacles(_clickPoint, transform.position, _obstacle);

    public float DamageRange => UnityEngine.Random.Range(_minDamage, _maxDamage);
    public PassiveCombo_Scorpion ComboCounter { get => _comboCounter; set => _comboCounter = value; }
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

    private void HandleSkillCanceled()
    {
        if (_hero?.Move != null)
        {
            Hero.Move.IsMoveBlocked = false;
            _clickPoint = Vector3.positiveInfinity;
            Hero.Move.StopLookAt();
        }

        _needDestroyArrowAfterSpawn = false;
        ChainBladeCastEnd(false);
        CmdDestroyChain();
    }
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _clickPoint = targetInfo.Points[0];
    }
    
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            if (GetMouseButton)
            {
                if (GetTargetCharacter() != null)
                {
                    float distance = Vector3.Distance(_hero.transform.position, _clickPoint);

                    if (distance <= Radius) _clickPoint = GetTargetCharacter().transform.position;

                    else
                    {
                        //_target = GetTarget().character;
                        _clickPoint = GetTargetCharacter().transform.position;
                    }
                }

                else _clickPoint = GetMousePoint();
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
        pullLineRenderer.enabled = true;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            targetTransform.position = Vector3.Lerp(start, end, t);

            pullLineRenderer.SetPosition(0, transform.position + Vector3.up * _arrowYOffset);

            Vector3 targetPos = targetTransform.position;
            targetPos.y += 1.32f;
            pullLineRenderer.SetPosition(1, targetPos);

            yield return null;
        }

        pullLineRenderer.enabled = false;
        if (agent != null && !agent.enabled) agent.enabled = true;

        ChainBladeCastEnd(true);
    }

    protected override void ClearData()
    {
        _clickPoint = Vector3.positiveInfinity;

        ClearTarget();
        //_target = null;
    }

    public void ChainBladeCast()
    {
        Hero.Move.StopMoveAndAnimationMove();
        AnimStartCastCoroutine();
        Vector3 direction = _clickPoint - Hero.transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.01f && direction.IsFinite())
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
    private void HandleArrowHit(Character target, float pullDuration)
    {
        Debug.Log("HIT handling on server");

        if (_chainArrowPrefab != null)
        {
            _chainArrowPrefab.OnHitTarget -= HandleArrowHit;
            _chainArrowPrefab.Cleanup();
            NetworkServer.Destroy(_chainArrowPrefab.gameObject);
            _chainArrowPrefab = null;
        }

        _pullCoroutine = StartCoroutine(PullTargetToPlayer(target, pullDuration));

        RpcHandleHitClient(target.netId, pullDuration);
    }

    [Command]
    private void CmdDestroyChain()
    {
        if (_chainArrowPrefab != null)
        {
            RpcDestroyChain(_chainArrowPrefab.gameObject);
            NetworkServer.Destroy(_chainArrowPrefab.gameObject);
            _chainArrowPrefab = null;
        }
        else _needDestroyArrowAfterSpawn = true;
    }

    [Command]
    private void CmdSpawnChainArrow(Vector3 clickPoint)
    {

        Vector3 direction = (clickPoint - transform.position).normalized;
        Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
        Vector3 targetPoint = transform.position + flatDirection * (CastLength - 0.5f);
        targetPoint.y = transform.position.y;
        Vector3 spawnPosition = transform.position + Vector3.up * _arrowYOffset;
        var arrow = Instantiate(chainArrowPrefab, spawnPosition, Quaternion.identity);
        if (_chainArrowPrefab != null) Destroy(_chainArrowPrefab.gameObject);
        _chainArrowPrefab = arrow;

        _chainArrowPrefab = arrow;

        if (_needDestroyArrowAfterSpawn)
        {
            RpcResetChain();
            RpcDestroyChain(_chainArrowPrefab.gameObject);
            NetworkServer.Destroy(_chainArrowPrefab.gameObject);
            _chainArrowPrefab = null;
            _needDestroyArrowAfterSpawn = false;
            return;
        }
        arrow.Init(playerLinks, 0, false, this);

        arrow.OnHitTarget += HandleArrowHit;

        NetworkServer.Spawn(arrow.gameObject);
        SceneManager.MoveGameObjectToScene(arrow.gameObject, _hero.NetworkSettings.MyRoom);

        arrow.InitArrow(targetPoint, transform, CastLength, DamageRange);
        RpcInitArrow(arrow.gameObject, targetPoint);
    }

    [ClientRpc]
    private void RpcHandleHitClient(uint targetId, float duration)
    {
        var obj = NetworkClient.spawned[targetId].gameObject;
        var target = obj.GetComponent<Character>();

        if (_pullCoroutine != null) StopCoroutine(_pullCoroutine);
        _pullCoroutine = StartCoroutine(PullTargetToPlayer(target, duration));
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
        arrow.Init(playerLinks, 0, false, this);
        arrow.InitArrow(targetPoint, transform, CastLength, DamageRange);
        _chainArrowPrefab = arrow;
    }
}
