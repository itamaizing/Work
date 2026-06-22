// IcePuddleObject.cs — полная замена

using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class IcePuddleObject : Projectiles
{
    [FormerlySerializedAs("healthPlayer")] private Health _healthComponent;

    [SerializeField] private DecalProjector decalProjector;

    private float _timeToDestroy   = 0;
    private float _damageToExit    = 0;
    private float _curEvade        = 0;
    private float _spawnTime;

    private bool _talentEvadeDadBoost    = false;
    private bool _talentFrostingFrozen   = false;
    private bool _iceDeathInIcePudleTalent = false;

    private bool  _deepColdTalent      = false;
    private const float DeepColdDamageToExit = 30f;

    private UserNetworkSettings _dadSettings;
    private readonly List<Character>                  _enemiesInZone      = new();
    private readonly Dictionary<Character, Coroutine> _frostingCoroutines = new();
    private readonly WaitForSeconds _waitApplyDelay = new WaitForSeconds(0.7f);
    private readonly WaitForSeconds _waitShort      = new WaitForSeconds(0.1f);

    private const float FrostEnergyCoolingBonusPerStack  = 1f;
    private const float FrostEnergyFrostingBonusPerStack = 5f;
    private const float FrostEnergyFrozenBonusPerStack   = 10f;

    public DecalProjector Decal { get => decalProjector; set => decalProjector = value; }

    private void OnDisable()
    {
        if (!isServer) return;

        foreach (var coroutine in _frostingCoroutines.Values)
            if (coroutine != null) StopCoroutine(coroutine);

        _frostingCoroutines.Clear();
        _enemiesInZone.Clear();
    }

    public override void Init(Character dad, float timeToDestroy, bool lastHit, Skill skill)
    {
        _dad          = dad;
        _skill        = skill;
        _initialized  = true;
        _lastHit      = lastHit;
        _healthComponent = _dad.Health;
        _timeToDestroy   = timeToDestroy;
        _spawnTime       = Time.time;

        _dadSettings = _dad?.GetComponent<UserNetworkSettings>();

        if (_lastHit) transform.localScale = Vector3.one * 1.7f;

        StartCoroutine(DestroyPuddle());
    }

    public void SetTalents(bool talentEvadeDadBoost, bool talentFrostingFrozen)
    {
        _talentEvadeDadBoost  = talentEvadeDadBoost;
        _talentFrostingFrozen = talentFrostingFrozen;
    }

    public void IceDeathInIcePudleTalentActive(bool value) => _iceDeathInIcePudleTalent = value;

    public void SetDeepColdTalent(bool value) => _deepColdTalent = value;

    private float GetDamageToExit() => _deepColdTalent ? DeepColdDamageToExit : 0f;

    private bool IsEnemy(GameObject target)
    {
        if (_dad == null) return IsEnemyByLayer(target);

        if (!_dadSettings || !target.TryGetComponent(out UserNetworkSettings targetSettings))
            return IsEnemyByLayer(target);

        if (!IsTeamAssigned(_dadSettings) || !IsTeamAssigned(targetSettings))
            return IsEnemyByLayer(target);

        return _dadSettings.TeamIndex != targetSettings.TeamIndex;
    }

    private bool IsTeamAssigned(UserNetworkSettings s) => s.TeamIndex != 0;

    private bool IsEnemyByLayer(GameObject target) =>
        ((1 << target.layer) & _skill.Targeting.Layer) != 0;

    private void Start() => _spriteRenderer.DOFade(1, 1);

    [Server]
    private void OnTriggerExit(Collider collision)
    {
        if (!collision.TryGetComponent<Character>(out var target)) return;
        if (!_enemiesInZone.Contains(target)) return;

        _enemiesInZone.Remove(target);

        if (_talentEvadeDadBoost && _enemiesInZone.Count == 0)
        {
            SetEvade(_dad.gameObject, -_curEvade);
            _curEvade = 0;
        }

        if (_frostingCoroutines.TryGetValue(target, out var routine))
        {
            StopCoroutine(routine);
            _frostingCoroutines.Remove(target);
        }
    }

    [Server]
    private void OnTriggerEnter(Collider collision)
    {
        if (!_initialized || !collision.TryGetComponent<Character>(out var target)) return;
        if (target == _dad) return;
        if (!IsEnemy(target.gameObject)) return;
        if (_enemiesInZone.Contains(target)) return;

        _enemiesInZone.Add(target);

        if (_talentFrostingFrozen && target.CharacterState.CheckForState(States.Frosting))
            ApplyStateWithFrostEnergyBonus(target, States.Frozen, RemainingLifetime());

        if (_talentEvadeDadBoost && _curEvade == 0)
        {
            _curEvade = 3;
            SetEvade(_dad.gameObject, _curEvade);
        }

        if (!_frostingCoroutines.ContainsKey(target))
            _frostingCoroutines[target] = StartCoroutine(CheckAndApplyFrosting(target));
    }

    private IEnumerator CheckAndApplyFrosting(Character enemy)
    {
        const float FrostingDelay = 0.8f;
        float timeWithoutFrosting = 0f;
        bool hadFrosting = false;

        while (_enemiesInZone.Contains(enemy))
        {
            yield return _waitShort;
            if (enemy == null || enemy.CharacterState == null) continue;

            var stateFrosting = enemy.CharacterState.GetState(States.Frosting) as FrostingState;
            
            bool hasFrosting = enemy.CharacterState.CheckForState(States.Frosting);

            if (stateFrosting != null)
            {
                if (!stateFrosting.SkillName.Contains("Puddle"))
                {
                    ApplyStateWithFrostEnergyBonus(enemy, States.Frosting, RemainingLifetime());
                }
                else
                {
                    timeWithoutFrosting = 0f;
                }
                hadFrosting = true;
            }
            else
            {
                timeWithoutFrosting += 0.1f;

                if (timeWithoutFrosting >= FrostingDelay)
                {
                    float duration = RemainingLifetime();
                    if (duration > 0.05f)
                        ApplyStateWithFrostEnergyBonus(enemy, States.Frosting, duration);

                    timeWithoutFrosting = 0f;
                    hadFrosting = false;
                }
            }
        }

        _frostingCoroutines.Remove(enemy);
    }

    private void ApplyStateWithFrostEnergyBonus(Character target, States state, float duration)
    {
        if (target == null || target.CharacterState == null) return;

        var ninjaResources = _dad?.Abilities?.GetSkill<NinjaResources>();
        bool deepFrostingActive = ninjaResources != null && ninjaResources.IsDeepFrosting;

        if (state == States.Frosting && target.CharacterState.CheckForState(States.Frosting))
        {
            if (deepFrostingActive)
            {
                target.CharacterState.AddState(state, duration, GetDamageToExit(),
                    _dad.gameObject, _skill.name + "Puddle");
            }
            else
            {
                var existingState = target.CharacterState.GetState(States.Frosting);
                if (existingState != null && duration > existingState.RemainingDuration)
                {
                    target.CharacterState.AddState(state, duration, GetDamageToExit(),
                        _dad.gameObject, _skill.name + "Puddle");
                }
            }
        }
        else
        {
            target.CharacterState.AddState(state, duration, GetDamageToExit(),
                _dad.gameObject, _skill.name + "Puddle");
        }

        _dad.Abilities.GetSkill<FrostEnergy>()?.ApplyFrostEnergyStateBonus(target, state, _skill);
    }

    private IEnumerator DestroyPuddle()
    {
        yield return new WaitForSeconds(_timeToDestroy);
        Explode();
    }

    private void Explode()
    {
        if (!isServer) return;

        foreach (var enemy in _enemiesInZone)
        {
            if (enemy != null && enemy.CharacterState != null)
                enemy.CharacterState.RemoveState(States.Frosting);
        }

        _enemiesInZone.Clear();

        foreach (var routine in _frostingCoroutines.Values)
            if (routine != null) StopCoroutine(routine);
        _frostingCoroutines.Clear();

        if (_curEvade != 0)
        {
            ClientRpcSetEvade(_dad.gameObject, -_curEvade);
            _dad.Health.SetEvadeAll(-_curEvade);
            _curEvade = 0;
        }

        if (_hitEffect != null)
        {
            GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
            Destroy(hitEffect, 5f);
        }

        NetworkServer.Destroy(gameObject);
    }

    private float RemainingLifetime() =>
        Mathf.Max(0f, _timeToDestroy - (Time.time - _spawnTime));

    [ClientRpc]
    private void ClientRpcSetEvade(GameObject player, float value)
    {
        player.GetComponent<Health>()?.SetEvadeAll(value);
    }

    private void SetEvade(GameObject player, float value)
    {
        player.GetComponent<Health>()?.SetEvadeAll(value);
        ClientRpcSetEvade(player, value);
    }
}