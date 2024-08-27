using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGameRules : GameRules
{
    [SerializeField] private float _lifeTime = 10f;
    [SerializeField] private bool isRemoveRoom = true;

    private TeamsPanel _teams;

    public override void GameStartServer()
    {
        StartCoroutine(SplitIntoTeams());

        if (isServer && isRemoveRoom)
            StartCoroutine(CloseJob());
    }

    protected override void GameStartClient()
    {
        _teams = FindObjectOfType<TeamsPanel>();

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].GetComponent<UserNetworkSettings>().TeamIndex == 1)
            {
                _teams.AddInFirstTeam(_players[i].GetComponent<Character>());
            }
            else
            {
                _teams.AddInSecondTeam(_players[i].GetComponent<Character>());
            }
        }
    }

    private IEnumerator CloseJob()
    {
        while(_lifeTime > 0)
        {
            _lifeTime -= Time.deltaTime;
            yield return null;
        }
        StartCoroutine(CloseRoomJob());
    }
}
