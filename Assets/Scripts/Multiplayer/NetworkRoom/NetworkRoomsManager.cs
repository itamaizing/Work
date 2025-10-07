using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkRoomsManager : NetworkBehaviour
{
	[SerializeField] private GameMode _gameMode;
	[SerializeField, Scene] private string _scene;
	[Header("Game Rules")]
	[SerializeField] private int _maxPlayers;
	[SerializeField] private GameRules _gameRulesPref;

	private GameRules _gameRules;

	private readonly List<NetworkRoom> _rooms = new();

    public string Scene => _scene;

    public GameMode GameMode { get => _gameMode; set => _gameMode = value; }

    protected override void OnValidate()
    {
        base.OnValidate();

		switch (_gameMode)
		{
			case GameMode.GM1vs1:
				_maxPlayers = 2;
				break;

			case GameMode.GM2vs2:
				_maxPlayers = 4;
				break;

			case GameMode.GM3vs3:
				_maxPlayers = 6;
				break;

			case GameMode.GM1vs1MaximumMode:
				_maxPlayers = 2;

				break;
		}
	}

	/// <summary>
	/// Old AddPlayerJob
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	//   public IEnumerator AddPlayerJob(GameObject player)
	//   {
	//	if (_rooms.Count <= 0 || _rooms[^1].IsHaveSlot == false)
	//       {
	//		NetworkRoom room = new NetworkRoom();
	//		room.Init(_scene, _maxPlayers);

	//		_rooms.Add(room);

	//		_rooms[^1].SlotsEnded += OnRoomSlotsEnded;
	//		_rooms[^1].RoomClosed += OnRoomClosed;

	//		yield return StartCoroutine(_rooms[^1].LoadRoomJob());
	//           _gameRules = Instantiate(_gameRulesPref);
	//       }

	//	_rooms[^1].TryAddPlayerInRoom(player);
	//}

	public IEnumerator AddPlayerJob(GameObject player)
	{
		if (_rooms.Count <= 0 || !_rooms[^1].IsHaveSlot)
		{
			NetworkRoom room = new NetworkRoom();
			room.Init(_scene, _maxPlayers);

			_rooms.Add(room);

			yield return StartCoroutine(room.LoadRoomJob());

			while (!room.IsLoaded)	yield return null;

			room.SlotsEnded += OnRoomSlotsEnded;
			room.RoomClosed += OnRoomClosed;

			_gameRules = Instantiate(_gameRulesPref);
			room.TryAddPlayerInRoom(player);
		}
		else
		{
			_rooms[^1].TryAddPlayerInRoom(player);
		}
	}

	private void OnRoomSlotsEnded(NetworkRoom room)
    {
		room.GameStart(_gameRules);
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
	GM1vs1MaximumMode,
	Battlegrounds,
	None
}

public enum MainGameMode
{
	Battlegrounds,
	Arena,
	None
}

