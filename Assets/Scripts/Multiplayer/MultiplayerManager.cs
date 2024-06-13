using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerManager : NetworkManager
{
    [SerializeField] private UserPrefab _userPrefab;
    [SerializeField, Scene] private string _onlineScene;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (Utils.IsSceneActive(_onlineScene))
        {
            var player = Instantiate(_userPrefab);
            NetworkServer.AddPlayerForConnection(conn, player.gameObject);
        }
    }

    public override void OnStartClient()
    {
        GameObject[] prefabs = Resources.LoadAll<GameObject>("MPPrefabs");

        for (int i = 0; i < prefabs.Length; i++)
        {
            NetworkClient.RegisterPrefab(prefabs[i]);
        }
    }

    public void SetPlayer(UserPrefab userPrefab)
    {
        _userPrefab = userPrefab;
    }
}
