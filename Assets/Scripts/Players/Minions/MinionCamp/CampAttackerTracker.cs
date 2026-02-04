using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CampAttackerTracker : NetworkBehaviour
{
    private HashSet<Character> _attackers = new();
    private Dictionary<Character, Coroutine> _attackerTimers = new();
    private float _attackerTimeoutSeconds = 5f;

    public IReadOnlyCollection<Character> Attackers => _attackers;

    public void Initialize()
    {
    }

    public void AddAttacker(GameObject attacker)
    {
        if (attacker == null) return;

        if (!attacker.TryGetComponent(out Character heroComponent))
            return;

        if (!isServer)
        {
            CmdRefreshAttackersCount(attacker);
            return;
        }

        if (_attackers.Contains(heroComponent))
        {
            if (_attackerTimers.TryGetValue(heroComponent, out var existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
                _attackerTimers.Remove(heroComponent);
            }
        }
        else
        {
            _attackers.Add(heroComponent);
            heroComponent.Health.HealTaked += OnHealTaken;
            heroComponent.CharacterState.OnStateAddFromPerson += OnBuffApplied;
        }

        _attackerTimers[heroComponent] = StartCoroutine(RemoveAttackerAfterDelay(heroComponent, _attackerTimeoutSeconds));
    }

    private void OnHealTaken(float amount, Skill skill, string skillName)
    {
        if (skill?.Hero?.gameObject != null)
        {
            AddAttacker(skill.Hero.gameObject);
        }
    }

    private void OnBuffApplied(GameObject whoAddBuff)
    {
        if (whoAddBuff != null)
        {
            AddAttacker(whoAddBuff);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdRefreshAttackersCount(GameObject target)
    {
        AddAttacker(target);
    }

    private IEnumerator RemoveAttackerAfterDelay(Character hero, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (hero == null) yield break;

        RemoveAttacker(hero);
    }

    private void RemoveAttacker(Character hero)
    {
        if (hero == null) return;

        hero.Health.HealTaked -= OnHealTaken;
        hero.CharacterState.OnStateAddFromPerson -= OnBuffApplied;

        _attackers.Remove(hero);
        _attackerTimers.Remove(hero);
    }
    
    public void StopAllTimers()
    {
        foreach (var timer in _attackerTimers.Values)
        {
            if (timer != null)
            {
                StopCoroutine(timer);
            }
        }
        
        _attackerTimers.Clear();
    }

    public void ClearAllAttackers()
    {
        foreach (var timer in _attackerTimers.Values)
        {
            if (timer != null)
            {
                StopCoroutine(timer);
            }
        }

        foreach (var attacker in _attackers)
        {
            if (attacker != null)
            {
                attacker.Health.HealTaked -= OnHealTaken;
                attacker.CharacterState.OnStateAddFromPerson -= OnBuffApplied;
            }
        }

        _attackerTimers.Clear();
        _attackers.Clear();
    }

    public Character FindHeroByConnection(NetworkConnectionToClient conn)
    {
        foreach (var hero in _attackers)
        {
            if (hero?.netIdentity?.connectionToClient == conn)
                return hero;
        }
        return null;
    }
}
