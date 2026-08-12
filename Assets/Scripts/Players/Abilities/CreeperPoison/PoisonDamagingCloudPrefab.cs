using Mirror;
using System.Collections;
using UnityEngine;

public class PoisonDamagingCloudPrefab : NetworkBehaviour
{
    [SerializeField] private ParticleSystem _poisonDamagingCloudParticle;
    private ParticleSystem _instancePoisonDamagingCloud;

    private Coroutine _lifetimeCoroutine;
    private Coroutine _activateParticleCoroutine;

    private float _baseDuration;
    private float _duration;

    private PoisonDamagingCloudPrefab _poisonDamageCloud;
    private Character _player;
    [ReadOnly][SerializeField] private Skill _skill;

    public PoisonDamagingCloudPrefab PoisonDamageCloud { get => _poisonDamageCloud; set => _poisonDamageCloud = value; }

    private void Update()
    {
        if (_instancePoisonDamagingCloud != null && _player != null)
        {
            _instancePoisonDamagingCloud.transform.position = _player.transform.position;
        }
    }

    public void InitializationProjectile(Character player, float duration, Skill skill, bool isFeelingPoisoning)
    {
        _player = player;
        _skill = skill;

        _duration = duration;
        _baseDuration = duration;
    }

    public void AddStack()
    {
        if (_activateParticleCoroutine == null && _poisonDamageCloud == null)
        {
            _activateParticleCoroutine = StartCoroutine(ActivatePoisonCloud());
        }
        else
        {
            UpdateInstanceCloud();
        }
        
        if (_player != null)
        {
            _player.CharacterState.CmdAddState(
                States.PoisonCloud, 
                _baseDuration, 
                0, 
                _player.gameObject, 
                _skill != null ? _skill.Name : null
            );
        }

        if (_lifetimeCoroutine != null) StopCoroutine(_lifetimeCoroutine);

        _duration = _baseDuration;
        _lifetimeCoroutine = StartCoroutine(LifeTimeJob());
    }

    private void InstantiateCloud()
    {
        if (_instancePoisonDamagingCloud == null && _player != null)
        {
            _instancePoisonDamagingCloud = Instantiate(_poisonDamagingCloudParticle, _player.transform);
            _instancePoisonDamagingCloud.Play();
        }
    }

    private void UpdateInstanceCloud()
    {
        if (_instancePoisonDamagingCloud != null)
        {
            _instancePoisonDamagingCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = _instancePoisonDamagingCloud.main;
            main.duration = _baseDuration;
            _instancePoisonDamagingCloud.Play();
        }
    }

    private IEnumerator ActivatePoisonCloud()
    {
        InstantiateCloud();
        yield return null;
    }

    private IEnumerator LifeTimeJob()
    {
        float time = _duration;

        while (time > 0)
        {
            time -= Time.deltaTime;
            yield return null;
        }

        if (_activateParticleCoroutine != null)
        {
            StopCoroutine(_activateParticleCoroutine);
            _activateParticleCoroutine = null;
        }

        if (_lifetimeCoroutine != null)
        {
            StopCoroutine(_lifetimeCoroutine);
            _lifetimeCoroutine = null;
        }

        if (_instancePoisonDamagingCloud != null)
        {
            _instancePoisonDamagingCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(_instancePoisonDamagingCloud.gameObject);
        }

        Destroy(gameObject);
        PoisonDamageCloud = null;
    }
}