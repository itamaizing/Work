using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonDamagingCloudPrefab : NetworkBehaviour
{
    [SerializeField] private ParticleSystem _poisonDamagingCloudParticle;
    private ParticleSystem _instancePoisonDamagingCloud;

    private Character _player;

    private int _currentStacks;
    private int _maxStacks;

    private float _baseDuration;
    private float _duration;
    private float _radiusCloud;

    private string _skillName;
    private bool _isHealingCloud;

    private Coroutine _lifetimeStacksCoroutine;
    private Coroutine _activateParticlePoisonCloudCoroutine;
    public PoisonDamagingCloudPrefab PoisonDamageCloud;

    private void Update()
    {
        if (_instancePoisonDamagingCloud != null)
        {
            Debug.Log("PoisonDamagingCloudPrefab / Update / after first if");
            _instancePoisonDamagingCloud.transform.position = _player.transform.position;
        }
    }

    public void InitializationProjectile(Character player, int maxStacks, float duration, float radiusCloud, string name)
    {
        _player = player;
        
        _maxStacks = maxStacks;
        _duration = duration;
        _baseDuration = duration;
        _radiusCloud = radiusCloud;
        _isHealingCloud = false;
        _skillName = name;
       Debug.Log("PoisonCloudProjectile / InitializationProjectile / _player = " + _player);
       Debug.Log("PoisonCloudProjectile / InitializationProjectile / _maxStacks = " + _maxStacks);
       Debug.Log("PoisonCloudProjectile / InitializationProjectile / _duration = " + _duration);
       Debug.Log("PoisonCloudProjectile / InitializationProjectile / _radiusCloud = " + _radiusCloud);
       Debug.Log("PoisonCloudProjectile / InitializationProjectile / _isHealingCloud = " + _isHealingCloud);

    }

    public void AddStack()
    {
        Debug.Log("PoisonCloudProjectile / AddStack");

        if (_currentStacks < _maxStacks)
        {
            Debug.Log("PoisonCloudProjectile / AddStack / if (currentStacks < maxStacks)");
            _currentStacks++;
            if (_activateParticlePoisonCloudCoroutine == null && PoisonDamageCloud == null)
            {
                _activateParticlePoisonCloudCoroutine = StartCoroutine(ActivatePoisonCloud());
                Debug.Log("PoisonCloudProjectile / AddStack /   if (_activateParticlePoisonCloudCoroutine == null) /_activateParticlePoisonCloudCoroutine = " + _activateParticlePoisonCloudCoroutine);
            }
            else
            {
                Debug.Log("PoisonCloudProjectile / AddStack / else / UpdateInstanceCloud Called");
                UpdateInstanceCloud();
            }
            _duration = _baseDuration;
        }

        if (_lifetimeStacksCoroutine != null)
        {
            StopCoroutine(_lifetimeStacksCoroutine);
        }

        _duration = _baseDuration;
        _lifetimeStacksCoroutine = StartCoroutine(LifeTimeStacks());
    }

    private void InstantiateCloud()
    {
        Debug.Log("PoisonCloudProjectile / InstantiateCloud");

        if (_instancePoisonDamagingCloud == null)
        {
            _instancePoisonDamagingCloud = Instantiate(_poisonDamagingCloudParticle, _player.transform);
            _instancePoisonDamagingCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = _instancePoisonDamagingCloud.main;
            main.duration = _duration;
            _instancePoisonDamagingCloud.Play();
            Debug.Log("PoisonCloudProjectile / InstantiateCloud / _instancePoisonDamagingCloud = " + _instancePoisonDamagingCloud);
        }
        
    }

    private void UpdateInstanceCloud()
    {
       Debug.Log("PoisonCloudProjectile / UpdateIntanceCloud");

        if (_instancePoisonDamagingCloud != null)
        {
           Debug.Log("PoisonCloudProjectile / UpdateIntanceCloud / _instancePoisonDamagingCloud != null");
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

    private IEnumerator LifeTimeStacks()
    {
        Debug.Log("PoisonCloudProjectile / LifeTimeStacks");

        yield return new WaitForSecondsRealtime(_duration);
        Debug.Log("PoisonCloudProjectile / LifeTimeStacks / after yield return");
        while (_currentStacks > 0)
        {
            _currentStacks = 0;
        }

        if (_instancePoisonDamagingCloud != null)
        {
            Debug.Log("PoisonCloudProjectile / Damage cloud not null");
            _instancePoisonDamagingCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(_instancePoisonDamagingCloud.gameObject);
            _instancePoisonDamagingCloud = null;

            Destroy(gameObject);
            PoisonDamageCloud = null;
        }

        StopAllCoroutines();
    }

    
}
