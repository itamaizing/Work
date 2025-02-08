using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Trap : Projectiles
{
    private HeroComponent _owner;
    private Vector3 _startPosition;
    private Vector3 _endPosition;

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

    private void SetupTrapShape()
    {
        Vector3 direction = _endPosition - _startPosition;
        float distance = direction.magnitude;

        transform.position = _startPosition + direction;
        transform.localScale = new Vector3(1, 1, distance);
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
