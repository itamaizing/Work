using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Trap : Projectiles
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform pointTrapRight;
    [SerializeField] private Transform pointTrapLeft;
    [SerializeField] private List<GameObject> hitBoxes;
    [SerializeField] private Material ropeMaterial;
    [SerializeField] private List<GameObject> ropes;

    private readonly List<BoxCollider> _boxes = new();
    private readonly List<Vector3> _baseSizes = new();

    private HeroComponent _owner;
    private Vector3 _startPosition;
    private Vector3 _endPosition;

    private bool _secondFixed;
    private const float YFix = 0.2f;

    private List<Character> _charactersInTrigger = new List<Character>();
    private bool _hasSnapped = false;

    private void Awake()
    {
        foreach (var hitBox in hitBoxes)
        {
            if (hitBox == null) continue;

            if (hitBox.TryGetComponent(out BoxCollider boxCollider))
            {
                _boxes.Add(boxCollider);
                _baseSizes.Add(boxCollider.size);
            }

            hitBox.SetActive(false);
        }
    }

    public void Init(HeroComponent owner, Skill skill, Vector3 startPosition, Vector3 endPosition)
    {
        _owner = owner;
        _skill = skill;
        _startPosition = startPosition;
        _endPosition = endPosition;
        _initialized = true;
    }

    public void FixSecondPoint()
    {
        _secondFixed = true;
        foreach (var hitBox in hitBoxes)
            hitBox?.SetActive(true);
    }

    private void SetLine(Vector3 a, Vector3 b)
    {
        a.y = b.y = YFix;
        lineRenderer.SetPosition(0, a);
        lineRenderer.SetPosition(1, b);
    }
    

    public void Finalise()
    {
        SetLine(pointTrapRight.position, pointTrapLeft.position);
        FixSecondPoint();
    }
    
    [Server]
    public void HandleHit(Collider other)
    {
        if (!_initialized) return;
        if (!other.TryGetComponent<Character>(out var target)) return;
        if (_charactersInTrigger.Contains(target)) return;

        _charactersInTrigger.Add(target);

        if (target.TryGetComponent<CharacterState>(out CharacterState state))
        {
            state.AddState(States.Bound, 99f, 0, _owner.gameObject, _skill.Name);

            var boundState = state.GetState(States.Bound) as Bound;
            boundState?.SetTrapObject(this.gameObject);

            if (TryGetComponent<TrapStateLife>(out var trapStateLife))
                trapStateLife.Init(target.gameObject);
            else
                gameObject.AddComponent<TrapStateLife>().Init(target.gameObject);

            ConfigureHitboxForTarget(target);
            RpcHideGroundVisuals();
            RpcConfigureHitboxForTarget(target.gameObject);
        }
    }
    
    [ClientRpc]
    private void RpcConfigureHitboxForTarget(GameObject targetGo)
    {
        if (targetGo == null) return;

        foreach (var hitBox in hitBoxes)
        {
            if (hitBox != null) hitBox.SetActive(false);
        }

        if (hitBoxes != null && hitBoxes.Count > 0 && hitBoxes[0] != null)
        {
            GameObject mainHitbox = hitBoxes[0];
            mainHitbox.transform.position = targetGo.transform.position + Vector3.up * 1f;
            mainHitbox.transform.rotation = Quaternion.identity;
            mainHitbox.SetActive(true);

            if (mainHitbox.TryGetComponent<HitBoxTrap>(out var hitBoxTrap))
            {
                hitBoxTrap.SetHit(true);
            }
        }
    }
    
    public void ResetPreview()
    {
        SetLine(pointTrapRight.position, pointTrapLeft.position);

        pointTrapLeft.gameObject.SetActive(true);
        foreach (var hitBox in hitBoxes) hitBox?.SetActive(false);

        _secondFixed = false;
    }
    
    [ClientRpc]
    private void RpcTryShowBar()
    {
        if (TryGetComponent<ObjectBar>(out var bar))
        {
            Debug.LogError("TryShowBar");
                    
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
        foreach (var hitBox in hitBoxes)
        {
            if (hitBox != null) hitBox.SetActive(false);
        }

        if (hitBoxes != null && hitBoxes.Count > 0 && hitBoxes[0] != null)
        {
            GameObject mainHitbox = hitBoxes[0];
            mainHitbox.transform.position = target.transform.position + Vector3.up * 1f;
            mainHitbox.transform.rotation = Quaternion.identity;
            mainHitbox.SetActive(true);

            if (mainHitbox.TryGetComponent<HitBoxTrap>(out var hitBoxTrap))
            {
                hitBoxTrap.SetHit(true);
            }

            if (mainHitbox.TryGetComponent<BoxCollider>(out var boxCollider))
            {
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector3(1.5f, 2f, 1.5f);
            }
        }
    }
}