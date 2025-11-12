using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Mucus : NetworkBehaviour
{
    private ObjectHealth _objectHealth;
    private List<MucusAutoGrowth> _mucusAutoGrowths = new();
    public List<MucusAutoGrowth> MucusAutoGrowths { get => _mucusAutoGrowths; set => _mucusAutoGrowths = value; }

    private void Start()
    {
        _objectHealth = GetComponent<ObjectHealth>();
    }
    public void AddMucusAutoGrowth(MucusAutoGrowth growth)
    {
        if (growth == null) return;

        if (_mucusAutoGrowths == null) _mucusAutoGrowths = new();

        if (!_mucusAutoGrowths.Contains(growth))
        {
            _mucusAutoGrowths.Add(growth);
            growth.OnAnyMucusAutoGrowthDestroyed += HandleMucusAutoGrowthDestroyed;
        }
    }
    private void HandleMucusAutoGrowthDestroyed()
    {
        _mucusAutoGrowths.RemoveAll(item => item == null || !item.isActiveAndEnabled);
        CheckAndUpdateState();
    }

    private void CheckAndUpdateState()
    {
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
}
