using Unity.Collections;
using UnityEngine;

public class RestartRound : MonoBehaviour
{
   [SerializeField][ReadOnly] private GameRules _gameRules;

    public GameRules GameRules { get => _gameRules; set => _gameRules = value; }

    public void Restart()
    {
        if (_gameRules == null)
        {
            Debug.LogError("GameRules not found");
            return;
        }

        _gameRules.CallRestartRound();
    }
}