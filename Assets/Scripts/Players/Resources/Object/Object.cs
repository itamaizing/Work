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


    private bool _isDeath;

    public ObjectData ObjectData => _objectData;
    public ObjectHealth ObjectHealth => _objectHealth;
    public List<Resource> Resources => _resources;
    public bool IsDeath => _isDeath;

    public int IndexTeam { get => _indexTeam; set => _indexTeam = value; }

    private void OnDestroy() => _objectHealth.OnDeath -= CmdOnDeath;

    public void Initialize()
    {
        foreach (var resource in Resources)
            if (resource.Type == ResourceType.Health) resource.Initialize(_objectData.MaxHealth, _objectData.RegenerationRate, 0, null);

        _objectHealth.InitializeObject(_objectData);
        _objectHealth.OnDeath += CmdOnDeath;
    }

    private void OnDied() => _isDeath = true;
    private void Start() => Initialize();
    public override void OnStartServer() => base.OnStartServer();
    public override void OnStopServer() => base.OnStopServer();

    [Command] private void CmdOnDeath() => RpcClientOnDied();
    [ClientRpc] private void RpcClientOnDied() => OnDied();
}
