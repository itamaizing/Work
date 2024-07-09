using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NetworkRoomsManager : NetworkBehaviour
{
	[SerializeField] private GameMode _gameMode;
	//[SerializeField] private GameObject _gameRulesPref;

	private List<NetworkRoom> _rooms = new();

	public void AddPlayer(GameObject player)
    {
		if (_rooms.Count <= 0)
        {

        }
    }
}

public enum GameMode
{
	GameMode1vs1,
	GameMode2vs2,
	GameMode3vs3,
	GameModeAllvsAll,
}
