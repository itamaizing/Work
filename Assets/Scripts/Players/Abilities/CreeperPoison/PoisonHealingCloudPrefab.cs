using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonHealingCloudPrefab : NetworkBehaviour
{
    [SerializeField] private ParticleSystem _poisonHealingCloudParticle;
    private ParticleSystem _instancePoisonHealingCloud;

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
    public PoisonHealingCloudPrefab PoisonHealingCloud { get; set; }

    private void Update()
    {
        if (_instancePoisonHealingCloud != null)
        {
            //Debug.Log("PoisonHealingCloudPrefab / Update / after first if");
            _instancePoisonHealingCloud.transform.position = _player.transform.position;
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
        //Debug.Log("PoisonHealingCloudPrefab / InitializationProjectile / _player = " + _player);
        //Debug.Log("PoisonHealingCloudPrefab / InitializationProjectile / _maxStacks = " + _maxStacks);
        //Debug.Log("PoisonHealingCloudPrefab / InitializationProjectile / _duration = " + _duration);
        //Debug.Log("PoisonHealingCloudPrefab / InitializationProjectile / _radiusCloud = " + _radiusCloud);
        //Debug.Log("PoisonHealingCloudPrefab / InitializationProjectile / _isHealingCloud = " + _isHealingCloud);

    }

    public void AddStack()
    {
        //Debug.Log("PoisonHealingCloudPrefab / AddStack");

        if (_currentStacks < _maxStacks)
        {
            //Debug.Log("PoisonHealingCloudPrefab / AddStack / if (currentStacks < maxStacks)");
            _currentStacks++;
            if (_activateParticlePoisonCloudCoroutine == null)
            {
                _activateParticlePoisonCloudCoroutine = StartCoroutine(ActivatePoisonCloud());
                //Debug.Log("PoisonHealingCloudPrefab / AddStack /   if (_activateParticlePoisonCloudCoroutine == null) /_activateParticlePoisonCloudCoroutine = " + _activateParticlePoisonCloudCoroutine);
            }
            else
            {
                //Debug.Log("PoisonHealingCloudPrefab / AddStack / else / UpdateInstanceCloud Called");
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
        //Debug.Log("PoisonHealingCloudPrefab / InstantiateCloud");

        if (_instancePoisonHealingCloud == null)
        {
            _instancePoisonHealingCloud = Instantiate(_poisonHealingCloudParticle, _player.transform);
            _instancePoisonHealingCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = _instancePoisonHealingCloud.main;
            main.duration = _duration;
            _instancePoisonHealingCloud.Play();
            //Debug.Log("PoisonHealingCloudPrefab / InstantiateCloud / _instancePoisonDamagingCloud = " + _instancePoisonHealingCloud);
        }

    }

    private void UpdateInstanceCloud()
    {
        //Debug.Log("PoisonHealingCloudPrefab / UpdateIntanceCloud");
        
        if (_instancePoisonHealingCloud != null)
        {
            //Debug.Log("PoisonHealingCloudPrefab / UpdateIntanceCloud / _instancePoisonDamagingCloud != null");
            _instancePoisonHealingCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = _instancePoisonHealingCloud.main;
            main.duration = _baseDuration;
            _instancePoisonHealingCloud.Play();
        }
    }

    private IEnumerator ActivatePoisonCloud()
    {
        InstantiateCloud();
        yield return null;
    }

    private IEnumerator LifeTimeStacks()
    {
        //Debug.Log("PoisonHealingCloudPrefab / LifeTimeStacks");

        yield return new WaitForSecondsRealtime(_duration);
        //Debug.Log("PoisonHealingCloudPrefab / LifeTimeStacks / after yield return");
        while (_currentStacks > 0)
        {
            _currentStacks = 0;
        }

        if (_instancePoisonHealingCloud != null)
        {
            //Debug.Log("PoisonHealingCloudPrefab / Damage cloud not null");
            _instancePoisonHealingCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(_instancePoisonHealingCloud.gameObject);
            _instancePoisonHealingCloud = null;

            Destroy(gameObject);
            PoisonHealingCloud = null;
        }

        StopAllCoroutines();
    }


}
