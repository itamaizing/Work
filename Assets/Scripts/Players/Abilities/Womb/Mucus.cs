using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Mucus : NetworkBehaviour
{
    [SerializeField][SyncVar] private List<MucusAutoGrowth> _mucusAutoGrowths = new List<MucusAutoGrowth>();

    private ObjectHealth _objectHealth;
    private Coroutine _delayedCheckCoroutine;
    public List<MucusAutoGrowth> MucusAutoGrowths 
    { 
        get => _mucusAutoGrowths;
        set
        {
            _mucusAutoGrowths = value;
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
        UpdateRegenMod();
    }
    private void Start()
    {
        _objectHealth = GetComponent<ObjectHealth>();
        UpdateRegenMod();
    }

    public void AddMucusAutoGrowth(MucusAutoGrowth autoGrowth)
    {
        if (autoGrowth == null || _mucusAutoGrowths.Contains(autoGrowth)) return;

        _mucusAutoGrowths.Add(autoGrowth);
        UpdateRegenMod();
    }

    public void RemoveMucusAutoGrowth(MucusAutoGrowth autoGrowth)
    {
        if (_mucusAutoGrowths.Remove(autoGrowth))
        {
            UpdateRegenMod();
        }
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
    private void UpdateRegenMod()
    {
        if (_objectHealth != null) _objectHealth.RegenMod = _mucusAutoGrowths.Count;
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
