using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class MPManager : NetworkManager
{
    [Scene] private string _room;
    private int _roomsNum = 0;
    private bool _subscenesLoaded;
    private readonly List<Scene> _rooms = new List<Scene>();
    private int _clientCount;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        StartCoroutine(OnServerAddPlayerDelayed(conn));
    }

    IEnumerator OnServerAddPlayerDelayed(NetworkConnectionToClient conn)
    {
        while (!_subscenesLoaded)
            yield return null;

        conn.Send(new SceneMessage { sceneName = _room, sceneOperation = SceneOperation.LoadAdditive });

        yield return new WaitForEndOfFrame();

        base.OnServerAddPlayer(conn);

        UserNetworkSettings playerScore = conn.identity.GetComponent<UserNetworkSettings>();
        playerScore.MyRoom = _rooms[(_clientCount % _rooms.Count)];
        playerScore.playerNumber = _clientCount;
        playerScore.scoreIndex = _clientCount / _rooms.Count;
        playerScore.matchIndex = _clientCount % _rooms.Count;

        if (_rooms.Count > 0)
            SceneManager.MoveGameObjectToScene(conn.identity.gameObject, _rooms[(_clientCount % _rooms.Count)]);

        _clientCount++;
    }

    public override void OnStartServer()
    {
        StartCoroutine(ServerLoadSubScenes());
    }

    IEnumerator ServerLoadSubScenes()
    {
        for (int index = 0; index <= _roomsNum; index++)
        {
            yield return SceneManager.LoadSceneAsync(_room, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics2D });
            Scene newScene = SceneManager.GetSceneAt(index);
            _rooms.Add(newScene);
        }

        _rooms.RemoveAt(0);

        foreach (var item in _rooms)
        {
            Debug.Log(item.name);
        }

        _subscenesLoaded = true;
    }

    public override void OnStopServer()
    {
        NetworkServer.SendToAll(new SceneMessage { sceneName = _room, sceneOperation = SceneOperation.UnloadAdditive });
        StartCoroutine(ServerUnloadSubScenes());
        _clientCount = 0;
    }

    IEnumerator ServerUnloadSubScenes()
    {
        for (int index = 0; index < _rooms.Count; index++)
            if (_rooms[index].IsValid())
                yield return SceneManager.UnloadSceneAsync(_rooms[index]);

        _rooms.Clear();
        _subscenesLoaded = false;

        yield return Resources.UnloadUnusedAssets();
    }

    public override void OnStopClient()
    {
        if (mode == NetworkManagerMode.Offline)
            StartCoroutine(ClientUnloadSubScenes());
    }

    IEnumerator ClientUnloadSubScenes()
    {
        for (int index = 0; index < SceneManager.sceneCount; index++)
            if (SceneManager.GetSceneAt(index) != SceneManager.GetActiveScene())
                yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(index));
    }
}
