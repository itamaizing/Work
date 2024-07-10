using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class NetworkRoomsManager : NetworkBehaviour
{
	[SerializeField] private GameMode _gameMode;
	[SerializeField, Scene] private string _scene;
	[Header("Game Rules")]
	[SerializeField] private int _maxPlayers;
	[SerializeField] private GameRules _gameRulesPref;

	private List<NetworkRoom> _rooms = new();

    protected override void OnValidate()
    {
        base.OnValidate();

		switch (_gameMode)
        {
			case GameMode.GMTest:
				_maxPlayers = 1;
				break;

			case GameMode.GM1vs1:
				_maxPlayers = 2;
				break;

			case GameMode.GM2vs2:
				_maxPlayers = 4;
				break;

			case GameMode.GM3vs3:
				_maxPlayers = 6;
				break;
        }
    }

    public IEnumerator AddPlayerJob(GameObject player)
    {
		if (_rooms.Count <= 0 || _rooms[_rooms.Count - 1].IsHaveSlot == false)
        {
			NetworkRoom room = new NetworkRoom();
			room.Init(_scene, _maxPlayers);

			_rooms.Add(room);

			_rooms[_rooms.Count - 1].SlotsEnded += OnRoomSlotsEnded;
			_rooms[_rooms.Count - 1].RoomClosed += OnRoomClosed;

			yield return StartCoroutine(_rooms[_rooms.Count - 1].LoadRoomJob());
		}

		_rooms[_rooms.Count - 1].TryAddPlayerInRoom(player);
	}

	private void OnRoomSlotsEnded(NetworkRoom room)
    {
		GameRules rules = Instantiate(_gameRulesPref);
		room.GameStart(rules);
    }

	private void OnRoomClosed(NetworkRoom room)
    {
		_rooms.Remove(room);
	}
}

public enum GameMode
{
	GMTest,
	GM1vs1,
	GM2vs2,
	GM3vs3,
	GMAllvsAll,
}
