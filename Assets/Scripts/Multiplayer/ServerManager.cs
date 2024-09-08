using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ServerManager : NetworkBehaviour
{
    [SerializeField] private List<NetworkRoomsManager> _managers;
    [SerializeField] private HeroSelectPanel _heroSelectPanel;
    [SerializeField] private GameModeSelectPanel _gameModeSelectPanel;
    [SerializeField] private Button _startButton;

    private void Awake()
    {
        _startButton.onClick.AddListener(AddPlayer);
    }

    private void AddPlayer()
    {
        AddPlayer(User.Instance.gameObject, _heroSelectPanel.SelectedHeroIndex, _gameModeSelectPanel.GameMode);
    }

    [Command(requiresAuthority = false)]
    private void AddPlayer(GameObject user, int CharacterIndex, GameMode gameMode)
    {
        StartCoroutine(AddPlayerInRoomJob(user, CharacterIndex, gameMode));
    }

    private int GetManagerIndex(GameMode mode)
    {
        for (int i = 0; i < _managers.Count; i++)
        {
            if (_managers[i].GameMode == mode)
                return i;
        }
        Debug.LogError("manager not found");
        return -37;
    }

    private IEnumerator AddPlayerInRoomJob(GameObject user, int CharacterIndex, GameMode gameMode)
    {
        GameObject player = Instantiate(_heroSelectPanel.HeroList[CharacterIndex].gameObject);
        NetworkServer.Spawn(player, user);

        int index = GetManagerIndex(gameMode);

        yield return StartCoroutine(_managers[index].AddPlayerJob(player));

        user.GetComponent<User>().connectionToClient.Send(new SceneMessage { sceneName = _managers[index].Scene, sceneOperation = SceneOperation.LoadAdditive });
        SceneManager.MoveGameObjectToScene(user, SceneManager.GetSceneAt(SceneManager.sceneCount - 1));
    }
}
