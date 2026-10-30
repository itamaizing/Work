using Mirror;
using System.Collections;
using UnityEngine;

public class PoisonDamagingCloudPrefab : NetworkBehaviour
{
    [SerializeField] private ParticleSystem _poisonDamagingCloudParticle;
    [ReadOnly][SerializeField] private Skill _skill;

    private ParticleSystem _instance;
    private Coroutine _lifetimeCoroutine;
    private float _baseDuration;
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

        ShowParticle();
        _player.CharacterState.AddState(States.PoisonCloud, _baseDuration, 0f,
            _player.gameObject, _skill != null ? _skill.Name : null);

        if (_lifetimeCoroutine != null) StopCoroutine(_lifetimeCoroutine);
        _lifetimeCoroutine = StartCoroutine(LifeTimeJob());

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
        {
            _instance = Instantiate(_poisonDamagingCloudParticle, _player.transform);
        }
        else
        {
            _instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        var main = _instance.main;
        main.duration = _baseDuration;
        _instance.Play();
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

        if (NetworkServer.active && _player != null && _player.CharacterState != null)
            _player.CharacterState.RemoveState(States.PoisonCloud);
    }
}