using Mirror;
using System;
using System.Diagnostics;
using UnityEngine;

public class MinionComponent : Character
{
    protected HeroComponent _myHeroParent;

    public event Action<MinionComponent> Destroyed;
    public event Action<MinionComponent> Intercepted;

    public virtual void SetAuthority(NetworkConnectionToClient con)
    {
        var temp = GetComponent<NetworkIdentity>();
        temp.RemoveClientAuthority();
        temp.AssignClientAuthority(con);

        Intercepted?.Invoke(this);
    }

    public void SetAuthority(NetworkConnectionToClient con)
    {
        var temp = GetComponent<NetworkIdentity>();
        temp.RemoveClientAuthority();
        temp.AssignClientAuthority(con);

        Intercepted?.Invoke(this);
    }

    private void OnDestroy()
    {
        Destroyed?.Invoke(this);
    }
}
