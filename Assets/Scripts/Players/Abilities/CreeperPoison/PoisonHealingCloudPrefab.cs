using Mirror;
using System.Collections;
using UnityEngine;

public class PoisonHealingCloudPrefab : NetworkBehaviour
{
    [SerializeField] private ParticleSystem _poisonHealingCloudParticle;
    [SerializeField] private int _maxStacks = 5;
    [SerializeField] private float _radiusCloud;
    [SerializeField] private LayerMask _alliesLayer;
    [SerializeField] private float _healTickRate = 1f;
    [SerializeField] private float _healModifier = 0.01f;
    [ReadOnly][SerializeField] private Skill _skill;

    private ParticleSystem _instance;
    private Coroutine _healCoroutine;
    private Coroutine _lifetimeCoroutine;
    private float _baseDuration;
    private int _currentStacks;
    private Character _player;

    private void Update()
    {
        if (_instance != null && _player != null)
            _instance.transform.position = _player.transform.position;
    }

    [Server]
    public void ServerInit(Character player, float duration, Skill skill)
    {
        _player = player;
        _skill = skill;
        _baseDuration = duration;
    }

    [Server]
    public void ServerAddStack()
    {
        if (_player == null) return;

        if (_currentStacks < _maxStacks) _currentStacks++;

        ShowParticle();

        if (_lifetimeCoroutine != null) StopCoroutine(_lifetimeCoroutine);
        _lifetimeCoroutine = StartCoroutine(LifeTimeJob());

        if (_healCoroutine == null) _healCoroutine = StartCoroutine(HealAllies());

        RpcRefresh(_player.gameObject, _baseDuration);
    }

    [ClientRpc]
    private void RpcRefresh(GameObject playerObject, float duration)
    {
        if (isServer) return;
        if (playerObject == null) return;

        _player = playerObject.GetComponent<Character>();
        _baseDuration = duration;
        ShowParticle();
    }

    private void ShowParticle()
    {
        if (!NetworkClient.active || _player == null) return;

        if (_instance == null)
            _instance = Instantiate(_poisonHealingCloudParticle, _player.transform);
        else
            _instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _instance.main;
        main.duration = _baseDuration;
        _instance.Play();
    }

    [Server]
    private void HealInRadius()
    {
        if (_player == null || _skill == null) return;

        Collider[] targets = Physics.OverlapSphere(_player.transform.position, _radiusCloud, _alliesLayer);
        foreach (var col in targets)
        {
            if (col == null) continue;
            if (!col.TryGetComponent<Character>(out var target)) continue;
            if (target.IsDead || target == _player) continue;

            var heal = new Heal { Value = target.Health.MaxValue * _healModifier, DamageableSkill = null };
            _skill.ServerApplyHeal(heal, target.gameObject);
        }
    }

    private IEnumerator HealAllies()
    {
        while (true)
        {
            HealInRadius();
            yield return new WaitForSeconds(_healTickRate);
        }
    }

    private IEnumerator LifeTimeJob()
    {
        yield return new WaitForSeconds(_baseDuration);
        _lifetimeCoroutine = null;
        NetworkServer.Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance != null) Destroy(_instance.gameObject);
    }
}