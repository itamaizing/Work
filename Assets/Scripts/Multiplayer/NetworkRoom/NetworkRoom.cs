using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public abstract class NetworkRoom : NetworkManager
{
    [SerializeField, Scene] public string _scene;
    [SerializeField] int _maxNumPlayers;

    private List<UserNetworkSettings> _players = new();
    private Scene _currentRoom;
    private bool _isLoaded = false;

    public bool IsHaveSlot { get => _maxNumPlayers > _players.Count; }
    public int NumOfFreeSlots { get => _maxNumPlayers - _players.Count; }
    public Scene Scene => _currentRoom;
    public bool IsLoaded => _isLoaded;

    public event UnityAction SlotsEnded;

    public abstract IEnumerator GameStarting();
    public abstract IEnumerator Game();
    public abstract IEnumerator GameEnding();

    public IEnumerator LoadRoomJob(LocalPhysicsMode physicsMode = LocalPhysicsMode.Physics2D)
    {
        if (_isLoaded == false)
        {
            yield return SceneManager.LoadSceneAsync(_scene, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = physicsMode });
            _currentRoom = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            _isLoaded = true;
        }
    }

    public IEnumerator UnloadRoomJob()
    {
        if (_isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(_currentRoom);
            _isLoaded = false;
        }
    }

    public bool TryAddPlayerInRoom(GameObject player)
    {
        if (IsHaveSlot && _isLoaded)
        {
            SceneManager.MoveGameObjectToScene(player, _currentRoom);

            UserNetworkSettings playerSettings = player.GetComponent<UserNetworkSettings>();

            playerSettings.MyRoom = _currentRoom;

            _players.Add(playerSettings);

            if (IsHaveSlot == false)
                SlotsEnded?.Invoke();

            return true;
        }
        else
        {
            Debug.LogError($"Room loaded status - {_isLoaded}\nFree slots - {NumOfFreeSlots}");
            return false;
        }
    }
}
