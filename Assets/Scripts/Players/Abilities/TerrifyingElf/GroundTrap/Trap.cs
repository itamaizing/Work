using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Trap : Projectiles
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform pointTrapRight;
    [SerializeField] private Transform pointTrapLeft;
    [SerializeField] private BoxCollider boxColliderTrap;

    private HeroComponent _owner;
    private Vector3 _startPosition;
    private Vector3 _endPosition;

    private bool _secondFixed;
    private const float YFix = 0.2f;

    private List<Character> _charactersInTrigger = new List<Character>();

    public void Init(HeroComponent owner, Skill skill, Vector3 startPosition, Vector3 endPosition)
    {
        _owner = owner;
        _skill = skill;
        _startPosition = startPosition;
        _endPosition = endPosition;
        _initialized = true;

        SetupTrapShape();
    }

    public void ResetPreview()
    {
        lineRenderer.positionCount = 2;
        SetLine(pointTrapRight.position, pointTrapRight.position);
        boxColliderTrap.enabled = false;
        pointTrapLeft.gameObject.SetActive(false);
        _secondFixed = false;
    }

    public void UpdateSecondPoint(Vector3 worldPos)
    {
        if (_secondFixed) return;

        worldPos.y = pointTrapRight.position.y;
        pointTrapLeft.position = worldPos;
        SetLine(pointTrapRight.position, pointTrapLeft.position);
    }

    public void FixSecondPoint()
    {
        _secondFixed = true;
        boxColliderTrap.enabled = true;
    }

    private void SetLine(Vector3 a, Vector3 b)
    {
        a.y = b.y = YFix;
        lineRenderer.SetPosition(0, a);
        lineRenderer.SetPosition(1, b);
    }

    private void SetupTrapShape()
    {
        Vector3 dir = _endPosition - _startPosition;
        float len = dir.magnitude;

        transform.position = _startPosition + dir * 0.5f;

        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(scale.x, len, scale.z);
    }

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (!_initialized) return;

        if (other.TryGetComponent<Character>(out Character target))
        {
            if (!_charactersInTrigger.Contains(target))
            {
                _charactersInTrigger.Add(target);

                if (target.TryGetComponent<CharacterState>(out CharacterState characterState))
                {
                    characterState.AddState(States.Stun, 999f, 0, _owner.gameObject, _skill.name);
                }
            }
        }
    }

    [Server]
    private void OnDestroy()
    {
        foreach (var character in _charactersInTrigger)
        {
            if (character.TryGetComponent<CharacterState>(out CharacterState characterState))
            {
                characterState.RemoveState(States.Stun);
            }
        }

        _charactersInTrigger.Clear();
    }
}
