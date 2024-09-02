using Mirror;
using Org.BouncyCastle.Asn1.Pkcs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonCloudDisplay : NetworkBehaviour
{
    [SerializeField] private Character _dad;
    [SerializeField] private ParticleSystem _poisonCloudPrefab;

    public PoisonCloudDisplay PoisonHealingCloud { get; set; }
    public PoisonCloudDisplay PoisonDamagingCloud { get; set; }

    private ParticleSystem _instancePoisonCloud;

    public int _currentStacks;
    public int _maxStacks;

    private float _duration;
    private float _baseDuration;

    private float _radiusCloud;

    private Coroutine _activatePoisonCloudCoroutine;
    private Coroutine _lifeTimeStacksCoroutine;

    public void InitializationPrefab(Character player, float duration, float radiusCloud, int maxStacks)
    {
        _dad = player;
        _duration = duration;
        _baseDuration = duration;
        _radiusCloud = radiusCloud;
        _maxStacks = maxStacks;
    }

    public void AddStack()
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            if (_activatePoisonCloudCoroutine == null)
            {
                _activatePoisonCloudCoroutine = StartCoroutine(ActivatePoisonCloud());
            }
            else
            {
                UpdateInstancePoisonCloud();
            }
        }

        if (_lifeTimeStacksCoroutine != null)
        {
            StopCoroutine(_lifeTimeStacksCoroutine);
        }

        _lifeTimeStacksCoroutine = StartCoroutine(LifeTimeStacks());
    }

    private void InstantiateCloud()
    {
        if (_instancePoisonCloud == null)
        {
            _instancePoisonCloud = Instantiate(_poisonCloudPrefab, transform.position, Quaternion.identity);
        }

        _instancePoisonCloud.Play();
    }

    private void UpdateInstancePoisonCloud()
    {
        if (_instancePoisonCloud != null)
        {
            _instancePoisonCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = _instancePoisonCloud.main;
            main.duration = _duration;
            _instancePoisonCloud.Play();
        }
    }

    private void Update()
    {
        if (_instancePoisonCloud != null)
        {
            _instancePoisonCloud.transform.position = _dad.transform.position;
        }
    }

    private IEnumerator ActivatePoisonCloud()
    {
        InstantiateCloud();
        yield return null;
    }

    private IEnumerator LifeTimeStacks()
    {
        _duration = _baseDuration;
        //Debug.Log("PoisonCloudDisplay / LifeTimeStacks");

        while (_currentStacks > 0)
        {
            yield return new WaitForSecondsRealtime(_duration);
            _currentStacks = 0;
            //Debug.Log($"PoisonCloudDisplay / LifeTimeStacks / after yield return / currentStacks = {_currentStacks}");
        }

        if (_currentStacks == 0)
        {
            if (_instancePoisonCloud != null)
            {
                _instancePoisonCloud.Stop();
                Destroy(_instancePoisonCloud.gameObject);
                _instancePoisonCloud.transform.parent = null;
                _instancePoisonCloud = null;
            }
            PoisonHealingCloud = null;
            PoisonDamagingCloud = null;
            StopAllCoroutines();
            Destroy(gameObject);
        }
    }

}