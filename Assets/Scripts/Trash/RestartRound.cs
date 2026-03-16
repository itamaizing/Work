using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartRound : MonoBehaviour
{
   [SerializeField][ReadOnly] private GameRules _gameRules;

    public GameRules GameRules { get => _gameRules; set => _gameRules = value; }

    public void Restart()
    {
        if (_gameRules != null) _gameRules.CallRestartRound();
        else RestartGame();
    }

    public void RestartGame()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}