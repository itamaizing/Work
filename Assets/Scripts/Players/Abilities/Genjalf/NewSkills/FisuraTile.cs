using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gangdollarff
{
    public class FisuraTile : NetworkBehaviour
    {
        [SerializeField] protected BoxCollider _collider;
        [SerializeField] protected GameObject[] _tiles;
        [SyncVar] protected int _maxSizeLock = 7;
        [SyncVar] protected float _widthModifier = 0f;

        [SyncVar] private Vector3 _startPosition;
        [SyncVar] private Vector3 _endPosition;
        [SyncVar] private int _size;
        
        public void AddMaxSize(int value) => _maxSizeLock += value;
        public void AddWidth(float value) => _widthModifier += value;

        [Client]
        protected virtual void Start()
        {
            Build();
        }

        public void SetStartPosition(Vector3 vector3)
        {
            _startPosition = vector3;
        }

        public void SetEndPosition(Vector3 vector3)
        {
            _endPosition = vector3;
        }

        public virtual void Build()
        {
            transform.position = _startPosition;
            transform.LookAt(_endPosition);

            var dir = _endPosition - _startPosition;
            float lenght = dir.magnitude;
            _size = (int)Math.Round(lenght);

            if(_size > _maxSizeLock)
                _size = _maxSizeLock;

            for (int i = 0; i < _size; i++)
                _tiles[i].SetActive(true);

            _collider.center = new Vector3(_collider.center.x, _collider.center.y, _size / 2f);
            _collider.size = new Vector3(_collider.size.x + _widthModifier, _collider.size.y, _size);
            _collider.enabled = true;
        }
    }
}
