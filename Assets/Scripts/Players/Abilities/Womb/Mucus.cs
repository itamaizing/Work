using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Mucus : NetworkBehaviour
{
    [SerializeField] private List<MucusAutoGrowth> _mucusAutoGrowths = new List<MucusAutoGrowth>();

    private ObjectHealth _objectHealth;
    private Coroutine _delayedCheckCoroutine;
    private bool _isSubscribed = false;
    public List<MucusAutoGrowth> MucusAutoGrowths 
    { 
        get => _mucusAutoGrowths;
        set
        {
            _mucusAutoGrowths = value;
            _objectHealth.RegenMod = _mucusAutoGrowths.Count;
        }
    }
    private void OnEnable()
    {
        MucusAutoGrowth.OnAnyMucusAutoGrowthDestroyed += DelayedCheck;
    }
    private void OnDisable()
    {
        MucusAutoGrowth.OnAnyMucusAutoGrowthDestroyed -= DelayedCheck;
        _mucusAutoGrowths.Clear();
    }
    private void Start()
    {
        _objectHealth = GetComponent<ObjectHealth>();
    }
    public void CheckAndUpdateState()
    {
        _mucusAutoGrowths.RemoveAll(item => item == null || item.Equals(null) || item.gameObject == null);

        if (_mucusAutoGrowths.Count <= 0)
        {
            if (_objectHealth != null)
            {
                _objectHealth.IsDestroyOnDeath = true;
                _objectHealth.ÑmdStopCustomRegeneration();
                _objectHealth.ÑmdStartCustomNegativeRegeneration();
            }
        }
    }
    private void DelayedCheck()
    {
        if (_delayedCheckCoroutine != null) StopCoroutine(_delayedCheckCoroutine);
        _delayedCheckCoroutine = StartCoroutine(DelayedCheckRoutine());
    }
    private IEnumerator DelayedCheckRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        CheckAndUpdateState();
    }
}
