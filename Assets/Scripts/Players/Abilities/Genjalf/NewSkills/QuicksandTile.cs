using System;
using System.Collections.Generic;
using Gangdollarff;
using Mirror;
using UnityEngine;

public class QuicksandTile : FisuraTile
{
    [SerializeField] private ParticleSystem _sandParticle;
    
    private List<GameObject> _charTemp = new();
    
    private bool IsEnemyTarget(GameObject target) => target.layer == LayerMask.NameToLayer("Enemy");

    private void Start()
    {
        base.Start();

        _collider.isTrigger = true;
    }

    public override void Build()
    {
        base.Build();
        
        var shape = _sandParticle.shape;
        shape.scale = new Vector3(_collider.size.x,_collider.size.z);
        _sandParticle.gameObject.transform.localPosition = _collider.center;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Character character) && IsEnemyTarget(other.gameObject))
        {
            _charTemp.Add(other.gameObject);
            ChangeMoveSpeed(character.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Character character) && IsEnemyTarget(other.gameObject))
        {
            SetDefaultSpeed(character.gameObject);
            _charTemp.Remove(other.gameObject);
        }
    }

    private void ChangeMoveSpeed(GameObject target)
    {
        if (target.TryGetComponent(out Character character))
        {
            character.Move.ChangeMoveSpeed(0.2f);
        }
    }

    private void SetDefaultSpeed(GameObject target)
    {
        if (target.TryGetComponent(out Character character))
        {
            character.Move.SetDefaultSpeed();
        }
    }

    private void OnDestroy()
    {
        if (_charTemp.Count != 0)
        {
            foreach (var character in _charTemp)
            {
                SetDefaultSpeed(character);
            }
        }
        
        _charTemp.Clear();
    }
}
