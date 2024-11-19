using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class Object : NetworkBehaviour
{
    [SerializeField] private ObjectData _objectData;
    [SerializeField] private ObjectHealth _objectHealth;
    [SerializeField] private List<Resource> _resources;
    [SerializeField] private int _indexTeam;

    public ObjectData ObjectData => _objectData;
    public ObjectHealth ObjectHealth => _objectHealth;
    public List<Resource> Resources => _resources;

    public int IndexTeam { get => _indexTeam; set => _indexTeam = value; }

    public void Initialize()
    {
        foreach (var resource in Resources)
        {
            if (resource.Type == ResourceType.Health)
            {
                resource.Initialize(
                    _objectData.MaxHealth,
                    _objectData.RegenerationRate,
                    0,
                    null);
            }
        }

        if (_objectHealth != null)
        {
            _objectHealth.InitializeObject(_objectData);
        }
    }

    private void Start()
    {
        Initialize();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
    }
}
