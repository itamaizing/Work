using Mirror;
using Org.BouncyCastle.Asn1.Pkcs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonCloudDisplay : NetworkBehaviour
{
    [SerializeField] private Character _dad;
    [SerializeField] private ParticleSystem _poisonCloudPrefab;

    public Character IsOwner;

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
        this.IsOwner = player;
        _dad = player;
        _duration = duration;
        _baseDuration = duration;
        _radiusCloud = radiusCloud;
        _maxStacks = maxStacks;
    }

    public void AddStack()
    {
        Debug.Log("AddStack");
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            Debug.Log($"AddStack / currentStacks = {_currentStacks}");
            if (_activatePoisonCloudCoroutine == null)
            {
                Debug.Log($"(if == null) _activatePoisonCloudCoroutine == {_activatePoisonCloudCoroutine}");
                _activatePoisonCloudCoroutine = StartCoroutine(ActivatePoisonCloud());
            }
            else
            {
                Debug.Log($" (else != null) _activatePoisonCloudCoroutine == {_activatePoisonCloudCoroutine}");
                UpdateInstancePoisonCloud();

                if (_lifeTimeStacksCoroutine != null)
                {
                    Debug.Log($" (if != null) _lifeTimeStacksCoroutine == {_lifeTimeStacksCoroutine}");
                    StopCoroutine(LifeTimeStacks());
                    _lifeTimeStacksCoroutine = null;
                    Debug.Log($" (if != null) after == null / _lifeTimeStacksCoroutine == {_lifeTimeStacksCoroutine}");
                }
            }

            _duration = _baseDuration;
            _lifeTimeStacksCoroutine = StartCoroutine(LifeTimeStacks());
            Debug.Log($"Start / _lifeTimeStacksCoroutine == {_lifeTimeStacksCoroutine}");
        }
        else if (_currentStacks == _maxStacks)
        {
            if (_lifeTimeStacksCoroutine != null)
            {
                StopCoroutine(LifeTimeStacks());
                _lifeTimeStacksCoroutine = null;
            }
        }
    }

    private void InstantiateCloud()
    {
        if (_instancePoisonCloud == null)
        {
            _instancePoisonCloud = Instantiate(_poisonCloudPrefab, transform.position, Quaternion.identity);
        }

        _instancePoisonCloud.Play();
        Debug.Log("InstanceCloud play");
    }

    private void UpdateInstancePoisonCloud()
    {
        Debug.Log("UdpatePoisonCloud");
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
        Debug.Log($"PoisonCloudDisplay / LifeTimeStacks");
        Debug.Log($"PoisonCloudDisplay / LifeTimeStacks / _currentStacks = {_currentStacks}");
        Debug.Log($"PoisonCloudDisplay / LifeTimeStacks / instancePoisonCloud = {_instancePoisonCloud}");

        Debug.Log($"PoisonCloudDisplay / LifeTimeStacks / _duration = {_duration}");
        yield return new WaitForSecondsRealtime(_duration);
        Debug.Log("After time while");

        while (_currentStacks > 0)
        {
            _currentStacks--;
            Debug.Log($"PoisonCloudDisplay / LifeTimeStacks / while currentStack > 0 (== {_currentStacks})");
        }

        if (_currentStacks == 0)
        {
            Debug.Log($"PoisonCloudDisplay / LifeTimeStacks / if currentStacks = 0 ( == {_currentStacks})");
            if (_instancePoisonCloud != null)
            {
                Debug.Log($"PoisonCloudDisplay / LifeTimeStacks / instanceCloud != null");
                _instancePoisonCloud.Stop();
                Destroy(_instancePoisonCloud.gameObject);
                _instancePoisonCloud.transform.parent = null;
                _instancePoisonCloud = null;
            }
            StopAllCoroutines();
            Destroy(gameObject);
        }
    }
}