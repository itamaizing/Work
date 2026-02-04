using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReconnaissanceFireAura : NetworkBehaviour
{
    [SerializeField] private float _partialBlindnessDuration = 5f;
    [SerializeField] private float _innerDarknessDuration = 13;
    [SerializeField] private GameObject _fireEffect;
    [SerializeField] private GameObject _fireEffectDark;
    [SerializeField] private bool _fireDarkTalent;
    [SerializeField] private bool _partialBlindnessTalent;
    [SerializeField] private FlameLightPulse _flameLightPulse;
    [SerializeField] private LayerMask _characterLayer;
    
    public event Action<bool> OnStateDarkTalentChanged;
    private Character _ownerHero;

    private readonly List<Character> _charactersInZone = new();
    private readonly HashSet<uint> _clientIds = new();
    private Coroutine _effectCoroutine;
    private WaitForSeconds _waitForSecond;

    #region Const
    private const float FireFlashDuration = 9999f;
    private const int MaxChanceValue = 100;
    private const int MinChanceValue = 0;
    #endregion

    [SyncVar(hook = nameof(OnStateDarkChanged))]
    private bool stateDark;

    public bool FireDarkTalent { get => _fireDarkTalent; set => _fireDarkTalent = value; }
    //public bool PartialBlindnessTalent { get => _partialBlindnessTalent; set => _partialBlindnessTalent = value; }
    public bool StateDark { get => stateDark; set => stateDark = value; }

    private bool IsEnemy(Character characterTarget, GameObject target)
    {
        if (_ownerHero == null) return IsEnemyByLayer(target);
        if (!_ownerHero.TryGetComponent(out UserNetworkSettings ownerSettings) || !characterTarget.TryGetComponent(out UserNetworkSettings targetSettings)) return IsEnemyByLayer(target);
        if (!IsTeamAssigned(ownerSettings) || !IsTeamAssigned(targetSettings)) return IsEnemyByLayer(target);

        return ownerSettings.TeamIndex != targetSettings.TeamIndex;
    }

    private bool IsTeamAssigned(UserNetworkSettings settings)
    {
        return settings.TeamIndex != 0;
    }

    private bool IsEnemyByLayer(GameObject target)
    {
        return ((1 << target.layer) & _characterLayer.value) != 0;
    }

    private void Start()
    {
        _waitForSecond = new WaitForSeconds(1);
    }

    public void Init(Character hero)
    {
        _ownerHero = hero;
    }

    [Server]
    private void RemoveAuthority()
    {
        var id = netIdentity;
        if (id.connectionToClient != null) id.RemoveClientAuthority();
    }

    private void OnDestroy()
    {
        if (_effectCoroutine != null) StopCoroutine(_effectCoroutine);


        foreach (var character in _charactersInZone) ForceExit(character);
        foreach (var id in _clientIds.ToArray()) RemoveCharacter(id);

        _charactersInZone.Clear();
        _clientIds.Clear();
    }

    private void ForceExit(Character character)
    {
        if (character == null) return;
        if (character.TryGetComponent<CharacterState>(out var state) && state.GetState(States.FireFlash) is FireFlash flash) flash.SwitchToFinite();
    }

    private void OnStateDarkChanged(bool oldValue, bool newValue)
    {
        SwitchEffectFire();
        OnStateDarkTalentChanged?.Invoke(newValue);
    }

    [Server]
    public void HandleTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out Character character)) return;
        if (_charactersInZone.Contains(character)) return;

        if (!IsEnemy(character, other.gameObject)) return;

        _charactersInZone.Add(character);
        RpcAddCharacter(character.netId);

        if (_effectCoroutine == null)
            _effectCoroutine = StartCoroutine(ApplyPartialBlindnessPeriodically());
    }

    [Server]
    public void HandleTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out Character character)) return;

        if (!IsEnemy(character, other.gameObject)) return;

        _charactersInZone.Remove(character);
        ForceExit(character);
        RpcRemoveCharacter(character.netId);

        if (_charactersInZone.Count == 0 && _effectCoroutine != null)
        {
            StopCoroutine(_effectCoroutine);
            _effectCoroutine = null;
        }
    }

    private IEnumerator ApplyPartialBlindnessPeriodically()
    {
        while (_charactersInZone.Count > 0)
        {
            foreach (Character character in _charactersInZone)
            {
                if (character == null || !character.TryGetComponent(out CharacterState state)) continue;

                state.AddState(States.TrueSightState, 1.1f, 0f, gameObject, "ReconnaissanceFireAura");

                if (stateDark && _fireDarkTalent)
                {
                    state.AddState(States.FireFlash, FireFlashDuration, 0f, gameObject, name);
                    var flash = state.GetState(States.FireFlash) as FireFlash;

                    if (UnityEngine.Random.Range(MinChanceValue, MaxChanceValue) < flash.Chance)
                    {
                        Debug.Log($"Chance: {flash.Chance}");
                        state.AddState(States.InnerDarkness, _innerDarknessDuration, 0f, gameObject, "ReconnaissanceFireAuraDark");
                    }
                    continue;
                }

                if (_partialBlindnessTalent) state.AddState(States.PartialBlindness, _partialBlindnessDuration, 0f, gameObject, "partialBlindnessTalent");
                else state.AddState(States.PartialBlindness, _partialBlindnessDuration, 0f, gameObject, "ReconnaissanceFireAura");
            }

            yield return _waitForSecond;
        }

        _effectCoroutine = null;
    }

    public void ApplyFireWorshipperTalentEffect(bool isActive)
    {
        if (isActive)
        {
            transform.localScale += Vector3.one;
            if (_fireEffect != null) _fireEffect.transform.localScale += Vector3.one;
            if (_fireEffectDark != null) _fireEffectDark.transform.localScale += Vector3.one;
            if (this.TryGetComponent<VisionComponent>(out VisionComponent vision)) vision.VisionRange += 1;

            if (_flameLightPulse != null)
            {
                _flameLightPulse.FlameLight.range += 1;
                Vector3 position = _flameLightPulse.transform.position;
                position.y -= 1f;
                _flameLightPulse.transform.position = position;
            }
        }
    }

    public void SwitchEffectFire()
    {
        _fireEffect.SetActive(false);
        _fireEffectDark.SetActive(true);
    }

    [ClientRpc]
    private void RpcAddCharacter(uint netId)
    {
        if (!NetworkClient.spawned.TryGetValue(netId, out var id)) return;
        if (!_clientIds.Add(netId)) return;
    }

    [ClientRpc] private void RpcRemoveCharacter(uint netId) => RemoveCharacter(netId);

    private void RemoveCharacter(uint netId)
    {
        if (!_clientIds.Remove(netId)) return;

        if (NetworkClient.spawned.TryGetValue(netId, out var id) &&
            id.TryGetComponent(out CharacterState state))
        {
            (state.GetState(States.FireFlash) as FireFlash)?.SwitchToFinite();
        }
    }
}