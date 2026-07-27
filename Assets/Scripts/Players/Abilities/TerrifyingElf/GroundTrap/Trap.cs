using System;
using Mirror;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Trap : Projectiles
{
    [Header("Visual Components")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform pointTrapRight;
    [SerializeField] private Transform pointTrapLeft;
    [SerializeField] private Material ropeMaterial;
    [SerializeField] private List<GameObject> ropes;

    [Header("Components")]
    [SerializeField] private Collider mainCollider; 
    private ObjectHealth _objectHealth;

    private readonly List<BoxCollider> _boxes = new();
    private readonly List<Vector3> _baseSizes = new();

    private CharacterState _currentState;
    public HeroComponent _owner;

    private bool _secondFixed;
    private const float YFix = 0.2f;

    private List<Character> _charactersInTrigger = new List<Character>();
    private bool _isHit = false; 

    [SyncVar] private int _ownerTeamIndex = -1;

    [SyncVar(hook = nameof(OnTargetCaught))]
    private GameObject _caughtTarget;

    private IDamageable _caughtTargetDamageable;

    private void Awake()
    {
        _objectHealth = GetComponentInChildren<ObjectHealth>();
        if (mainCollider == null) mainCollider = GetComponent<Collider>();

        _boxes.Clear();
        _baseSizes.Clear();
        if (mainCollider is BoxCollider boxCollider)
        {
            _boxes.Add(boxCollider);
            _baseSizes.Add(boxCollider.size);
        }
    }

    public void Init(HeroComponent owner, Skill skill, Vector3 startPosition, Vector3 endPosition)
    {
        _owner = owner;
        _skill = skill;
        _initialized = true;

        if (_owner != null && _owner.NetworkSettings != null)
        {
            _ownerTeamIndex = _owner.NetworkSettings.TeamIndex;
        }
    }

    public void FixSecondPoint()
    {
        _secondFixed = true;
        if (mainCollider != null) 
            mainCollider.enabled = true;
    }

    private void SetLine(Vector3 a, Vector3 b)
    {
        a.y = b.y = YFix;
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, a);
            lineRenderer.SetPosition(1, b);
        }
    }

    public void Finalise()
    {
        SetLine(pointTrapRight.position, pointTrapLeft.position);
        FixSecondPoint();
    }

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (!_initialized || _isHit) return;
        HandleHit(other);
    }

    [Server]
    public void HandleHit(Collider other)
    {
        if (!other.TryGetComponent<Character>(out var target)) return;
        if (_charactersInTrigger.Contains(target)) return;

        if (_ownerTeamIndex != -1 && target.NetworkSettings.TeamIndex == _ownerTeamIndex)
        {
            return;
        }

        _charactersInTrigger.Add(target);

        if (target.TryGetComponent<CharacterState>(out CharacterState state))
        {
            _isHit = true; 
            _caughtTarget = target.gameObject;
            _caughtTargetDamageable = target.GetComponent<IDamageable>();

            state.AddState(States.Bound, 99f, 0, _owner.gameObject, _skill.Name);

            var boundState = state.GetState(States.Bound) as Bound;
            boundState?.SetTrapObject(this.gameObject);

            if (TryGetComponent<TrapStateLife>(out var trapStateLife))
            {
                trapStateLife.Init(target.gameObject);
            }

            ConfigureHitboxForTarget(target);
            RpcHideGroundVisuals();
            RpcConfigureHitboxForTarget(target.gameObject);
        }
    }

    private void OnTargetCaught(GameObject oldTarget, GameObject newTarget)
    {
        if (newTarget == null) return;

        transform.SetParent(newTarget.transform);
        transform.localPosition = new Vector3(0f, 1f, 0f);
        transform.localRotation = Quaternion.identity;
        transform.parent = null;
    }

    public void ShowPhantomValue(Damage phantomValue)
    {
        if (_caughtTargetDamageable != null)
        {
            _caughtTargetDamageable.ShowPhantomValue(phantomValue);
            return;
        }
        if (_objectHealth != null) _objectHealth.ShowPhantomValue(phantomValue);
    }

    [ClientRpc]
    private void RpcConfigureHitboxForTarget(GameObject targetGo)
    {
        if (targetGo == null) return;

        if (TryGetComponent<TrapStateLife>(out var trapStateLife))
            trapStateLife.Init(targetGo);

        transform.position = targetGo.transform.position + Vector3.up * 1f;
        transform.rotation = Quaternion.identity;

        if (mainCollider is BoxCollider boxCollider)
        {
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(1.5f, 2f, 1.5f);
        }

        _isHit = true;
        RpcTryShowBar();
    }

    public void ResetPreview()
    {
        SetLine(pointTrapRight.position, pointTrapLeft.position);
        pointTrapLeft.gameObject.SetActive(true);
        if (mainCollider != null) mainCollider.enabled = false;
        _secondFixed = false;
    }

    private void RpcTryShowBar()
    {
        if (TryGetComponent<ObjectBar>(out var bar))
        {
            bar.ShowHealthBar();
        }
    }

    public void UpdateLinePreview() => SetLine(pointTrapRight.position, pointTrapLeft.position);

    [ClientRpc]
    private void RpcHideGroundVisuals()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
        if (pointTrapRight != null) pointTrapRight.gameObject.SetActive(false);
        if (pointTrapLeft != null) pointTrapLeft.gameObject.SetActive(false);

        if (ropes != null)
        {
            foreach (var rope in ropes)
            {
                if (rope != null) rope.SetActive(false);
            }
        }
    }

    private void ConfigureHitboxForTarget(Character target)
    {
        transform.position = target.transform.position + Vector3.up * 1f;
        transform.rotation = Quaternion.identity;

        if (mainCollider is BoxCollider boxCollider)
        {
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(1.5f, 2f, 1.5f);
        }

        transform.SetParent(target.transform);
        transform.localPosition = new Vector3(0f, 1f, 0f);
        transform.localRotation = Quaternion.identity;
        transform.parent = null;
    }
}